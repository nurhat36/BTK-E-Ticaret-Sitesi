using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using BTKETicaretSitesi.Models;

namespace BTKETicaretSitesi.Data
{
    public static class SeedData
    {
        public static async Task Initialize(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Admin kullanıcı oluştur
            var adminEmail = "admin@site.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Slug = "admin",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    PhoneNumber = "5551234567", // Sample phone number
                    ProfileImageUrl = "/uploads/profile-images/6663f122-be96-4dbb-81b1-53e38824ed6c_20250626212920.jpg" // Default profile image URL
                };
                await userManager.CreateAsync(adminUser, "Admin123!"); // Güçlü bir şifre ver

                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
