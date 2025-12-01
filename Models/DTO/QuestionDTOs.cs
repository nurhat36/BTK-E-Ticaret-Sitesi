namespace BTKETicaretSitesi.Models.DTO
{
    public class QuestionAnalysisRequest
    {
        public List<QuestionItemDto> Questions { get; set; }
    }

    public class QuestionItemDto
    {
        public string QuestionText { get; set; }
        public string AnswerText { get; set; }
    }

    public class QuestionAnalysisResponse
    {
        public bool IsSuccess { get; set; }
        public string CommonQuestionsSummary { get; set; } // Sık sorulanlar özeti
        public string CommonAnswersSummary { get; set; }   // Verilen cevaplar özeti
        public string ErrorMessage { get; set; }
    }
}
