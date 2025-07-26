using BTKETicaretSitesi.Models;
using System.Collections.Generic;

namespace BTKETicaretSitesi.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; }
        public string MainImage { get; set; }
        public List<ProductImage> OtherImages { get; set; }
        public List<ProductAttribute> Attributes { get; set; }
        public List<ProductVariant> Variants { get; set; }
        public List<ProductReview> Reviews { get; set; }
    }

    public class ProductCreateViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public string SKU { get; set; }
        public string Barcode { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; } = true;
        public int CategoryId { get; set; }
        public int StoreId { get; set; }

        public List<Category> Categories { get; set; }
        public List<Store> Stores { get; set; }
    }

    public class ProductEditViewModel
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
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public int StoreId { get; set; }

        public List<Category> Categories { get; set; }
        public List<Store> Stores { get; set; }
    }

    // ProductImagesViewModel.cs
    public class ProductImagesViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public List<ProductImage> Images { get; set; }
    }

    // ProductAttributesViewModel.cs
    public class ProductAttributesViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public List<ProductAttribute> Attributes { get; set; }
    }

    // ProductVariantsViewModel.cs
    public class ProductVariantsViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public List<ProductVariant> Variants { get; set; }
    }

    // ProductReviewsViewModel.cs
    public class ProductReviewsViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public List<ProductReview> Reviews { get; set; }
    }
}