using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BTKETicaretSitesi.Controllers
{
    [Authorize]
    [Route("Store/Orders")]
    [EnableRateLimiting("GenelSiteLimiti")]

    public class StoreOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StoreOrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Mağazaya ait tüm siparişler
        [HttpGet("Index")]
        public async Task<IActionResult> Index(OrderStatus? status = null)
        {
            var userId = _userManager.GetUserId(User);
            var storeId = await _context.Stores
                .Where(s => s.OwnerId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (storeId == 0)
            {
                return NotFound("Mağaza bulunamadı");
            }

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                .Include(o => o.StatusHistory)
                    .ThenInclude(h => h.ChangedByUser)
                .Where(o => o.Items.Any(i => i.StoreId == storeId))
                .OrderByDescending(o => o.OrderDate);

            if (status.HasValue)
            {
                query = (IOrderedQueryable<Order>)query.Where(o => o.Status == status.Value);
            }

            var orders = await query.ToListAsync();

            // Aynı mağazaya ait birden fazla ürün varsa gruplama yap
            foreach (var order in orders)
            {
                order.Items = order.Items.Where(i => i.StoreId == storeId).ToList();
            }

            ViewBag.Status = status;
            return View(orders);
        }

        // Mağazaya ait sipariş detayı
        [HttpGet("Details/{orderNumber}")]
        public async Task<IActionResult> Details(string orderNumber)
        {
            var userId = _userManager.GetUserId(User);
            var storeId = await _context.Stores
                .Where(s => s.OwnerId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (storeId == 0)
            {
                return NotFound("Mağaza bulunamadı");
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                .Include(o => o.StatusHistory)
                    .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber &&
                                       o.Items.Any(i => i.StoreId == storeId));

            if (order == null)
            {
                return NotFound();
            }

            // Sadece bu mağazaya ait ürünleri göster
            order.Items = order.Items.Where(i => i.StoreId == storeId).ToList();

            return View(order);
        }

        // Sipariş durumunu güncelleme
        [HttpPost("UpdateStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string orderNumber, OrderStatus status)
        {
            var userId = _userManager.GetUserId(User);
            var storeId = await _context.Stores
                .Where(s => s.OwnerId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (storeId == 0)
            {
                return NotFound("Mağaza bulunamadı");
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber &&
                                       o.Items.Any(i => i.StoreId == storeId));

            if (order == null)
            {
                return NotFound();
            }

            // Ana sipariş durumunu güncelle
            order.Status = status;

            // Durum geçmişine ekle
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = status,
                ChangedAt = DateTime.Now,
                ChangedByUserId = userId,
                Notes = $"Mağaza tarafından durum güncellendi: {status}"
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { orderNumber });
        }

        // İade işlemi başlatma
        [HttpPost("InitiateRefund")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateRefund(string orderNumber, int itemId, string reason)
        {
            var userId = _userManager.GetUserId(User);
            var storeId = await _context.Stores
                .Where(s => s.OwnerId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (storeId == 0)
            {
                return NotFound("Mağaza bulunamadı");
            }

            var orderItem = await _context.OrderItems
                .Include(i => i.Order)
                .ThenInclude(o => o.StatusHistory)
                .FirstOrDefaultAsync(i => i.Id == itemId &&
                                        i.Order.OrderNumber == orderNumber &&
                                        i.StoreId == storeId);

            if (orderItem == null)
            {
                return NotFound();
            }

            // Sipariş durumunu Refunded olarak güncelle
            orderItem.Order.Status = OrderStatus.Refunded;

            orderItem.Order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = OrderStatus.Refunded,
                ChangedAt = DateTime.Now,
                ChangedByUserId = userId,
                Notes = $"İade talebi oluşturuldu. Sebep: {reason}"
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { orderNumber });
        }
    }
}