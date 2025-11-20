namespace BTKETicaretSitesi.Models.DTO
{
    public class ReviewAnalysisRequest
    {
        public List<ReviewItemDto> Reviews { get; set; }
    }

    public class ReviewItemDto
    {
        public double Rating { get; set; }
        public string Comment { get; set; }
    }

    public class ReviewAnalysisResponse
    {
        public bool IsSuccess { get; set; }
        public string Sentiment { get; set; }
        public double QualityScore { get; set; }
        public string SummaryInsights { get; set; }
        public string ErrorMessage { get; set; }
    }
}
