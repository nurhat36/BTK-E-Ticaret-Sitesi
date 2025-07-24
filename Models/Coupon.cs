namespace BTKETicaretSitesi.Models
{
    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool IsPercentage { get; set; } // Sabit miktar mı yüzde mi?
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int UsageLimit { get; set; } = 0; // 0 = sınırsız
        public int UsedCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Hangi mağazaya ait (null ise platform genelinde geçerli)
        public int? StoreId { get; set; }
        public Store Store { get; set; }

        // Hangi kategorilerde geçerli
        public ICollection<CouponCategory> ApplicableCategories { get; set; }
    }

    public class CouponCategory
    {
        public int CouponId { get; set; }
        public Coupon Coupon { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}