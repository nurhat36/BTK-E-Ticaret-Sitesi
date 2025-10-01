using BTKETicaretSitesi.Models;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GeminiQuestionAnalysisService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiQuestionAnalysisService> _logger;

    public GeminiQuestionAnalysisService(IConfiguration configuration, ILogger<GeminiQuestionAnalysisService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ProductQuestionAnalysis> AnalyzeQuestionsAndAnswers(List<ProductQuestion> questions)
    {
        string? geminiApiKey = _configuration["GoogleAI:ApiKey"];
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            _logger.LogError("Gemini API Anahtarı bulunamadı.");
            throw new InvalidOperationException("Gemini API Anahtarı yapılandırılmamış.");
        }

        // Soruları ve yanıtları tek bir metin bloğu haline getirme
        var combinedText = string.Join("\n---\n", questions.Select(q =>
            $"Soru: {q.QuestionText}\nYanıt: {(string.IsNullOrEmpty(q.AnswerText) ? "Henüz yanıtlanmamış" : q.AnswerText)}"));

        string prompt = $@"
Aşağıdaki ürün soru-cevaplarını incele ve analiz et. İki ana başlıkta özetle. Yanıtları aşağıdaki formatta döndür:

SIK_SORULAN_SORULAR_OZETI: (Kullanıcıların en sık sorduğu konuları veya soruları maddeler halinde özetle)
SIK_VERILEN_YANITLAR_OZETI: (Satıcının bu sorulara verdiği yanıtların en çok hangi konulara odaklandığını özetle)

Soru-Cevaplar:
{combinedText}
";
        try
        {
            var googleAIClient = new GoogleAi(geminiApiKey);
            var model = googleAIClient.CreateGenerativeModel("models/gemini-2.0-flash");

            var response = await model.GenerateContentAsync(prompt);
            string analysisText = response?.Text();
            Console.WriteLine($"Soru analizi sonucu: {analysisText}");

            // Yanıtı ayrıştır
            var analysisData = ParseQuestionAnalysisResponse(analysisText);

            return new ProductQuestionAnalysis
            {
                ProductId = questions.First().ProductId,
                CommonQuestionsSummary = analysisData.CommonQuestionsSummary,
                CommonAnswersSummary = analysisData.CommonAnswersSummary,
                AnalysisDate = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Soru analizi API çağrısında hata: {ex.Message}");
            return new ProductQuestionAnalysis
            {
                ProductId = questions.First().ProductId,
                CommonQuestionsSummary = "Analiz yapılamadı.",
                CommonAnswersSummary = "Analiz yapılamadı."
            };
        }
    }

    private static ProductQuestionAnalysis ParseQuestionAnalysisResponse(string analysisText)
    {
        var result = new ProductQuestionAnalysis();
        var lines = analysisText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        bool isParsingQuestions = false;
        bool isParsingAnswers = false;
        var questionsBuilder = new System.Text.StringBuilder();
        var answersBuilder = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith("SIK_SORULAN_SORULAR_OZETI:"))
            {
                isParsingQuestions = true;
                isParsingAnswers = false;
            }
            else if (line.StartsWith("SIK_VERILEN_YANITLAR_OZETI:"))
            {
                isParsingQuestions = false;
                isParsingAnswers = true;
            }
            else if (isParsingQuestions)
            {
                questionsBuilder.AppendLine(line.Trim());
            }
            else if (isParsingAnswers)
            {
                answersBuilder.AppendLine(line.Trim());
            }
        }

        result.CommonQuestionsSummary = questionsBuilder.ToString().Trim();
        result.CommonAnswersSummary = answersBuilder.ToString().Trim();

        return result;
    }
}