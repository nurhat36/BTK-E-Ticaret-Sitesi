namespace BTKETicaretSitesi.Models
{
    public class ProductAttribute
    {
        public int Id { get; set; }
        public string Key { get; set; } // "Renk", "Ağırlık", "Boyut" vb.
        public string Value { get; set; } // "Kırmızı", "1kg", "XL" vb.

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}