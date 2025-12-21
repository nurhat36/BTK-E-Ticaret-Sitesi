using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BTKETicaretSitesi.Controllers
{
    [Authorize]
    [Route("Store/ProductQuestions")]
    [EnableRateLimiting("GenelSiteLimiti")]

    public class StoreProductQuestionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QuestionAnalysisService _questionAnalysisService;

        public StoreProductQuestionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            QuestionAnalysisService questionAnalysisService)
        {
            _context = context;
            _userManager = userManager;
            _questionAnalysisService = questionAnalysisService;
        }

        // Mağazadaki ürünlere gelen sorular
        [HttpGet("Index")]
        public async Task<IActionResult> Index(QuestionStatus status)
        {
            var userId = _userManager.GetUserId(User);

            var query = _context.ProductQuestions
                .Include(q => q.Product)  // Ürün bilgisi
                .Include(q => q.User)     // Soruyu soran
                .Include(q => q.Seller)   // Cevaplayan (opsiyonel)
                .Where(q => q.Product.Store.OwnerId == userId);
            
            // Filtreleme kısmını düzeltiyoruz
            query = status switch
            {
                QuestionStatus.Unanswered => query.Where(q => string.IsNullOrEmpty(q.AnswerText)),
                QuestionStatus.Answered => query.Where(q => q.AnswerText != null && q.AnswerText.Length > 0),
                _ => query // Tümü
            };

            var questions = await query
                .OrderByDescending(q => q.QuestionDate)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(questions);
        }
        // Soru detay ve yanıtlama sayfası
        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.Id == id && q.Product.Store.OwnerId == userId);

            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // Yanıt gönderme action'ı
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnswerQuestion(int id, string answerText)
        {
            var userId = _userManager.GetUserId(User);
            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .FirstOrDefaultAsync(q => q.Id == id && q.Product.Store.OwnerId == userId);

            if (question == null)
            {
                return NotFound();
            }

            question.AnswerText = answerText;
            question.AnswerDate = DateTime.Now;
            question.SellerId = userId;
            question.IsPublished = true; // Admin onayı bekliyor

            _context.Update(question);
            await _context.SaveChangesAsync();
            //yapay zeka analizi
            await _questionAnalysisService.AnalyzeQuestionsIfNeeded(question.ProductId);

            TempData["SuccessMessage"] = "Yanıtınız gönderildi. Yönetici onayından sonra yayınlanacaktır.";
            return RedirectToAction(nameof(Index));
        }
    }

    public enum QuestionStatus
    {
        All,
        Unanswered,
        Answered
    }
}