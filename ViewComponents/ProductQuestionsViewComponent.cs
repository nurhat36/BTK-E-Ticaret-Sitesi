using BTKETicaretSitesi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BTKETicaretSitesi.ViewComponents
{
    public class ProductQuestionsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public ProductQuestionsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int productId)
        {
            var questions = await _context.ProductQuestions
                .Include(q => q.User)
                .Include(q => q.Seller)
                .Where(q => q.ProductId == productId && q.IsPublished)
                .OrderByDescending(q => q.QuestionDate)
                .ToListAsync();

            ViewBag.ProductId = productId;
            return View(questions);
        }
    }
}