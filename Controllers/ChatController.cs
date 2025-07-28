using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions; // Regex için ekledik
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using GenerativeAI;
using GenerativeAI.Types;
using BTKETicaretSitesi.Models.Chat; // Kendi modellerinizin namespace'i
using BTKETicaretSitesi.Data; // ApplicationDbContext için ekledik
using BTKETicaretSitesi.Models; // Product, Category gibi modeller için ekledik
using Microsoft.EntityFrameworkCore; // ToListAsync için ekledik

namespace BTKETicaretSitesi.Controllers
{
    public class ChatController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatController> _logger;
        private readonly ApplicationDbContext _dbContext; // ApplicationDbContext'i enjekte ettik
        private const string ChatHistorySessionKey = "_ChatHistory"; // Session anahtarı

        public ChatController(IConfiguration configuration, ILogger<ChatController> logger, ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext; // Atama
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("api/chat/sendmessage")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { reply = "Mesaj boş olamaz." });
            }

            string? geminiApiKey = _configuration["GoogleAI:ApiKey"];

            if (string.IsNullOrEmpty(geminiApiKey))
            {
                _logger.LogError("Gemini API Anahtarı bulunamadı. Lütfen 'GoogleAI:ApiKey' anahtarını kontrol edin.");
                return StatusCode(500, new { reply = "API Anahtarı yapılandırılmamış." });
            }

            List<ChatMessage> chatHistory = GetChatHistoryFromSession();

            try
            {
                var googleAIClient = new GoogleAi(geminiApiKey);
                var model = googleAIClient.CreateGenerativeModel("models/gemini-1.5-flash"); // Ya da "models/gemini-pro" kullanabilirsiniz

                // Kullanıcının mesajından anahtar kelime ve bütçe bilgilerini çıkar
                var (keywords, maxBudget) = ExtractBudgetAndKeywords(request.Message);

                List<Product> relevantProducts = new List<Product>();
                string productInfoForAI = "";

                // Eğer anahtar kelime veya bütçe varsa veritabanında arama yap
                if (keywords.Any() || maxBudget.HasValue)
                {
                    _logger.LogInformation($"Ürün araması: Anahtar Kelimeler='{string.Join(", ", keywords)}', Bütçe='{maxBudget}'");
                    relevantProducts = await SearchProductsInDatabase(keywords, maxBudget);
                    productInfoForAI = FormatProductsForAI(relevantProducts);
                    _logger.LogInformation($"Veritabanından bulunan ürün sayısı: {relevantProducts.Count}");
                }

                // GenerativeAI'nin Content tipine dönüştürülmüş geçmiş
                var historyForGemini = new List<Content>();
                foreach (var msg in chatHistory)
                {
                    historyForGemini.Add(new Content { Role = msg.Role, Parts = new List<Part> { new Part { Text = msg.Text } } });
                }

                var allContents = new List<Content>();
                allContents.AddRange(historyForGemini); // Geçmişi ekle

                // AI'ya verilecek talimat ve ürün bilgileri
                string systemInstruction = "Sen bir e-ticaret asistanısın. Kullanıcının sorularına mevcut ürün bilgilerini kullanarak yanıt ver. Başka bir kaynaktan bilgi ekleme. Eğer sorulan ürün bulunamazsa veya bütçeye uygun değilse, bunu net bir şekilde belirt ve başka nasıl yardımcı olabileceğini sor.";

                if (!string.IsNullOrEmpty(productInfoForAI))
                {
                    systemInstruction += "\n\nAşağıda kullanıcının sorgusuyla eşleşen ürün bilgileri bulunmaktadır:\n" + productInfoForAI;
                }

                // Sistem talimatını ekle (Bu genellikle sohbetin başında bir kez eklenir, ancak her istekte de gönderilebilir)
                allContents.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = systemInstruction } } });

                // Kullanıcının mevcut mesajını ekle
                allContents.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = request.Message } } });

                // GenerateContentRequest nesnesi oluştur
                var generateContentRequest = new GenerateContentRequest
                {
                    Contents = allContents
                };

                var response = await model.GenerateContentAsync(generateContentRequest);

                string? botReply = response?.Text();

                if (string.IsNullOrWhiteSpace(botReply))
                {
                    botReply = "Üzgünüm, yapay zeka şu anda yanıt veremiyor. Lütfen tekrar deneyin.";
                    _logger.LogWarning("Gemini'den boş veya geçersiz yanıt alındı.");
                }
                else
                {
                    chatHistory.Add(new ChatMessage { Role = "user", Text = request.Message }); // Kullanıcının mesajını da geçmişe ekle
                    chatHistory.Add(new ChatMessage { Role = "model", Text = botReply });
                    SaveChatHistoryToSession(chatHistory); // Session'a kaydet
                }

                return Ok(new ChatResponse { Reply = botReply });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Gemini API çağrısında veya ürün aramasında bir hata oluştu: {ex.Message}", ex);
                return StatusCode(500, new { reply = "Üzgünüm, sunucu tarafında bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        // --- Yardımcı Metotlar ---

        private (List<string> keywords, decimal? maxBudget) ExtractBudgetAndKeywords(string message)
        {
            var keywords = new List<string>();
            decimal? maxBudget = null;

            // Bütçe çıkarma (örneğin "50000 TL", "50k", "50000 lira")
            var budgetMatch = Regex.Match(message, @"(\d+(\.\d+)?)\s*(tl|lira|k)?", RegexOptions.IgnoreCase);
            if (budgetMatch.Success)
            {
                if (decimal.TryParse(budgetMatch.Groups[1].Value, out decimal amount))
                {
                    // "k" veya "bin" gibi kısaltmaları kontrol et
                    if (budgetMatch.Groups[3].Success && budgetMatch.Groups[3].Value.Equals("k", StringComparison.OrdinalIgnoreCase))
                    {
                        amount *= 1000;
                    }
                    maxBudget = amount;
                }
            }

            // Anahtar kelimeleri çıkarma (basit bir yaklaşım, daha gelişmiş NLP gerekebilir)
            var commonKeywords = new[] { "bilgisayar", "telefon", "laptop", "tablet", "monitör", "klavye", "mouse", "kulaklık", "televizyon" };
            foreach (var keyword in commonKeywords)
            {
                if (message.ToLowerInvariant().Contains(keyword))
                {
                    keywords.Add(keyword);
                }
            }

            // Eğer özel bir kelime yakalayamadıysak ve bütçe varsa, "ürün" olarak genel bir arama yapabiliriz.
            if (!keywords.Any() && maxBudget.HasValue)
            {
                // Eğer bütçe varsa ama belirli bir ürün tipi yoksa, genel bir "elektronik" veya "ürün" araması yap.
                // Bu kısım, ProductCategory gibi bilgilerinizle daha detaylandırılabilir.
                // Şimdilik boş bırakalım, Gemini genel bir cevap verecektir.
            }
            // "bilgisayar", "laptop" gibi kelimeleri içeren bir kategori bulmaya çalış
            if (keywords.Contains("bilgisayar") || keywords.Contains("laptop"))
            {
                keywords.Add("bilgisayar"); // Kategoriye daha spesifik eşleşme için
            }
            else if (keywords.Contains("telefon"))
            {
                keywords.Add("telefon");
            }
            // Daha fazla kategori veya ürün tipi için burayı genişletebilirsiniz

            return (keywords, maxBudget);
        }


        private async Task<List<Product>> SearchProductsInDatabase(List<string> keywords, decimal? maxBudget)
        {
            var query = _dbContext.Products
                .Include(p => p.Category) // Kategori adını almak için
                .Include(p => p.Images) // Resim bilgilerini almak için
                .AsQueryable();

            if (keywords != null && keywords.Any())
            {
                // Anahtar kelimeleri ürün adı veya açıklamasında ara
                // Ayrıca Category.Name içinde de arama yapabiliriz
                query = query.Where(p =>
                    keywords.Any(k => p.Name.ToLower().Contains(k.ToLower()) ||
                                      p.Description.ToLower().Contains(k.ToLower()) ||
                                      (p.Category != null && p.Category.Name.ToLower().Contains(k.ToLower()))));
            }

            if (maxBudget.HasValue)
            {
                query = query.Where(p => p.Price <= maxBudget.Value);
            }

            // Sadece aktif ve stokta olan ürünleri veya belirli bir durumdaki ürünleri filtreleyebilirsiniz
            // query = query.Where(p => p.IsActive && p.StockQuantity > 0);

            // İlk 5-10 ürünü getirmek, çok fazla veri göndermemek için iyi bir strateji olabilir
            return await query.Take(10).ToListAsync();
        }

        private string FormatProductsForAI(List<Product> products)
        {
            if (!products.Any())
            {
                return "Belirtilen kriterlere uygun ürün bulunamadı.";
            }

            var productStrings = new List<string>();
            foreach (var product in products)
            {
                string imageUrl = product.Images?.FirstOrDefault()?.ImageUrl ?? "Resim yok";
                string categoryName = product.Category?.Name ?? "Kategori Bilinmiyor";

                // URL formatını güncelledik: /Urunler/{Id}/{Slug}
                // Uygulamanızın temel URL'sini otomatik alması için Url.Action veya doğrudan string birleştirme kullanabilirsiniz.
                // Eğer /Urunler/ controller/action'ınız varsa:
                string productUrl = Url.Action("Details", "SaleProducts", new { id = product.Id }, Request.Scheme);
                // VEYA daha basit bir string birleştirme:
                // string productUrl = $"/Urunler/{product.Id}/{product.Slug}";


                productStrings.Add(
                    $"Ürün Adı: {product.Name}\n" +
                    $"Fiyat: {product.Price:C} TL\n" +
                    $"Kategori: {categoryName}\n" +
                    $"Açıklama: {product.Description?.Substring(0, Math.Min(product.Description.Length, 150))}...\n" + // Açıklamayı kısalt
                    $"Stok: {product.StockQuantity}\n" +
                    
                    $"URL: {productUrl}" // Güncellenmiş URL
                );
            }

            return string.Join("\n---\n", productStrings);
        }

        private List<ChatMessage> GetChatHistoryFromSession()
        {
            var sessionString = HttpContext.Session.GetString(ChatHistorySessionKey);
            if (string.IsNullOrEmpty(sessionString))
            {
                return new List<ChatMessage>();
            }
            try
            {
                return JsonSerializer.Deserialize<List<ChatMessage>>(sessionString) ?? new List<ChatMessage>();
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Sohbet geçmişi JSON ayrıştırma hatası: {ex.Message}. Geçmiş temizleniyor.");
                HttpContext.Session.Remove(ChatHistorySessionKey);
                return new List<ChatMessage>();
            }
        }

        private void SaveChatHistoryToSession(List<ChatMessage> history)
        {
            try
            {
                // Geçmişi çok uzun tutmamak için bir limit koyabiliriz (örneğin son 10 mesaj)
                var limitedHistory = history.TakeLast(10).ToList();
                HttpContext.Session.SetString(ChatHistorySessionKey, JsonSerializer.Serialize(limitedHistory));
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Sohbet geçmişi JSON serileştirme hatası: {ex.Message}");
            }
        }
    }
}