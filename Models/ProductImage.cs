namespace BTKETicaretSitesi.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public bool IsMainImage { get; set; }
        public int DisplayOrder { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}