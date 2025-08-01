using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BTKETicaretSitesi.Controllers
{
    public class ProductQuestionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductQuestionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Ürüne soru sorma formu
        [HttpGet]
        [Authorize]
        public IActionResult AskQuestion(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductQuestion
            {
                ProductId = productId,
                Product = product
            };

            return View(model);
        }

        // Ürüne soru gönderme
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskQuestion(ProductQuestion question)
        {
            if (!ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                question.UserId = user.Id;
                question.QuestionDate = DateTime.Now;
                question.IsPublished = true; // Admin onayına kadar yayınlanmaz

                _context.ProductQuestions.Add(question);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Soru başarıyla gönderildi. Yönetici onayından sonra yayınlanacaktır.";
                return RedirectToAction("Details", "SaleProducts", new { id = question.ProductId });
            }

            // Model geçerli değilse formu tekrar göster
            question.Product = await _context.Products.FindAsync(question.ProductId);
            return View(question);
        }

        // Ürüne ait yayınlanmış soruları listeleme (Partial View için)
        public async Task<IActionResult> GetProductQuestions(int productId)
        {
            var questions = await _context.ProductQuestions
                .Include(q => q.User)
                .Include(q => q.Seller)
                .Where(q => q.ProductId == productId && q.IsPublished)
                .OrderByDescending(q => q.QuestionDate)
                .ToListAsync();

            return PartialView("_ProductQuestions", questions);
        }
    }
}