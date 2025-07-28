using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using BTKETicaretSitesi.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BTKETicaretSitesi.Controllers
{
    [Authorize]
    [Route("Orders")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Sipariş listesi
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.CustomerId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Sipariş detayı
        [HttpGet("Details/{orderNumber}")]
        public async Task<IActionResult> Details(string orderNumber)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                .Include(o => o.StatusHistory)
                .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.CustomerId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
        [Authorize]
        public async Task<IActionResult> OrderComplete(int orderId)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Sipariş tamamlandığında sepeti temizle
            var cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null)
            {
                _context.CartItems.RemoveRange(cart.Items);
                await _context.SaveChangesAsync();
            }

            return View(order);
        }

        // Sipariş oluşturma (Checkout)
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            var cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index");
            }

            var model = new CheckoutViewModel
            {
                Cart = cart,
                UserAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .ToListAsync(),
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                NewAddress = new Address
                {
                    FirstName = user.UserName,
                    LastName = user.UserName,
                    PhoneNumber = user.PhoneNumber
                }
            };

            // Set default addresses if available
            if (user.DefaultShippingAddressId != null)
            {
                model.SelectedShippingAddressId = user.DefaultShippingAddressId;
            }

            if (user.DefaultBillingAddressId != null)
            {
                model.SelectedBillingAddressId = user.DefaultBillingAddressId;
            }
            else if (user.DefaultShippingAddressId != null)
            {
                model.SelectedBillingAddressId = user.DefaultShippingAddressId;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            // Sepeti yükle
            model.Cart = await _context.ShoppingCarts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            // Sepet kontrolü
            if (model.Cart == null || !model.Cart.Items.Any())
            {
                return RedirectToAction("Index", "ShoppingCart");
            }

            // Model validasyonu (EN BAŞTA OLMALI)
            if (ModelState.IsValid)
            {
                model.UserAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .ToListAsync();
                return View(model);
            }

            // Adres işlemleri
            if (model.SelectedShippingAddressId != null)
            {
                var shippingAddress = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == model.SelectedShippingAddressId && a.UserId == userId);

                if (shippingAddress != null)
                {
                    model.ShippingAddress = FormatAddress(shippingAddress);
                    model.PhoneNumber = shippingAddress.PhoneNumber;
                }
            }

            if (model.SelectedBillingAddressId != null)
            {
                var billingAddress = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == model.SelectedBillingAddressId && a.UserId == userId);

                model.BillingAddress = billingAddress != null ?
                    FormatAddress(billingAddress) : model.ShippingAddress;
            }
            else if (model.UseShippingAddressForBilling)
            {
                model.BillingAddress = model.ShippingAddress;
            }

            // SİPARİŞ OLUŞTURMA KISMI
            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                CustomerId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                ShippingAddress = model.ShippingAddress,
                BillingAddress = model.BillingAddress,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = "Pending",
                TransactionId = Guid.NewGuid().ToString(), // Örnek transaction ID
                OrderTotal = model.Cart.Items.Sum(i => (i.Variant?.PriceDifference ?? i.Product.Price) * i.Quantity),
                Items = model.Cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    VariantId = i.VariantId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Variant?.PriceDifference ?? i.Product.Price,
                    
                    StoreId = i.Product.StoreId,
                    TotalPrice = (i.Variant?.PriceDifference ?? i.Product.Price) * i.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(model.Cart.Items);
            await _context.SaveChangesAsync();

            // BAŞARILI YÖNLENDİRME
            return RedirectToAction("OrderComplete", new { orderId = order.Id });
        }

        // Yardımcı metod
        private string FormatAddress(Address address)
        {
            return $"{address.FirstName} {address.LastName}\n" +
                   $"{address.Neighborhood} Mah. {address.StreetAddress}\n" +
                   $"{address.District}/{address.City}";
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        private decimal CalculateShippingFee(Order order)
        {
            // Basit kargo ücreti hesaplama
            return order.OrderTotal > 200 ? 0 : 15;
        }

        private decimal CalculateTax(Order order)
        {
            // Basit vergi hesaplama (%8)
            return order.OrderTotal * 0.08m;
        }
    }
}