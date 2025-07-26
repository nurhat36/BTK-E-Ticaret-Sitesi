using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using BTKETicaretSitesi.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTKETicaretSitesi.Controllers
{
    [Route("products/{productId}/attributes")]
    public class ProductAttributeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductAttributeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            return View(new ProductAttributesViewModel
            {
                ProductId = productId,
                ProductName = product.Name,
                Attributes = product.Attributes.ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(int productId, [FromForm] ProductAttribute attribute)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            attribute.ProductId = productId;
            _context.ProductAttributes.Add(attribute);
            await _context.SaveChangesAsync();

            return PartialView("_AttributeItem", attribute);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int productId, int id, [FromForm] ProductAttribute attribute)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _context.ProductAttributes
                .FirstOrDefaultAsync(pa => pa.Id == id && pa.ProductId == productId);

            if (existing == null) return NotFound();

            existing.Key = attribute.Key;
            existing.Value = attribute.Value;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int productId, int id)
        {
            var attribute = await _context.ProductAttributes
                .FirstOrDefaultAsync(pa => pa.Id == id && pa.ProductId == productId);

            if (attribute == null) return NotFound();

            _context.ProductAttributes.Remove(attribute);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
