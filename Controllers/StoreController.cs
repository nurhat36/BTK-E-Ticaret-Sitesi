using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BTKETicaretSitesi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models.ViewModels;

namespace BTKETicaretSitesi.Controllers
{
    [Authorize]
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public StoreController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: Mağaza Paneli
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var store = await _context.Stores
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
            {
                return RedirectToAction("Create");
            }

            return View(store);
        }
        [HttpGet("{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var store = await _context.Stores
                .Include(s => s.Owner)
                .Include(s => s.Products)
                    .ThenInclude(p => p.Images)
                .Include(s => s.Products)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(s => s.Slug == slug && s.IsApproved);

            if (store == null)
            {
                return NotFound();
            }

            // Popüler ürünleri getir (en çok satan veya en çok görüntülenen)
            var popularProducts = store.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToList();

            var model = new StoreDetailsViewModel
            {
                Store = store,
                PopularProducts = popularProducts,
                AllProductsCount = store.Products.Count
            };

            return View(model);
        }

        // GET: Mağaza Oluşturma Sayfası
        public IActionResult Create()
        {
            return View();
        }

        // POST: Mağaza Oluşturma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StoreViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Slug oluşturma
                var slug = model.Name.ToLower().Replace(" ", "-");
                slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

                // Logo yükleme
                string logoUrl = null;
                if (model.LogoImage != null && model.LogoImage.Length > 0)
                {
                    try
                    {
                        // wwwroot yolunu doğru şekilde al
                        var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "store-logos");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = $"{userId}_{slug}{Path.GetExtension(model.LogoImage.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.LogoImage.CopyToAsync(fileStream);
                        }

                        logoUrl = $"/uploads/store-logos/{uniqueFileName}";
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Logo yüklenirken hata oluştu: " + ex.Message);
                        return View(model);
                    }
                }

                // Mağaza oluşturma işlemi
                var store = new Store
                {
                    Name = model.Name,
                    Description = model.Description,
                    Slug = slug,
                    LogoUrl = logoUrl,
                    BannerUrl = logoUrl,
                    OwnerId = userId,
                    IsApproved = false,
                    CreatedAt = DateTime.Now,
                };

                _context.Stores.Add(store);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mağazanız başarıyla oluşturuldu. Admin onayı bekleniyor.";
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }

        // GET: Mağaza Düzenleme Sayfası
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
            {
                return NotFound();
            }

            var model = new StoreEditViewModel
            {
                Id = store.Id,
                Name = store.Name,
                Description = store.Description,
                CurrentLogoUrl = store.LogoUrl
            };

            return View(model);
        }

        // POST: Mağaza Düzenleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StoreEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == model.Id && s.OwnerId == userId);

                if (store == null)
                {
                    return NotFound();
                }

                // Logo güncelleme
                string logoUrl = null;
                if (model.LogoImage != null && model.LogoImage.Length > 0)
                {
                    try
                    {
                        // wwwroot yolunu doğru şekilde al
                        var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "store-logos");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = $"{userId}_{store.Slug}{Path.GetExtension(model.LogoImage.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.LogoImage.CopyToAsync(fileStream);
                        }

                        logoUrl = $"/uploads/store-logos/{uniqueFileName}";
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Logo yüklenirken hata oluştu: " + ex.Message);
                        return View(model);
                    }
                }

                store.Name = model.Name;
                store.Description = model.Description;
                store.UpdatedAt = DateTime.Now;

                _context.Update(store);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mağaza bilgileri başarıyla güncellendi.";
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }

        // GET: Mağaza İstatistikleri (Örnek)
        public async Task<IActionResult> Stats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var store = await _context.Stores
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
            {
                return RedirectToAction("Create");
            }

            // Temel istatistikler
            var model = new StoreStatsViewModel
            {
                Store = store,
                TotalProducts = await _context.Products.CountAsync(p => p.StoreId == store.Id),
                TotalOrders = await _context.OrderItems
                    .Where(oi => oi.StoreId == store.Id)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync(),
                TotalRevenue = await _context.OrderItems
                    .Where(oi => oi.StoreId == store.Id)
                    .SumAsync(oi => oi.UnitPrice * oi.Quantity)
            };

            // GitHub benzeri aktivite heatmap verisi (son 1 yıl)
            var startDate = DateTime.Now.AddYears(-1);
            var orderItems = await _context.OrderItems
                .Where(oi => oi.StoreId == store.Id)
                .Include(oi => oi.Order) // Order'ı include et
                .Where(oi => oi.Order.OrderDate >= startDate)
                .ToListAsync(); // Client-side'a çek

            model.SalesActivities = orderItems
                .GroupBy(oi => oi.Order.OrderDate.Date)
                .Select(g => new SalesActivity
                {
                    Date = g.Key,
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    ProductCount = g.Sum(oi => oi.Quantity)
                })
                .ToList();

            // Aylık satış verileri (client-side gruplama)
            model.MonthlySales = orderItems
                .GroupBy(oi => new {
                    Year = oi.Order.OrderDate.Year,
                    Month = oi.Order.OrderDate.Month
                })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ToDictionary(
                    g => $"{g.Key.Year}-{g.Key.Month:D2}",
                    g => g.Sum(oi => oi.Quantity)
                );

            // En çok satan 5 ürün
            model.TopProducts = orderItems
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new TopProduct
                {
                    ProductName = g.Key.Name,
                    SalesCount = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(p => p.SalesCount)
                .Take(5)
                .ToList();

            return View(model);
        }
    }
}