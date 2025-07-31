namespace BTKETicaretSitesi.Models
{
    public class ProductQuestion
    {
        public int Id { get; set; }

        // Sorunun metni
        public string QuestionText { get; set; }

        // Sorunun sorulduğu tarih
        public DateTime QuestionDate { get; set; } = DateTime.Now;

        // Yanıtın metni (opsiyonel, satıcı yanıtlamadıysa null olabilir)
        public string? AnswerText { get; set; }

        // Yanıtın verildiği tarih (opsiyonel)
        public DateTime? AnswerDate { get; set; }

        // Soru ve yanıtın yayınlanıp yayınlanmayacağı (admin onayı)
        public bool IsPublished { get; set; } = false;

        // Hangi ürüne ait olduğu
        public int ProductId { get; set; }
        public Product Product { get; set; }

        // Soruyu soran kullanıcı
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Yanıtı veren satıcı (opsiyonel)
        public string? SellerId { get; set; }
        public ApplicationUser? Seller { get; set; } // Satıcı da bir ApplicationUser olabilir
    }
}