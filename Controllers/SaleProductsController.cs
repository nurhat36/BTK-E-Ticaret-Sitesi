using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using BTKETicaretSitesi.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using BTKETicaretSitesi.Services;
namespace BTKETicaretSitesi.Controllers { 
[AllowAnonymous]
[Route("Urunler")]
public class SaleProductsController : Controller
{
    private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ReviewAnalysisService _reviewAnalysisService;

        public SaleProductsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ReviewAnalysisService reviewAnalysisService)
        {
            _context = context;
            _userManager = userManager;
            _reviewAnalysisService = reviewAnalysisService;
        }

        // Tüm aktif ürünlerin listesi
        [HttpGet]
    public async Task<IActionResult> Index(
        int? categoryId,
        string search,
        string sortBy = "newest",
        int page = 1,
        int pageSize = 12)
    {
        var query = _context.Products
            .Include(p => p.Store)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsActive && p.StockQuantity > 0 && p.Store.IsApproved);

        // Kategori filtresi
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
            ViewBag.CurrentCategory = await _context.Categories.FindAsync(categoryId);
        }

        // Arama filtresi
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.Description.Contains(search) ||
                p.Store.Name.Contains(search));
            ViewBag.SearchTerm = search;
        }

        // Sıralama
        switch (sortBy)
        {
            case "price_asc":
                query = query.OrderBy(p => p.Price);
                break;
            case "price_desc":
                query = query.OrderByDescending(p => p.Price);
                break;
           
            default: // "newest"
                query = query.OrderByDescending(p => p.CreatedAt);
                break;
        }

        // Sayfalama
        var totalItems = await query.CountAsync();
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.SortBy = sortBy;

        return View(products);
    }
        [HttpPost("CreateReview/{productId}/{slug?}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReview(int? slug,int productId, int rating, string comment, string title)
        {
            Console.WriteLine("girdim girdim");
            if (!ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    var review = new ProductReview
                    {
                        ProductId = productId,
                        UserId = user.Id,
                        Rating = rating,
                        Title = title,
                        Comment = comment,
                        ReviewDate = DateTime.Now,
                        IsApproved = true // Yorumlar admin onayından sonra yayınlansın
                    };

                    _context.ProductReviews.Add(review);
                    await _context.SaveChangesAsync();
                    //yorumu analize gönderiyoruz 30 yorum olmadan çalışmayacak
                    await _reviewAnalysisService.AnalyzeReviewsIfNeeded(review.ProductId);

                    TempData["ReviewMessage"] = "Yorumunuz başarıyla gönderildi. Admin onayından sonra yayınlanacaktır.";
                    return RedirectToAction("Details", "SaleProducts", new { id = productId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Yorum eklenirken bir hata oluştu: " + ex.Message);
                    TempData["ReviewError"] = "Yorum eklenirken bir hata oluştu.";
                }
            }

            return RedirectToAction("Details", "SaleProducts", new { id = productId });
        }
        [HttpGet("Search")] // veya [HttpGet("Search")] query string için
        public async Task<IActionResult> Search(string query, int? categoryId, int page = 1)
        {
            const int pageSize = 12;

            var productsQuery = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Store)
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Store.IsApproved);

            // Arama sorgusu varsa filtrele
            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(query) ||
                    p.Description.Contains(query) ||
                    p.Category.Name.Contains(query) ||
                    p.Store.Name.Contains(query));
            }
            ViewBag.Categoryes= await _context.Categories.ToListAsync();

            // Kategori filtresi
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            var totalItems = await productsQuery.CountAsync();
            var products = await productsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductSearchViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    ImageUrl = p.Images.FirstOrDefault().ImageUrl,
                    StoreName = p.Store.Name,
                    StoreSlug = p.Store.Slug,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name
                })
                .ToListAsync();

            var model = new ProductSearchResultViewModel
            {
                Query = query,
                Products = products,
                Pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    ItemsPerPage = pageSize,
                    TotalItems = totalItems
                },
                CategoryId = categoryId
            };

            return View(model);
        }

        [HttpGet("category/{slug}")]
        public async Task<IActionResult> Category(string slug, int page = 1)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug);

            if (category == null)
            {
                return NotFound();
            }

            return RedirectToAction("Search","Urunler", new {query=category.Name, categoryId = category.Id, page });
        }

        // Ürün detay sayfası
        [HttpGet("{id}/{slug?}")]
    public async Task<IActionResult> Details(int id, string slug = null)
    {
        var product = await _context.Products
            .Include(p => p.Store)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Attributes)
            .Include(p => p.ReviewAnalysis)
            .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.StockQuantity > 0 && p.Store.IsApproved);


        if (product == null)
        {
            return NotFound();
        }

        // SEO için slug kontrolü
        if (slug != product.Slug)
        {
            return RedirectToAction("Details", new { id, slug = product.Slug });
        }

        // Benzer ürünler
        ViewBag.RelatedProducts = await _context.Products
            .Include(p => p.Images)
            .Where(p => p.CategoryId == product.CategoryId &&
                   p.Id != product.Id &&
                   p.IsActive &&
                   p.StockQuantity > 0)
            .Take(4)
            .ToListAsync();

        return View(product);
    }

    // Hızlı görünüm modal için
    [HttpGet("QuickView/{id}")]
    public async Task<IActionResult> QuickView(int id)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product == null)
        {
            return NotFound();
        }

        return PartialView("_QuickView", product);
    }
}
}