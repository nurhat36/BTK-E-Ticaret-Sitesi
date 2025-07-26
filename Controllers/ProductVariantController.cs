using BTKETicaretSitesi.Attributes;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using BTKETicaretSitesi.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTKETicaretSitesi.Controllers
{
    
    [Route("products/{productId}/variants")]
    public class ProductVariantController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductVariantController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            return View(new ProductVariantsViewModel
            {
                ProductId = productId,
                ProductName = product.Name,
                Variants = product.Variants.ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(int productId, [FromForm] ProductVariant variant)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            variant.ProductId = productId;
            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            return PartialView("_VariantItem", variant);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int productId, int id, [FromForm] ProductVariant variant)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _context.ProductVariants
                .FirstOrDefaultAsync(pv => pv.Id == id && pv.ProductId == productId);

            if (existing == null) return NotFound();

            existing.VariantType = variant.VariantType;
            existing.VariantValue = variant.VariantValue;
            existing.StockQuantity = variant.StockQuantity;
            existing.SKU = variant.SKU;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        

        public async Task<IActionResult> Delete(int productId, int id)
        {
            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(pv => pv.Id == id && pv.ProductId == productId);

            if (variant == null) return NotFound();

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
