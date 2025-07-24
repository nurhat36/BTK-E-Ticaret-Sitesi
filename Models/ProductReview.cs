namespace BTKETicaretSitesi.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        public int Rating { get; set; } // 1-5 arası
        public string Title { get; set; }
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = false;

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}