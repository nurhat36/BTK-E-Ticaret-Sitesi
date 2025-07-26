using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using BTKETicaretSitesi.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BTKETicaretSitesi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger,ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeIndexViewModel
            {
                FeaturedProducts = await _context.Products
            .Include(p => p.Store)
            .Include(p => p.Category)
            .Include(p => p.Images) // Resimleri de include ediyoruz
            .Where(p => p.Store.IsApproved && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .ToListAsync(),

                Categories = await _context.Categories
                    
                    .OrderBy(c => c.Name)
                    .Take(8)
                    .ToListAsync()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
