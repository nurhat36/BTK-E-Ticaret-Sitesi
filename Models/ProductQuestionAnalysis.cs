namespace BTKETicaretSitesi.Models
{
    public class ProductQuestionAnalysis
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        // Müşterilerin en sık sorduğu soruların veya konuların özeti
        public string CommonQuestionsSummary { get; set; }

        // Satıcının en sık verdiği yanıtların veya tavsiyelerin özeti
        public string CommonAnswersSummary { get; set; }

        // Analizin yapıldığı tarih
        public DateTime AnalysisDate { get; set; } = DateTime.Now;
    }
}