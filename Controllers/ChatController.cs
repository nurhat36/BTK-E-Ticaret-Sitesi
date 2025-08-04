using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using GenerativeAI;
using GenerativeAI.Types;
using BTKETicaretSitesi.Models.Chat;
using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace BTKETicaretSitesi.Controllers
{
    public class ChatController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMemoryCache _memoryCache;
        private const string ChatHistorySessionKey = "_ChatHistory";
        private const string ProductEmbeddingsCacheKey = "_ProductEmbeddings";
        private const int MaxConversationHistory = 10;
        private const int MaxRetrievedProducts = 5;

        public ChatController(IConfiguration configuration, ILogger<ChatController> logger,
                            ApplicationDbContext dbContext, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext;
            _memoryCache = memoryCache;
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
                _logger.LogError("Gemini API Anahtarı bulunamadı.");
                return StatusCode(500, new { reply = "API Anahtarı yapılandırılmamış." });
            }

            List<ChatMessage> chatHistory = GetChatHistoryFromSession();

            try
            {
                var googleAIClient = new GoogleAi(geminiApiKey);
                var model = googleAIClient.CreateGenerativeModel("models/gemini-1.5-flash");

                // 1. Kullanıcı mesajını analiz et ve arama parametrelerini çıkar
                var searchParams = AnalyzeUserMessage(request.Message);

                // 2. RAG için ürün bilgilerini al (vektörel benzerlik + filtreleme)
                var (relevantProducts, searchExplanation) = await RetrieveRelevantProducts(searchParams);

                // 3. RAG contextini oluştur
                string ragContext = BuildRagContext(relevantProducts, searchParams, searchExplanation);

                // 4. Sistem talimatlarını ve bağlamı hazırla
                var systemPrompt = BuildSystemPrompt(ragContext);

                // 5. Gemini için mesaj tarihçesini hazırla
                var messages = PrepareMessageHistory(chatHistory, systemPrompt, request.Message);

                // 6. Gemini'ye isteği gönder
                var response = await model.GenerateContentAsync(new GenerateContentRequest { Contents = messages });

                // 7. Yanıtı işle ve geçmişi güncelle
                string botReply = ProcessBotResponse(response, chatHistory, request.Message);

                return Ok(new ChatResponse
                {
                    Reply = botReply

                   
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat işleme hatası");
                return StatusCode(500, new { reply = "Üzgünüm, bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        #region RAG Implementation

        private SearchParameters AnalyzeUserMessage(string message)
        {
            var parameters = new SearchParameters();

            // Anahtar kelime çıkarma
            var words = Regex.Split(message.ToLowerInvariant(), @"\s+|\b")
                .Where(w => w.Length > 2 && !IsStopWord(w))
                .Distinct()
                .ToList();

            parameters.Keywords = words;

            // Bütçe çıkarma
            var budgetMatch = Regex.Match(message, @"(\d[\d\.,]*)\s*(tl|lira|k)?", RegexOptions.IgnoreCase);
            if (budgetMatch.Success)
            {
                string amountStr = budgetMatch.Groups[1].Value.Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                {
                    if (budgetMatch.Groups[2].Success && budgetMatch.Groups[2].Value.Equals("k", StringComparison.OrdinalIgnoreCase))
                    {
                        amount *= 1000;
                    }
                    parameters.MaxBudget = amount;
                }
            }

            // Nitelik çıkarma
            var knownAttributes = GetKnownProductAttributes();
            foreach (var attr in knownAttributes)
            {
                var pattern = $@"{Regex.Escape(attr.ToLowerInvariant())}\s*[:=]?\s*([a-z0-9\s\.-]+)";
                var match = Regex.Match(message.ToLowerInvariant(), pattern);
                if (match.Success)
                {
                    string value = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(value)
                        && !IsStopWord(value)
                        && !parameters.Keywords.Contains(value))
                    {
                        parameters.Attributes[attr] = value;
                    }
                }
            }

            // Özel nitelik eşleştirmeleri
            ExtractSpecialAttributes(message, parameters);

            return parameters;
        }

        private async Task<(List<Product> products, string explanation)> RetrieveRelevantProducts(SearchParameters parameters)
        {
            var explanation = new StringBuilder();
            var products = new List<Product>();

            // 1. Önce tam eşleşmeleri ara (filtreleme)
            var filteredProducts = await SearchProductsWithFilters(parameters);
            explanation.AppendLine($"Filtreleme sonucu {filteredProducts.Count} ürün bulundu.");

            if (filteredProducts.Count >= MaxRetrievedProducts)
            {
                products = filteredProducts.Take(MaxRetrievedProducts).ToList();
                explanation.AppendLine($"İlk {MaxRetrievedProducts} ürün seçildi.");
                return (products, explanation.ToString());
            }

            // 2. Vektörel benzerlik araması yap (basit bir keyword eşleştirme simülasyonu)
            if (parameters.Keywords.Any())
            {
                var semanticProducts = await SearchProductsWithSemanticMatch(parameters);
                explanation.AppendLine($"Semantik arama sonucu {semanticProducts.Count} ek ürün bulundu.");

                // Benzersiz ürünleri birleştir
                var uniqueProducts = filteredProducts
                    .Union(semanticProducts)
                    .DistinctBy(p => p.Id)
                    .Take(MaxRetrievedProducts)
                    .ToList();

                products = uniqueProducts;
            }
            else
            {
                products = filteredProducts;
            }

            return (products, explanation.ToString());
        }

        private async Task<List<Product>> SearchProductsWithFilters(SearchParameters parameters)
        {
            var query = _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Attributes)
                .AsQueryable();

            // Anahtar kelime filtreleme
            if (parameters.Keywords.Any())
            {
                query = query.Where(p =>
                    parameters.Keywords.Any(k =>
                        p.Name.ToLower().Contains(k) ||
                        p.Description.ToLower().Contains(k) ||
                        (p.Category != null && p.Category.Name.ToLower().Contains(k)))
                );
            }

            // Bütçe filtreleme
            if (parameters.MaxBudget.HasValue)
            {
                query = query.Where(p => p.Price <= parameters.MaxBudget.Value);
            }

            // Nitelik filtreleme
            foreach (var attr in parameters.Attributes)
            {
                query = query.Where(p => p.Attributes.Any(pa =>
                    pa.Key.ToLower() == attr.Key.ToLower() &&
                    pa.Value.ToLower().Contains(attr.Value.ToLower())
                ));
            }

            return await query.Take(MaxRetrievedProducts * 2).ToListAsync();
        }

        private async Task<List<Product>> SearchProductsWithSemanticMatch(SearchParameters parameters)
        {
            // Gerçek bir vektörel arama yerine basit bir benzerlik araması
            // Üretim ortamında Pinecone, Weaviate gibi bir vektör veritabanı kullanılmalı

            var allProducts = await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Attributes)
                .ToListAsync();

            // Basit bir puanlama mekanizması
            var scoredProducts = allProducts.Select(p => new
            {
                Product = p,
                Score = CalculateProductScore(p, parameters)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(MaxRetrievedProducts)
            .Select(x => x.Product)
            .ToList();

            return scoredProducts;
        }

        private int CalculateProductScore(Product product, SearchParameters parameters)
        {
            int score = 0;

            // Anahtar kelime eşleşmeleri
            foreach (var keyword in parameters.Keywords)
            {
                if (product.Name.ToLower().Contains(keyword)) score += 3;
                if (product.Description.ToLower().Contains(keyword)) score += 1;
                if (product.Category?.Name.ToLower().Contains(keyword) == true) score += 2;
            }

            // Nitelik eşleşmeleri
            foreach (var attr in parameters.Attributes)
            {
                var productAttr = product.Attributes.FirstOrDefault(a =>
                    a.Key.ToLower() == attr.Key.ToLower() &&
                    a.Value.ToLower().Contains(attr.Value.ToLower()));

                if (productAttr != null) score += 2;
            }

            // Bütçe yakınlığı (bütçe belirtilmişse)
            if (parameters.MaxBudget.HasValue && product.Price > 0)
            {
                decimal ratio = product.Price / parameters.MaxBudget.Value;
                if (ratio <= 1) score += 5; // Bütçe içinde
                else if (ratio <= 1.2m) score += 3; // Bütçeye yakın
                else if (ratio <= 1.5m) score += 1; // Biraz üzerinde
            }

            return score;
        }

        private string BuildRagContext(List<Product> products, SearchParameters parameters, string searchExplanation)
        {
            if (!products.Any())
            {
                return "Kullanıcının arama kriterlerine uygun ürün bulunamadı. " +
                       "Kullanıcıya ürün bulunamadığını ancak farklı kriterlerle arama yapabileceğini söyleyin.";
            }

            var context = new StringBuilder();
            context.AppendLine("Kullanıcının arama kriterleri:");
            context.AppendLine($"- Anahtar kelimeler: {string.Join(", ", parameters.Keywords)}");
            if (parameters.MaxBudget.HasValue)
                context.AppendLine($"- Maksimum bütçe: {parameters.MaxBudget.Value:C} TL");
            if (parameters.Attributes.Any())
                context.AppendLine($"- Nitelikler: {string.Join(", ", parameters.Attributes.Select(a => $"{a.Key}={a.Value}"))}");

            context.AppendLine("\nArama sonucunda bulunan ürünler:");

            foreach (var product in products)
            {
                string productUrl = Url.Action("Details", "SaleProducts", new { id = product.Id }, Request.Scheme);
                string attributes = product.Attributes != null ?
                    string.Join(", ", product.Attributes.Select(a => $"{a.Key}: {a.Value}")) : "Yok";

                context.AppendLine($@"
Ürün Adı: {product.Name}
Fiyat: {product.Price:C} TL
Kategori: {product.Category?.Name ?? "Bilinmiyor"}
Nitelikler: {attributes}
URL: {productUrl}
---");
            }

            if (_configuration["Environment"] == "Development")
            {
                context.AppendLine($"\n[DEBUG] Arama detayları: {searchExplanation}");
            }

            return context.ToString();
        }

        #endregion

        #region Helper Methods

        private List<string> GetKnownProductAttributes()
        {
            return _memoryCache.GetOrCreate("KnownAttributes", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return _dbContext.ProductAttributes
                    .Select(pa => pa.Key)
                    .Distinct()
                    .ToList();
            });
        }

        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string> {
                "bir", "ve", "veya", "için", "olan", "ne", "kadar",
                "tl", "lira", "k", "fiyat", "model", "nasıl", "ben",
                "biraz", "çok", "bana", "gibi", "ile", "bu", "şu"
            };
            return stopWords.Contains(word.ToLowerInvariant());
        }

        private void ExtractSpecialAttributes(string message, SearchParameters parameters)
        {
            // RAM
            var ramMatch = Regex.Match(message, @"(\d+)\s*(gb|gigabyte)\s*ram", RegexOptions.IgnoreCase);
            if (ramMatch.Success) parameters.Attributes["RAM"] = ramMatch.Groups[1].Value + " GB";

            // Depolama
            var storageMatch = Regex.Match(message, @"(\d+)\s*(gb|tb|gigabyte|terabyte)\s*(ssd|hdd)?", RegexOptions.IgnoreCase);
            if (storageMatch.Success)
            {
                string storageType = storageMatch.Groups[3].Success ? storageMatch.Groups[3].Value.ToUpper() : "";
                if (!string.IsNullOrEmpty(storageType)) parameters.Attributes["DepolamaTipi"] = storageType;
                parameters.Attributes["DepolamaKapasitesi"] = storageMatch.Groups[1].Value + " " + storageMatch.Groups[2].Value.ToUpper();
            }

            // İşlemci
            var cpuMatch = Regex.Match(message, @"(intel|amd)?\s*(i\d+|ryzen\s*\d+|snapdragon|mediatek|a\d+x?)", RegexOptions.IgnoreCase);
            if (cpuMatch.Success)
            {
                if (cpuMatch.Groups[1].Success) parameters.Attributes["İşlemciMarka"] = cpuMatch.Groups[1].Value;
                parameters.Attributes["İşlemciModel"] = cpuMatch.Groups[2].Value;
            }

            // Ekran boyutu
            var screenMatch = Regex.Match(message, @"(\d+(\.\d+)?)\s*(inç|inch)", RegexOptions.IgnoreCase);
            if (screenMatch.Success) parameters.Attributes["EkranBoyutu"] = screenMatch.Groups[1].Value + " inç";

            // Renk
            var colorMatch = Regex.Match(message, @"(kırmızı|mavi|siyah|beyaz|yeşil|sarı|pembe|gri|turuncu|mor)\b", RegexOptions.IgnoreCase);
            if (colorMatch.Success) parameters.Attributes["Renk"] = colorMatch.Groups[1].Value;
        }

        private string BuildSystemPrompt(string ragContext)
        {
            return $@"
Sen bir e-ticaret asistanısın. Kullanıcıların ürünler hakkında sorularını yanıtlıyorsun. 
Aşağıdaki bilgileri kullanarak kullanıcının sorusuna yanıt ver:

1. Yalnızca aşağıda verilen ürün bilgilerini kullan. Eğer sorunun yanıtı bu bilgilerde yoksa, 
   'Ürün bilgilerimde bu konuda bir detay bulunmuyor' şeklinde yanıt ver.

2. Ürün URL'lerini her zaman tıklanabilir HTML linkleri olarak göster:
   <a href='URL' target='_blank'>Ürün Sayfası</a>

3. Eğer uygun ürün yoksa, kullanıcıya bunu kibarca bildir ve farklı kriterler önermekten çekinme.

4. Ürün karşılaştırması yapıyorsan, önemli özellikleri (fiyat, temel özellikler) tablo halinde göster.

Kullanılabilir Ürün Bilgileri:
{ragContext}
";
        }

        private List<Content> PrepareMessageHistory(List<ChatMessage> chatHistory, string systemPrompt, string userMessage)
        {
            var messages = new List<Content>();

            // Sistem talimatını ekle
            messages.Add(new Content
            {
                Role = "user",
                Parts = new List<Part> { new Part { Text = systemPrompt } }
            });

            // Sohbet geçmişini ekle (model ve kullanıcı mesajları)
            foreach (var msg in chatHistory)
            {
                messages.Add(new Content
                {
                    Role = msg.Role,
                    Parts = new List<Part> { new Part { Text = msg.Text } }
                });
            }

            // Son kullanıcı mesajını ekle
            messages.Add(new Content
            {
                Role = "user",
                Parts = new List<Part> { new Part { Text = userMessage } }
            });

            return messages;
        }

        private string ProcessBotResponse(GenerateContentResponse response, List<ChatMessage> chatHistory, string userMessage)
        {
            string botReply = response?.Text()?.Trim() ?? "Üzgünüm, yanıt oluşturulamadı. Lütfen tekrar deneyin.";

            // HTML linkleri işle (güvenlik için temel sanitize)
            botReply = Regex.Replace(botReply, @"<a\s+href=""(.*?)""", match =>
            {
                var url = match.Groups[1].Value;
                return url.StartsWith("http") ? match.Value : "<a href=\"#\"";
            });

            // Geçmişi güncelle
            chatHistory.Add(new ChatMessage { Role = "user", Text = userMessage });
            chatHistory.Add(new ChatMessage { Role = "model", Text = botReply });

            // Geçmişi sınırla ve kaydet
            if (chatHistory.Count > MaxConversationHistory * 2)
            {
                chatHistory = chatHistory.TakeLast(MaxConversationHistory * 2).ToList();
            }
            SaveChatHistoryToSession(chatHistory);

            return botReply;
        }

        private List<ChatMessage> GetChatHistoryFromSession()
        {
            var sessionString = HttpContext.Session.GetString(ChatHistorySessionKey);
            if (string.IsNullOrEmpty(sessionString)) return new List<ChatMessage>();

            try
            {
                return JsonSerializer.Deserialize<List<ChatMessage>>(sessionString) ?? new List<ChatMessage>();
            }
            catch
            {
                return new List<ChatMessage>();
            }
        }

        private void SaveChatHistoryToSession(List<ChatMessage> history)
        {
            try
            {
                HttpContext.Session.SetString(ChatHistorySessionKey, JsonSerializer.Serialize(history));
            }
            catch
            {
                // Session hatası durumunda geçmişi kaydetme
            }
        }

        #endregion

        #region Supporting Classes

        private class SearchParameters
        {
            public List<string> Keywords { get; set; } = new List<string>();
            public decimal? MaxBudget { get; set; }
            public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        }

        #endregion
    }
}