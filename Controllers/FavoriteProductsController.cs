using BTKETicaretSitesi.Models;
using BTKETicaretSitesi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BTKETicaretSitesi.Controllers
{
    [Authorize]
    [Route("FavoriteProducts")]
    public class FavoriteProductsController : Controller
    {
        private readonly IFavoriteProductService _favoriteProductService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoriteProductsController(
            IFavoriteProductService favoriteProductService,
            UserManager<ApplicationUser> userManager)
        {
            _favoriteProductService = favoriteProductService;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            var userId = _userManager.GetUserId(User);

            if (await _favoriteProductService.IsProductFavorite(userId, productId))
            {
                await _favoriteProductService.RemoveFavoriteProduct(userId, productId);
                return Json(new { isFavorite = false });
            }
            else
            {
                await _favoriteProductService.AddFavoriteProduct(userId, productId);
                return Json(new { isFavorite = true });
            }
        }
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var favoriteProducts = await _favoriteProductService.GetUserFavoriteProducts(userId);
            return View(favoriteProducts);
        }

        [HttpGet]
        public async Task<IActionResult> IsFavorite(int productId)
        {
            var userId = _userManager.GetUserId(User);
            var isFavorite = await _favoriteProductService.IsProductFavorite(userId, productId);
            return Json(new { isFavorite });
        }
    }
}
