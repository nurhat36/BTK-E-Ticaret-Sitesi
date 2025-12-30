using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using BTKETicaretSitesi.Services;
using BTKETicaretSitesi.Endpoints;
using Hangfire;
using System.Threading.RateLimiting;
using BTKETicaretSitesi.Middleware;
using McpService.Middleware;

var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://+:80");
//builder.Configuration.AddEnvironmentVariables();

//var fileEnv = Environment.GetEnvironmentVariable("GEMINI_API_KEY_FILE") ?? "/run/secrets/gemini_api_key";
//if (File.Exists(fileEnv))
//{
//    var apiKey = File.ReadAllText(fileEnv).Trim();
//    builder.Configuration["GoogleAI:ApiKey"] = apiKey; // 👈 Config'e ekledik
//}


// Veritabanı bağlantısı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"))); // Veritabanına işleri kaydeder

builder.Services.AddHangfireServer(); // İşleri yapacak sunucuyu başlatır


builder.Services.AddRateLimiter(options =>
{
    // Hata durumunda (429) ne dönecek?
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 1. GENEL KORUMA (Tüm Site İçin)
    // Her IP adresi dakikada en fazla 100 sayfa gezebilir.
    options.AddPolicy("GenelSiteLimiti", context =>
    {
        // Kullanıcının IP adresini alıyoruz
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                        ?? context.Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress, // Limiti IP'ye göre ayır (ÖNEMLİ!)
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100, // Dakikada 100 istek
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2
            });
    });

    // 2. KRİTİK İŞLEM KORUMASI (Sipariş, Login vb.)
    // Her IP adresi dakikada en fazla 5 kritik işlem yapabilir.
    options.AddPolicy("KritikIslemLimiti", context =>
    {
        // IP adresini al
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                        ?? context.Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

        // Eğer kullanıcı giriş yapmışsa, UserID'ye göre de sınırlayabilirsin!
        // var userId = context.User.Identity?.Name ?? ipAddress; 

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,  // Dakikada SADECE 5 kere basabilir!
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0    // Kuyruk yok, 6. basışta direkt hata ver.
            });
    });


    // LİMİT AŞILDIĞINDA NE OLACAK?
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        // İsteğin tipini kontrol et
        // "Accept" başlığında "text/html" varsa, bu bir tarayıcı isteğidir.
        if (context.HttpContext.Request.Headers.Accept.ToString().Contains("text/html"))
        {
            // Kullanıcıyı oluşturduğumuz şık sayfaya yönlendir
            context.HttpContext.Response.Redirect("/Home/TooManyRequests");
        }
        else
        {
            // Bu bir API isteği veya AJAX çağrısıdır (Örn: Sepete Ekle butonu)
            // Sayfa yönlendirmesi yapma, sadece JSON mesaj dön.
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "RateLimitExceeded",
                message = "Çok fazla istek gönderdiniz. Lütfen 1 dakika bekleyin.\n sakin olunuz."
            }, token);
        }
    };
});


// Kimlik ve rol yapılandırması
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Email gönderimi yapılandırması
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IFavoriteProductService, FavoriteProductService>();
builder.Services.AddScoped<ReviewAnalysisService>();
// Google AI servisi için yapılandırma
builder.Services.AddHttpClient<GeminiApiService>();
builder.Services.AddScoped<GeminiQuestionAnalysisService>();
builder.Services.AddScoped<QuestionAnalysisService>();
// Notification servisi için yapılandırma
builder.Services.AddScoped<NotificationService>();



builder.Services.AddHttpContextAccessor(); // Email veya kullanıcı bilgilerine ulaşmak için gerekli

// Razor Pages ve Controller desteği
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache(); // Zaten varsa tekrar ekleme
builder.Services.AddSingleton<DdosGuardService>(); // Tek bir bekçi (Singleton)
builder.Services.AddSingleton<ScannerDetectorService>();

// SADECE BU İŞ İÇİN EKLENMESİ GEREKENLER: Session servislerini ekle
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturumun ne kadar süreyle aktif kalacağı
    options.Cookie.HttpOnly = true; // JavaScript'ten çerezlere erişimi engeller (güvenlik)
    options.Cookie.IsEssential = true; // GDPR uyumluluğu için gerekli çerez olduğunu işaretler
});


var app = builder.Build();

// Seed işlemi (rol ve kullanıcı ekleme)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {

        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await SeedData.Initialize(userManager, roleManager, context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı tohumlama sırasında bir hata oluştu.");
    }
}

// HTTP pipeline yapılandırması
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Production ortamında özel hata sayfaları
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseHsts(); // HTTP Strict Transport Security Protocol
}
app.UseHangfireDashboard(); // "/hangfire" adresinde harika bir panel açar!
app.UseHttpsRedirection();


app.UseMiddleware<ScannerDetectionMiddleware>();
app.UseMiddleware<DdosProtectionMiddleware>();
app.UseStaticFiles();

app.UseRouting();

// SADECE BU İŞ İÇİN EKLENMESİ GEREKENLER: Session middleware'ini etkinleştir
// app.UseRouting() sonrası ve app.UseAuthentication() öncesi olmalı
app.UseSession();
app.UseRateLimiter();

app.UseAuthentication(); // <-- Bu şart
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapMcpEndpoints();

app.Run();