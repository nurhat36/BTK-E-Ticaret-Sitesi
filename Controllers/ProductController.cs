using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using BTKETicaretSitesi.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using BTKETicaretSitesi.Models.ViewModels;
using BTKETicaretSitesi.ViewModels;

namespace BTKETicaretSitesi.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Store)
                .Include(p => p.Images)
                .Include(p => p.Attributes)
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .ToListAsync();

            return View(products);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Store)
                .Include(p => p.Images)
                .Include(p => p.Attributes)
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                MainImage = product.Images.FirstOrDefault(i => i.IsMainImage)?.ImageUrl,
                OtherImages = product.Images.Where(i => !i.IsMainImage).OrderBy(i => i.DisplayOrder).ToList(),
                Attributes = product.Attributes.ToList(),
                Variants = product.Variants.ToList(),
                Reviews = product.Reviews.Where(r => r.IsApproved).ToList()
            };

            return View(viewModel);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            var viewModel = new ProductCreateViewModel
            {
                Categories = _context.Categories.ToList(),
                Stores = _context.Stores.ToList()
            };
            return View(viewModel);
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel viewModel,
    List<IFormFile> productImages,
    List<ProductAttribute> Attributes,
    List<ProductVariant> Variants)
        {
            if (!ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Ürün temel bilgilerini kaydet
                        var product = new Product
                        {
                            Name = viewModel.Name,
                            Description = viewModel.Description,
                            Price = viewModel.Price,
                            DiscountPrice = viewModel.DiscountPrice,
                            StockQuantity = viewModel.StockQuantity,
                            SKU = viewModel.SKU,
                            Barcode = viewModel.Barcode,
                            Slug = viewModel.Slug,
                            IsActive = viewModel.IsActive,
                            CategoryId = viewModel.CategoryId,
                            StoreId = viewModel.StoreId,
                            CreatedAt = DateTime.Now
                        };

                        _context.Products.Add(product);
                        await _context.SaveChangesAsync();

                        // Ürün görsellerini işle
                        if (productImages != null && productImages.Count > 0)
                        {
                            var imageList = new List<ProductImage>();
                            var isFirstImage = true;
                            var displayOrder = 1;

                            foreach (var imageFile in productImages)
                            {
                                if (imageFile.Length > 0)
                                {
                                    // Resim kaydetme işlemi (bu kısmı kendi storage sisteminize göre düzenleyin)
                                    var imageUrl = await SaveImage(imageFile);

                                    imageList.Add(new ProductImage
                                    {
                                        ImageUrl = imageUrl,
                                        IsMainImage = isFirstImage,
                                        DisplayOrder = displayOrder,
                                        ProductId = product.Id
                                    });

                                    isFirstImage = false;
                                    displayOrder++;
                                }
                            }

                            if (imageList.Any())
                            {
                                await _context.ProductImages.AddRangeAsync(imageList);
                            }
                        }

                        // Ürün özelliklerini ekle
                        if (Attributes != null && Attributes.Any())
                        {
                            var attributes = Attributes
                                .Where(a => !string.IsNullOrEmpty(a.Key) && !string.IsNullOrEmpty(a.Value))
                                .Select(a => new ProductAttribute
                                {
                                    Key = a.Key,
                                    Value = a.Value,
                                    ProductId = product.Id
                                })
                                .ToList();

                            if (attributes.Any())
                            {
                                await _context.ProductAttributes.AddRangeAsync(attributes);
                            }
                        }

                        // Ürün varyantlarını ekle
                        if (Variants != null && Variants.Any())
                        {
                            var variants = Variants
                                .Where(v => !string.IsNullOrEmpty(v.VariantType))
                                .Select(v => new ProductVariant
                                {
                                    VariantType = v.VariantType,
                                    VariantValue = v.VariantValue,
                                    StockQuantity = v.StockQuantity,
                                    SKU = v.SKU,
                                    ProductId = product.Id
                                })
                                .ToList();

                            if (variants.Any())
                            {
                                await _context.ProductVariants.AddRangeAsync(variants);
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Details), new { id = product.Id });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Ürün kaydedilirken bir hata oluştu: " + ex.Message);
                        // Hata loglama işlemi yapılabilir
                    }
                }
            }

            // Eğer model geçerli değilse veya hata oluştuysa
            viewModel.Categories = await _context.Categories.ToListAsync();
            viewModel.Stores = await _context.Stores.ToListAsync();
            return View(viewModel);
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            // Bu metodu kendi resim saklama sisteminize göre uyarlayın
            // Örnek: wwwroot/uploads klasörüne kaydetme

            var uploadsFolder = Path.Combine("wwwroot", "uploads", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/uploads/products/" + uniqueFileName;
        }
        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductEditViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                StockQuantity = product.StockQuantity,
                SKU = product.SKU,
                Barcode = product.Barcode,
                Slug = product.Slug,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                StoreId = product.StoreId,
                Categories = _context.Categories.ToList(),
                Stores = _context.Stores.ToList()
            };

            return View(viewModel);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                try
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null)
                    {
                        return NotFound();
                    }

                    product.Name = viewModel.Name;
                    product.Description = viewModel.Description;
                    product.Price = viewModel.Price;
                    product.DiscountPrice = viewModel.DiscountPrice;
                    product.StockQuantity = viewModel.StockQuantity;
                    product.SKU = viewModel.SKU;
                    product.Barcode = viewModel.Barcode;
                    product.Slug = viewModel.Slug;
                    product.IsActive = viewModel.IsActive;
                    product.CategoryId = viewModel.CategoryId;
                    product.StoreId = viewModel.StoreId;
                    product.UpdatedAt = DateTime.Now;

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            viewModel.Categories = _context.Categories.ToList();
            viewModel.Stores = _context.Stores.ToList();
            return View(viewModel);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Store)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            product.IsActive = false;
            product.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // Ürün görselleri için action'lar
        public async Task<IActionResult> ManageImages(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductImagesViewModel
            {
                ProductId = productId,
                Images = product.Images.OrderBy(i => i.DisplayOrder).ToList()
            };

            return View(viewModel);
        }

        // Ürün özellikleri için action'lar
        public async Task<IActionResult> ManageAttributes(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductAttributesViewModel
            {
                ProductId = productId,
                Attributes = product.Attributes.ToList()
            };

            return View(viewModel);
        }

        // Ürün yorumları için action'lar
        public async Task<IActionResult> ManageReviews(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductReviewsViewModel
            {
                ProductId = productId,
                Reviews = product.Reviews.ToList()
            };

            return View(viewModel);
        }
    }
}