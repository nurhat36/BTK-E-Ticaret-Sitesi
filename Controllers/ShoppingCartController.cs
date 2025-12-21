using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using BTKETicaretSitesi.Models.ViewModels;
using Microsoft.AspNetCore.RateLimiting;

namespace BTKETicaretSitesi.Controllers
{
    [Route("[controller]")]
    [Authorize]


    [EnableRateLimiting("GenelSiteLimiti")]
    public class ShoppingCartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Sepeti görüntüle

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Images) // Ürün varyantlarını da dahil ediyoruz
                .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new ShoppingCart { UserId = userId };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return View(cart);
        }

        // Sepete ürün ekle
        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart(int productId, int? variantId, int quantity = 1)
        {
            if (quantity < 1)
            {
                return BadRequest("Geçersiz miktar.");
            }

            var userId = _userManager.GetUserId(User);
            var cart = await _context.ShoppingCarts
                .Include(c => c.Items) // Items koleksiyonunu yüklüyoruz
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    UserId = userId,
                    Items = new List<CartItem>() // Items koleksiyonunu initialize ediyoruz
                };
                _context.ShoppingCarts.Add(cart);
            }
            else if (cart.Items == null)
            {
                cart.Items = new List<CartItem>(); // Null ise initialize ediyoruz
            }

            // Null kontrolü eklenmiş hali
            var existingItem = cart.Items?
                .FirstOrDefault(i => i.ProductId == productId && i.VariantId == variantId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.AddedAt = DateTime.Now;
            }
            else
            {
                var newItem = new CartItem
                {
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity = quantity,
                    ShoppingCartId = cart.Id,
                    AddedAt = DateTime.Now
                };
                cart.Items.Add(newItem);
            }

            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Sepetten ürün çıkar
        [HttpPost("RemoveFromCart")]
        public async Task<IActionResult> RemoveFromCart(int itemId)
        {
            var userId = _userManager.GetUserId(User);
            var cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return NotFound("Sepet bulunamadı.");
            }

            var itemToRemove = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (itemToRemove != null)
            {
                _context.CartItems.Remove(itemToRemove);
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Sepetteki ürün miktarını güncelle
        [HttpPost("UpdateQuantity")]
        public async Task<IActionResult> UpdateQuantity(int itemId, int newQuantity)
        {
            if (newQuantity < 1)
            {
                return BadRequest("Miktar 1'den küçük olamaz.");
            }

            var userId = _userManager.GetUserId(User);
            var cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return NotFound("Sepet bulunamadı.");
            }

            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
            {
                return NotFound("Ürün bulunamadı.");
            }

            item.Quantity = newQuantity;
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Sepeti temizle
        [HttpPost("ClearCart")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = _userManager.GetUserId(User);
            var cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return NotFound("Sepet bulunamadı.");
            }

            _context.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // ShoppingCartController'a ekleyin
        
    }
}