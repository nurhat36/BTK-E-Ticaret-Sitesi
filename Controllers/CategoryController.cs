using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using System.Linq;

namespace BTKETicaretSitesi.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryTree()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .Where(c => c.ParentCategoryId == null)
                .Select(c => new CategoryTreeViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    SubCategories = c.SubCategories.Select(sc => new CategoryTreeViewModel
                    {
                        Id = sc.Id,
                        Name = sc.Name,
                        Slug = sc.Slug
                    }).ToList()
                })
                .ToListAsync();

            return Json(categories);
        }

        public class CategoryTreeViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Slug { get; set; }
            public List<CategoryTreeViewModel> SubCategories { get; set; }
        }
    }
}
