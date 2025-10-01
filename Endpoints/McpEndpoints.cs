using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using BTKETicaretSitesi.Services; // QuestionAnalysisService burada

namespace BTKETicaretSitesi.Endpoints
{
    public static class McpEndpoints
    {
        public static void MapMcpEndpoints(this WebApplication app)
        {
            // Sağlık kontrolü
            app.MapGet("/mcp/health", () => "MCP Servisi Çalışıyor!");

            // Yapay zeka soru endpoint’i
            app.MapPost("/mcp/analyze/{productId:int}", async (int productId, QuestionAnalysisService questionService) =>
            {
                // ProductId ile analiz başlat
                await questionService.AnalyzeQuestionsIfNeeded(productId);
                return Results.Ok(new { message = $"Product {productId} için analiz tamamlandı." });
            });
        }
    }

    // Opsiyonel: Model artık endpoint içinde gerekmiyor çünkü ProductId ile çalışıyoruz
}
