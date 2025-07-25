namespace BTKETicaretSitesi.Models.ViewModels
{
    public class ProductAIDescriptionRequest
    {
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public List<ProductAttributeDto> Attributes { get; set; } = new List<ProductAttributeDto>();
        public List<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();
    }

    public class ProductAttributeDto
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class ProductVariantDto
    {
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public string SKU { get; set; }
    }
}
