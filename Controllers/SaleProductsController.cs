using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
namespace BTKETicaretSitesi.Controllers { 
[AllowAnonymous]
[Route("Urunler")]
public class SaleProductsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SaleProductsController(ApplicationDbContext context)
    {
        _context = context;
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