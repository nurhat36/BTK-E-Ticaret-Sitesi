using BTKETicaretSitesi.Models;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using BTKETicaretSitesi.Models.DTO;

public class GeminiQuestionAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiQuestionAnalysisService> _logger;

    // HttpClient inject ediliyor
    public GeminiQuestionAnalysisService(HttpClient httpClient, ILogger<GeminiQuestionAnalysisService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;

        // BaseUrl'i appsettings'den alıp ayarlıyoruz
        var baseUrl = config["McpSettings:BaseUrl"];
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    // Dönüş tipi sizin Entity'niz olan 'ProductQuestionAnalysis'
    public async Task<ProductQuestionAnalysis> AnalyzeQuestionsAndAnswers(List<ProductQuestion> questions)
    {
        // 1. McpService'e gidecek paketi hazırla
        var requestDto = new QuestionAnalysisRequest
        {
            Questions = questions.Select(q => new QuestionItemDto
            {
                QuestionText = q.QuestionText,
                AnswerText = q.AnswerText
            }).ToList()
        };

        try
        {
            // 2. McpService'e POST isteği at
            var response = await _httpClient.PostAsJsonAsync("/api/mcp/analyze-questions", requestDto);

            if (response.IsSuccessStatusCode)
            {
                var apiResult = await response.Content.ReadFromJsonAsync<QuestionAnalysisResponse>();

                if (apiResult != null && apiResult.IsSuccess)
                {
                    // 3. Gelen sonucu Entity'e çevir
                    return new ProductQuestionAnalysis
                    {
                        ProductId = questions.First().ProductId,

                        // Null kontrolü (Fallback)
                        CommonQuestionsSummary = !string.IsNullOrEmpty(apiResult.CommonQuestionsSummary)
                                                 ? apiResult.CommonQuestionsSummary
                                                 : "Özet çıkarılamadı.",

                        CommonAnswersSummary = !string.IsNullOrEmpty(apiResult.CommonAnswersSummary)
                                               ? apiResult.CommonAnswersSummary
                                               : "Özet çıkarılamadı.",

                        AnalysisDate = DateTime.Now
                    };
                }
            }

            _logger.LogError($"MCP Servis Hatası: {response.StatusCode}");
            return CreateErrorModel(questions.First().ProductId, "Servis yanıt vermedi.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Bağlantı Hatası: {ex.Message}");
            return CreateErrorModel(questions.First().ProductId, "Bağlantı hatası.");
        }
    }

    private ProductQuestionAnalysis CreateErrorModel(int productId, string msg)
    {
        return new ProductQuestionAnalysis
        {
            ProductId = productId,
            CommonQuestionsSummary = msg,
            CommonAnswersSummary = msg,
            AnalysisDate = DateTime.Now
        };
    }
}