using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;
using BTKETicaretSitesi.Data; // ApplicationDbContext'in bulunduğu namespace

namespace BTKETicaretSitesi.Attributes
{
    public class StoreOwnerOnlyAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new ChallengeResult();
                return;
            }

            if (user.IsInRole("Admin"))
            {
                return;
            }

            var storeId = context.RouteData.Values["id"]?.ToString();
            if (string.IsNullOrEmpty(storeId))
            {
                storeId = context.HttpContext.Request.Query["id"].ToString();
            }

            if (string.IsNullOrEmpty(storeId) || !int.TryParse(storeId, out int id))
            {
                context.Result = new ForbidResult();
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
            var store = dbContext.Stores.Find(id);

            if (store == null || store.OwnerId != user.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}