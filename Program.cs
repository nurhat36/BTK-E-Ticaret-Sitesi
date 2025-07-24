using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using BTKETicaretSitesi.Services;

var builder = WebApplication.CreateBuilder(args);

// Veritaban� ba�lant�s�
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Kimlik ve rol yap�land�rmas�
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Email g�nderimi yap�land�rmas�
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

builder.Services.AddHttpContextAccessor(); // Email veya kullan�c� bilgilerine ula�mak i�in gerekli

// Razor Pages ve Controller deste�i
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Seed i�lemi (rol ve kullan�c� ekleme)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedData.Initialize(userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritaban� tohumlama s�ras�nda bir hata olu�tu.");
    }
}

// HTTP pipeline yap�land�rmas�
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // <-- Bu �art
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
