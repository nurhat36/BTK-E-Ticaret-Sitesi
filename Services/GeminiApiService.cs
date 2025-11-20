using BTKETicaretSitesi.Models;
using BTKETicaretSitesi.Models.DTO;
using System.Net.Http.Json; // Bu namespace gerekli

namespace BTKETicaretSitesi.Services
{
    public class GeminiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiApiService> _logger;

        // HttpClientFactory, Program.cs'ten gelecek
        public GeminiApiService(HttpClient httpClient, ILogger<GeminiApiService> logger, IConfiguration config)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Base adresini ayarlıyoruz
            var baseUrl = config["McpSettings:BaseUrl"];
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<ProductReviewAnalysis> AnalyzeReviews(List<ProductReview> reviews)
        {
            // 1. Veriyi McpService'in anlayacağı formata (DTO) çevir
            var requestDto = new ReviewAnalysisRequest
            {
                Reviews = reviews.Select(r => new ReviewItemDto
                {
                    Rating = r.Rating,
                    Comment = r.Comment
                }).ToList()
            };

            try
            {
                // 2. McpService'e HTTP POST isteği at (Siparişi mutfağa ver)
                // "/api/mcp/analyze-reviews" -> McpController'daki adres
                var response = await _httpClient.PostAsJsonAsync("/api/mcp/analyze-reviews", requestDto);

                // 3. Cevabı Oku
                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadFromJsonAsync<ReviewAnalysisResponse>();

                    if (apiResult != null && apiResult.IsSuccess)
                    {
                        // 4. Gelen sonucu Ana Projenin kendi modeline çevir
                        return new ProductReviewAnalysis
                        {
                            SentimentAnalysisResult = apiResult.Sentiment,
                            QualityScore = apiResult.QualityScore,
                            SummaryInsights = apiResult.SummaryInsights,
                            AnalysisDate = DateTime.Now,
                            ProductId = reviews.First().ProductId
                        };
                    }
                }

                // Hata durumu
                _logger.LogError($"MCP Servis Hatası: {response.StatusCode}");
                return CreateErrorResult(reviews.First().ProductId, "Analiz servisi yanıt vermedi.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Bağlantı Hatası: {ex.Message}");
                return CreateErrorResult(reviews.First().ProductId, "Servise bağlanılamadı.");
            }
        }

        private ProductReviewAnalysis CreateErrorResult(int productId, string msg)
        {
            return new ProductReviewAnalysis
            {
                ProductId = productId,
                SentimentAnalysisResult = "Hata",
                QualityScore = 0,
                SummaryInsights = msg
            };
        }
    }
}