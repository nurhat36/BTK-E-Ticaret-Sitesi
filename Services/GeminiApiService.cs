using BTKETicaretSitesi.Models;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Text;
namespace BTKETicaretSitesi.Services { 
public class GeminiApiService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiApiService> _logger;

    public GeminiApiService(IConfiguration configuration, ILogger<GeminiApiService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ProductReviewAnalysis> AnalyzeReviews(List<ProductReview> reviews)
    {
        string? geminiApiKey = _configuration["GoogleAI:ApiKey"];

        if (string.IsNullOrEmpty(geminiApiKey))
        {
            _logger.LogError("Gemini API Anahtarı bulunamadı. 'GoogleAI:ApiKey' anahtarını kontrol edin.");
            throw new InvalidOperationException("Gemini API Anahtarı yapılandırılmamış.");
        }

        // Yorum metinlerini tek bir metin bloğu haline getirme
        var combinedReviews = string.Join("\n---\n", reviews.Select(r => $"Puan: {r.Rating}/5. Yorum: {r.Comment}"));

        // Prompt'u hazırlama
        string prompt = $@"
Aşağıdaki ürün yorumlarını incele ve üç farklı başlıkta analiz et. Sonuçları aşağıdaki formatta döndür:

DUYGU_DURUMU: (yorumların genel hissiyatı: 'Pozitif', 'Negatif' veya 'Nötr' olarak belirt)
KALITE_PUANI: (1-10 arasında bir puan ver ve kısa gerekçesini yaz)
OZET_ONGORULER: (yorumlar arasından en çok tekrar eden ortak noktaları ve müşteri önerilerini maddeler halinde özetle. Örnek: 'Ürünün kalıbı dar olduğu için bir beden büyük alınması öneriliyor.', 'Kargo hızlı geldi.')

Yorumlar:
{combinedReviews}
";

        try
        {
            var googleAIClient = new GoogleAi(geminiApiKey);
            var model = googleAIClient.CreateGenerativeModel("models/gemini-1.5-flash");

            var generateContentRequest = new GenerateContentRequest
            {
                Contents = new List<Content>
                {
                    new Content { Role = "user", Parts = new List<Part> { new Part { Text = prompt } } }
                }
            };

            var response = await model.GenerateContentAsync(generateContentRequest);
            string? analysisText = response?.Text();

            if (string.IsNullOrWhiteSpace(analysisText))
            {
                _logger.LogWarning("Gemini'den boş veya geçersiz analiz yanıtı alındı.");
                return new ProductReviewAnalysis
                {
                    SentimentAnalysisResult = "Nötr",
                    QualityScore = 5.0,
                    SummaryInsights = "Yorum analizi yapılamadı."
                };
            }
            Console.WriteLine("Gemini API'den gelen analiz metni: " + analysisText);
            // Gelen metni ayrıştırma
            var analysisData = ParseAnalysisResponse(analysisText);

            return new ProductReviewAnalysis
            {
                ProductId = reviews.First().ProductId,
                SentimentAnalysisResult = analysisData.SentimentAnalysisResult,
                QualityScore = analysisData.QualityScore,
                SummaryInsights = analysisData.SummaryInsights,
                AnalysisDate = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Gemini API çağrısında bir hata oluştu: {ex.Message}", ex);
            return new ProductReviewAnalysis
            {
                ProductId = reviews.First().ProductId,
                SentimentAnalysisResult = "Hata",
                QualityScore = 0.0,
                SummaryInsights = $"Analiz sırasında bir hata oluştu: {ex.Message}"
            };
        }
    }

        private static ProductReviewAnalysis ParseAnalysisResponse(string analysisText)
        {
            var result = new ProductReviewAnalysis();
            var lines = analysisText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool isInSummary = false;
            var summaryBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("DUYGU_DURUMU:"))
                {
                    result.SentimentAnalysisResult = line.Replace("DUYGU_DURUMU:", "").Trim();
                }
                else if (line.StartsWith("KALITE_PUANI:"))
                {
                    var scorePart = line.Replace("KALITE_PUANI:", "").Trim();

                    // 8/10 gibi ifadede ilk sayıyı almaya çalış
                    var firstPart = scorePart.Split(' ')[0]; // "8/10"
                    var scoreString = firstPart.Split('/')[0].Replace(",", "."); // "8"

                    if (double.TryParse(scoreString, out double score))
                    {
                        result.QualityScore = score;
                    }
                }

                else if (line.StartsWith("OZET_ONGORULER:"))
                {
                    isInSummary = true;
                }
                else if (isInSummary && (line.StartsWith("*") || line.StartsWith("-")))
                {
                    summaryBuilder.AppendLine(line.Trim());
                }
            }

            result.SummaryInsights = summaryBuilder.ToString().Trim();
            return result;

        }

    }
}