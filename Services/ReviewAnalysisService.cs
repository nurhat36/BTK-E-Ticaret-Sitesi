
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Hangfire;
namespace BTKETicaretSitesi.Services
{
    

    public class ReviewAnalysisService
    {
        private readonly ApplicationDbContext _context;
        // Gemini API'sine çağrı yapacak servis
        private readonly GeminiApiService _geminiApiService;

        public ReviewAnalysisService(ApplicationDbContext context, GeminiApiService geminiApiService)
        {
            _context = context;
            _geminiApiService = geminiApiService;
        }

        // Bu metot, her yeni yorum eklendiğinde çağrılacak
        public async Task AnalyzeReviewsIfNeeded(int productId)
        {
            // 1. Ürün için en son ne zaman analiz yapıldığını bul
            var lastAnalysis = await _context.ProductReviewAnalyses
                                             .Where(a => a.ProductId == productId)
                                             .OrderByDescending(a => a.AnalysisDate)
                                             .FirstOrDefaultAsync();

            // 2. En son analiz tarihinden sonra gelen yeni yorumları say
            var lastAnalysisDate = lastAnalysis?.AnalysisDate ?? DateTime.MinValue;

            var newReviewCount = await _context.ProductReviews
                                               .Where(r => r.ProductId == productId && r.ReviewDate > lastAnalysisDate)
                                               .CountAsync();

            // 3. 30 yoruma ulaşıldıysa veya ilk kez analiz yapılacaksa
            if (newReviewCount % 30==0 || lastAnalysis == null)
            {
                BackgroundJob.Enqueue<ReviewAnalysisService>(x => x.RunBackgroundAnalysisAsync(productId));
            }
        }
        public async Task RunBackgroundAnalysisAsync(int productId)
        {
            // 1. Verileri arka planda tekrar çekiyoruz (Çünkü önceki Context kapanmış olabilir)
            // Yeni bir scope oluşturulması gerekebilir ama Hangfire bunu genelde halleder.
            var reviewsToAnalyze = await _context.ProductReviews
                                                 .Where(r => r.ProductId == productId)
                                                 .OrderByDescending(r => r.ReviewDate)
                                                 .Take(30)
                                                 .ToListAsync();

            if (!reviewsToAnalyze.Any()) return;

            // 2. Gemini API çağrısı (McpService üzerinden)
            var analysisResult = await _geminiApiService.AnalyzeReviews(reviewsToAnalyze);

            // 3. Kaydetme işlemi
            await SaveAnalysisResult(productId, analysisResult);
        }

        // Bu metot, Gemini'den gelen yanıtı veritabanına kaydetmek için kullanılacak
        private async Task SaveAnalysisResult(int productId, ProductReviewAnalysis analysis)
        {
            // Varolan bir kayıt varsa güncelle
            var existingAnalysis = await _context.ProductReviewAnalyses
                                                 .FirstOrDefaultAsync(a => a.ProductId == productId);

            if (existingAnalysis != null)
            {
                existingAnalysis.SentimentAnalysisResult = analysis.SentimentAnalysisResult;
                existingAnalysis.QualityScore = analysis.QualityScore;
                existingAnalysis.SummaryInsights = analysis.SummaryInsights;
                existingAnalysis.AnalysisDate = DateTime.Now;
                _context.ProductReviewAnalyses.Update(existingAnalysis);
            }
            else // Yoksa yeni bir kayıt ekle
            {
                _context.ProductReviewAnalyses.Add(analysis);
            }

            await _context.SaveChangesAsync();
        }
    }
}
