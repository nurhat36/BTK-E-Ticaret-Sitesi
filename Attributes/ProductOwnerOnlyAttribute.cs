using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using BTKETicaretSitesi.Data;
using Microsoft.EntityFrameworkCore;

namespace BTKETicaretSitesi.Attributes
{
    public class ProductOwnerOnlyAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Kullanıcı giriş yapmış mı kontrolü
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new ChallengeResult();
                return;
            }

            // Adminler tüm ürünleri görebilir
            if (user.IsInRole("Admin"))
            {
                return;
            }

            // Route'dan productId'yi al
            var productId = context.RouteData.Values["id"]?.ToString();
            if (string.IsNullOrEmpty(productId))
            {
                productId = context.HttpContext.Request.Query["id"].ToString();
            }

            if (string.IsNullOrEmpty(productId) || !int.TryParse(productId, out int id))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Veritabanından ürün ve mağaza bilgisini çek
            var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
            var product = dbContext.Products
                .Include(p => p.Store)
                .FirstOrDefault(p => p.Id == id);

            if (product == null || product.Store.OwnerId != user.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}