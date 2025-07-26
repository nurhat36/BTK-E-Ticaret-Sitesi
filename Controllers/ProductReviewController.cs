using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTKETicaretSitesi.Controllers
{
    [Route("products/{productId}/reviews")]
    public class ProductReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            return View(new ProductReviewsViewModel
            {
                ProductId = productId,
                ProductName = product.Name,
                Reviews = product.Reviews.OrderByDescending(r => r.ReviewDate).ToList()
            });
        }

        [HttpPost("approve/{id}")]
        public async Task<IActionResult> Approve(int productId, int id)
        {
            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(pr => pr.Id == id && pr.ProductId == productId);

            if (review == null) return NotFound();

            review.IsApproved = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("reject/{id}")]
        public async Task<IActionResult> Reject(int productId, int id)
        {
            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(pr => pr.Id == id && pr.ProductId == productId);

            if (review == null) return NotFound();

            review.IsApproved = false;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int productId, int id)
        {
            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(pr => pr.Id == id && pr.ProductId == productId);

            if (review == null) return NotFound();

            _context.ProductReviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
