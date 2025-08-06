namespace BTKETicaretSitesi.Models
{
    public class KnowledgeBase
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<string> Keywords { get; set; } // Arama için anahtar kelimeler
        public KnowledgeCategory Category { get; set; }
    }

    public enum KnowledgeCategory
    {
        ProductInfo,
        ReturnPolicy,
        PaymentMethods,
        ShippingInfo,
        AccountManagement,
        General
    }
}
