using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

public class QuestionAnalysisService
{
    private readonly ApplicationDbContext _context;
    private readonly GeminiQuestionAnalysisService _geminiService;

    public QuestionAnalysisService(ApplicationDbContext context, GeminiQuestionAnalysisService geminiService)
    {
        _context = context;
        _geminiService = geminiService;
    }

    public async Task AnalyzeQuestionsIfNeeded(int productId)
    {
        // Analizin son ne zaman yapıldığını kontrol et
        var lastAnalysis = await _context.ProductQuestionAnalyses
            .FirstOrDefaultAsync(a => a.ProductId == productId);

        var questionsToAnalyze = await _context.ProductQuestions
            .Where(q => q.ProductId == productId && q.IsPublished && !string.IsNullOrEmpty(q.AnswerText))
            .OrderByDescending(q => q.QuestionDate)
            .Take(100) // Son 100 soru-cevap üzerinde analiz yap
            .ToListAsync();

        if (questionsToAnalyze.Count >= 20 || (lastAnalysis == null && questionsToAnalyze.Any()))
        {
            // Yeterli sayıda soru-cevap varsa veya ilk kez analiz yapılacaksa
            var analysisResult = await _geminiService.AnalyzeQuestionsAndAnswers(questionsToAnalyze);

            // Veritabanına kaydet veya güncelle
            if (lastAnalysis != null)
            {
                lastAnalysis.CommonQuestionsSummary = analysisResult.CommonQuestionsSummary;
                lastAnalysis.CommonAnswersSummary = analysisResult.CommonAnswersSummary;
                lastAnalysis.AnalysisDate = DateTime.Now;
                _context.ProductQuestionAnalyses.Update(lastAnalysis);
            }
            else
            {
                _context.ProductQuestionAnalyses.Add(analysisResult);
            }
            await _context.SaveChangesAsync();
        }
    }
}