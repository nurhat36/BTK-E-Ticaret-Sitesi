using BTKETicaretSitesi.Models;
using BTKETicaretSitesi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BTKETicaretSitesi.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly NotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationViewComponent(
            NotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, true);

            return View(notifications);
        }
    }
}
