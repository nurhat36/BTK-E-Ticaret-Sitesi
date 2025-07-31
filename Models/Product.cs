using Microsoft.AspNetCore.Mvc;

namespace BTKETicaretSitesi.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public string SKU { get; set; }
        public string Barcode { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Mağaza ilişkisi
        public int StoreId { get; set; }
        public Store Store { get; set; }

        // Kategori ilişkisi
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        // Ürün görselleri
        public ICollection<ProductImage> Images { get; set; }

        // Ürün özellikleri
        public ICollection<ProductAttribute> Attributes { get; set; }

        // Ürün varyantları (renk, beden vb.)
        public ICollection<ProductVariant> Variants { get; set; }

        // Ürün yorumları
        public ICollection<ProductReview> Reviews { get; set; }
        public ProductReviewAnalysis ReviewAnalysis { get; set; }
    }
}