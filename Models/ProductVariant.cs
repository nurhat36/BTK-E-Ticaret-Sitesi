namespace BTKETicaretSitesi.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }
        public string VariantType { get; set; } // "Renk", "Beden" vb.
        public string VariantValue { get; set; } // "Kırmızı", "XL" vb.
        public decimal PriceDifference { get; set; } = 0;
        public int StockQuantity { get; set; }
        public string SKU { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}