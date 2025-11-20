using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using BTKETicaretSitesi.Services;
using BTKETicaretSitesi.Endpoints;

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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// SADECE BU İŞ İÇİN EKLENMESİ GEREKENLER: Session middleware'ini etkinleştir
// app.UseRouting() sonrası ve app.UseAuthentication() öncesi olmalı
app.UseSession();

app.UseAuthentication(); // <-- Bu şart
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapMcpEndpoints();

app.Run();