namespace BTKETicaretSitesi.Models
{
    public class ProductReviewAnalysis
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string SentimentAnalysisResult { get; set; }
        public double QualityScore { get; set; }

        // Yeni eklenen sütun: Özetleyici öngörüler ve öneriler
        public string SummaryInsights { get; set; }

        public DateTime AnalysisDate { get; set; } = DateTime.Now;
    }
}