using BTKETicaretSitesi.Attributes;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using BTKETicaretSitesi.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BTKETicaretSitesi.Controllers
{
    
    [Route("products/{productId}/images")]
    [EnableRateLimiting("GenelSiteLimiti")]
    public class ProductImageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductImageController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            return View(new ProductImagesViewModel
            {
                ProductId = productId,
                ProductName = product.Name,
                Images = product.Images.OrderBy(i => i.DisplayOrder).ToList()
            });
        }
        
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(int productId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Geçersiz dosya");

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var isFirstImage = !await _context.ProductImages.AnyAsync(pi => pi.ProductId == productId);
                var displayOrder = await _context.ProductImages.CountAsync(pi => pi.ProductId == productId) + 1;

                var image = new ProductImage
                {
                    ImageUrl = $"/uploads/products/{uniqueFileName}",
                    IsMainImage = isFirstImage,
                    DisplayOrder = displayOrder,
                    ProductId = productId
                };

                _context.ProductImages.Add(image);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = image.Id,
                    url = image.ImageUrl,
                    isMain = image.IsMainImage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Resim yüklenirken hata: {ex.Message}");
            }
        }

        [HttpPost("set-main")]
       

        public async Task<IActionResult> SetMain(int productId, int imageId)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(pi => pi.Id == imageId && pi.ProductId == productId);

            if (image == null) return NotFound();

            try
            {
                var currentMain = await _context.ProductImages
                    .FirstOrDefaultAsync(pi => pi.ProductId == productId && pi.IsMainImage);

                if (currentMain != null)
                    currentMain.IsMainImage = false;

                image.IsMainImage = true;
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ana resim ayarlanırken hata: {ex.Message}");
            }
        }

        [HttpPost("delete")]
        

        public async Task<IActionResult> Delete(int productId, int imageId)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(pi => pi.Id == imageId && pi.ProductId == productId);

            if (image == null) return NotFound();

            try
            {
                // Fiziksel dosyayı sil
                var filePath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Resim silinirken hata: {ex.Message}");
            }
        }

        [HttpPost("reorder")]
        

        public async Task<IActionResult> Reorder(int productId, [FromBody] int[] imageIds)
        {
            try
            {
                var images = await _context.ProductImages
                    .Where(pi => pi.ProductId == productId)
                    .ToListAsync();

                for (int i = 0; i < imageIds.Length; i++)
                {
                    var image = images.FirstOrDefault(pi => pi.Id == imageIds[i]);
                    if (image != null)
                        image.DisplayOrder = i + 1;
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sıralama güncellenirken hata: {ex.Message}");
            }
        }
    }
}
