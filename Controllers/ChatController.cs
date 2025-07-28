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

namespace BTKETicaretSitesi.Controllers
{
    public class ChatController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private const string ChatHistorySessionKey = "_ChatHistory";

        public ChatController(IConfiguration configuration, ILogger<ChatController> logger, ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext;
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
                var model = googleAIClient.CreateGenerativeModel("models/gemini-1.5-flash");

                // Kullanıcının mesajından anahtar kelime, bütçe ve nitelik bilgilerini çıkar
                var (keywords, maxBudget, attributes) = ExtractBudgetAndKeywords(request.Message);

                List<Product> relevantProducts = new List<Product>();
                string productInfoForAI = "";

                // Eğer anahtar kelime, bütçe veya nitelik varsa veritabanında arama yap
                if (keywords.Any() || maxBudget.HasValue || attributes.Any())
                {
                    _logger.LogInformation($"Ürün araması: Anahtar Kelimeler='{string.Join(", ", keywords)}', Bütçe='{maxBudget}', Nitelikler='{string.Join(", ", attributes.Select(a => $"{a.Key}: {a.Value}"))}'");
                    relevantProducts = await SearchProductsInDatabase(keywords, maxBudget, attributes);
                    productInfoForAI = FormatProductsForAI(relevantProducts);
                    _logger.LogInformation($"Veritabanından bulunan ürün sayısı: {relevantProducts.Count}");
                }

                var historyForGemini = new List<Content>();
                foreach (var msg in chatHistory)
                {
                    historyForGemini.Add(new Content { Role = msg.Role, Parts = new List<Part> { new Part { Text = msg.Text } } });
                }

                var allContents = new List<Content>();
                allContents.AddRange(historyForGemini);

                string systemInstruction = "Sen bir e-ticaret asistanısın. Kullanıcının sorularına mevcut ürün bilgilerini kullanarak yanıt ver. Başka bir kaynaktan bilgi ekleme. Eğer sorulan ürün bulunamazsa veya bütçeye uygun değilse, bunu net bir şekilde belirt ve başka nasıl yardımcı olabileceğini sor. Ürün URL'lerini tıklanabilir HTML linkleri olarak döndür.";

                if (!string.IsNullOrEmpty(productInfoForAI))
                {
                    systemInstruction += "\n\nAşağıda kullanıcının sorgusuyla eşleşen ürün bilgileri bulunmaktadır:\n" + productInfoForAI;
                }

                allContents.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = systemInstruction } } });
                allContents.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = request.Message } } });

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
                    chatHistory.Add(new ChatMessage { Role = "user", Text = request.Message });
                    chatHistory.Add(new ChatMessage { Role = "model", Text = botReply });
                    SaveChatHistoryToSession(chatHistory);
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

        private (List<string> keywords, decimal? maxBudget, Dictionary<string, string> attributes) ExtractBudgetAndKeywords(string message)
        {
            var keywords = new List<string>();
            decimal? maxBudget = null;
            var attributes = new Dictionary<string, string>();

            // Bütçe çıkarma (örneğin "50000 TL", "50k", "50000 lira", "50.000 TL")
            var budgetMatch = Regex.Match(message, @"(\d[\d\.,]*)\s*(tl|lira|k)?", RegexOptions.IgnoreCase);
            if (budgetMatch.Success)
            {
                string amountStr = budgetMatch.Groups[1].Value.Replace(".", ""); // Binlik ayıracı kaldır
                amountStr = amountStr.Replace(",", "."); // Ondalık ayıracı düzelt
                if (decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amount))
                {
                    if (budgetMatch.Groups[2].Success && budgetMatch.Groups[2].Value.Equals("k", StringComparison.OrdinalIgnoreCase))
                    {
                        amount *= 1000;
                    }
                    maxBudget = amount;
                }
            }

            // Anahtar kelimeleri çıkarma:
            // Mesajdaki kelimeleri ayır ve özel kelimeleri/sayıları ele
            var words = Regex.Split(message.ToLowerInvariant(), @"\s+|\b")
                             .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 2 &&
                                         !decimal.TryParse(w, out _)); // Sayı olmayan ve 2 karakterden uzun kelimeler

            // Stop kelimeler (Türkçe'de yaygın bağlaçlar, edatlar vb. bu listeye eklenebilir)
            var stopWords = new HashSet<string> { "bir", "ve", "veya", "için", "olan", "ne", "kadar", "tl", "lira", "k", "fiyat", "model", "nasıl", "ben", "biraz", "çok", "bana", "gibi", "ile" };

            foreach (var word in words)
            {
                if (!stopWords.Contains(word))
                {
                    keywords.Add(word);
                }
            }

            // Eğer keywords boşsa ve bütçe yoksa, yine de mesajın kendisini bir anahtar kelime olarak ekleyebiliriz.
            // Bu, çok genel sorular için bir fallback olabilir.
            if (!keywords.Any() && !maxBudget.HasValue && !attributes.Any())
            {
                keywords.Add(message.ToLowerInvariant());
            }

            // Nitelikleri çıkarma: Daha genel bir yaklaşım deneyelim.
            // Kullanıcının belirttiği olası anahtar-değer çiftlerini yakalamaya çalışalım.
            // Örn: "renk kırmızı", "boyut L", "malzeme pamuk", "kapasite 256 GB"
            // Bu kısım ProductAttribute modelinizdeki 'Key' değerlerinizle eşleşmelidir.
            // Veritabanındaki ProductAttribute Key değerlerinizin bir listesini alabiliriz:
            // Not: Bu kısım eşzamansız (async) çalışamaz, bu yüzden geçici bir çözüm.
            // Daha iyi bir yaklaşım, bu listeyi bir Cache'te tutmak veya servisten almak olur.
            var knownAttributeKeys = _dbContext.ProductAttributes
                                             .Select(pa => pa.Key)
                                             .Distinct()
                                             .ToList();

            foreach (var key in knownAttributeKeys)
            {
                // Key'in mesajda geçip geçmediğini kontrol et
                // Regex.Escape ile özel karakterlerden kaçın
                var attrPattern = $@"{Regex.Escape(key.ToLowerInvariant())}\s*[:=]?\s*([a-z0-9\s\.-]+)";
                var attrMatch = Regex.Match(message.ToLowerInvariant(), attrPattern, RegexOptions.IgnoreCase);
                if (attrMatch.Success && attrMatch.Groups.Count > 1)
                {
                    // Yakalanan değeri al ve fazla boşlukları temizle
                    string extractedValue = attrMatch.Groups[1].Value.Trim();
                    // Eğer extractedValue bir stop kelime değilse veya boş değilse ekle
                    if (!string.IsNullOrEmpty(extractedValue) && !stopWords.Contains(extractedValue))
                    {
                        attributes[key] = extractedValue;
                        // Nitelik olarak çıkarılan kelimeyi keywords'ten çıkar (çift aramayı önlemek için)
                        keywords.RemoveAll(k => extractedValue.Contains(k) || k.Contains(extractedValue));
                    }
                }
            }

            // Daha önce kullandığımız spesifik regex'leri de koruyabiliriz, genel yaklaşıma ek olarak
            // RAM
            var ramMatch = Regex.Match(message, @"(\d+)\s*(gb|gigabyte)\s*ram", RegexOptions.IgnoreCase);
            if (ramMatch.Success) attributes["RAM"] = ramMatch.Groups[1].Value + " GB";
            // SSD/HDD
            var storageMatch = Regex.Match(message, @"(\d+)\s*(gb|tb|gigabyte|terabyte)\s*(ssd|hdd)?", RegexOptions.IgnoreCase);
            if (storageMatch.Success)
            {
                string storageType = storageMatch.Groups[3].Success ? storageMatch.Groups[3].Value.ToUpper() : "";
                if (!string.IsNullOrEmpty(storageType)) attributes["DepolamaTipi"] = storageType;
                attributes["DepolamaKapasitesi"] = storageMatch.Groups[1].Value + " " + storageMatch.Groups[2].Value.ToUpper();
            }
            // İşlemci (daha genel bir eşleşme, örn. "i7", "ryzen 5" tek başına da yakalamak için)
            var cpuGenericMatch = Regex.Match(message, @"(intel|amd)?\s*(i\d+|ryzen\s*\d+|snapdragon|mediatek|a\d+x?)", RegexOptions.IgnoreCase);
            if (cpuGenericMatch.Success)
            {
                if (cpuGenericMatch.Groups[1].Success) attributes["İşlemciMarka"] = cpuGenericMatch.Groups[1].Value;
                attributes["İşlemciModel"] = cpuGenericMatch.Groups[2].Value;
            }
            // Ekran Boyutu
            var screenSizeMatch = Regex.Match(message, @"(\d+(\.\d+)?)\s*(inç|inch)", RegexOptions.IgnoreCase);
            if (screenSizeMatch.Success) attributes["EkranBoyutu"] = screenSizeMatch.Groups[1].Value + " inç";
            // Renk (örneğin "kırmızı elbise", "siyah telefon")
            var colorMatch = Regex.Match(message, @"(kırmızı|mavi|siyah|beyaz|yeşil|sarı|pembe|gri|turuncu|mor)\s*", RegexOptions.IgnoreCase);
            if (colorMatch.Success) attributes["Renk"] = colorMatch.Groups[1].Value;


            return (keywords.Distinct().ToList(), maxBudget, attributes); // Tekrar eden kelimeleri kaldır
        }


        private async Task<List<Product>> SearchProductsInDatabase(List<string> keywords, decimal? maxBudget, Dictionary<string, string> attributes)
        {
            var query = _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Attributes) // Nitelikleri yükle
                .AsQueryable();

            if (keywords != null && keywords.Any())
            {
                // Keywords'ü hem ürün adı/açıklamasında hem de kategori adında ara
                query = query.Where(p =>
                    keywords.Any(k => p.Name.ToLower().Contains(k.ToLower()) ||
                                      p.Description.ToLower().Contains(k.ToLower()) ||
                                      (p.Category != null && p.Category.Name.ToLower().Contains(k.ToLower()))));
            }

            if (maxBudget.HasValue)
            {
                query = query.Where(p => p.Price <= maxBudget.Value);
            }

            // Niteliklere göre filtreleme
            foreach (var attr in attributes)
            {
                string attributeKey = attr.Key;
                string attributeValue = attr.Value;

                query = query.Where(p => p.Attributes.Any(pa =>
                    pa.Key.ToLower() == attributeKey.ToLower() &&
                    pa.Value.ToLower().Contains(attributeValue.ToLower())
                ));
            }

            return await query.Take(10).ToListAsync(); // İlk 10 ürünü getir
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
                string categoryName = product.Category?.Name ?? "Kategori Bilinmiyor";

                // URL oluşturma mantığı sizin uygulamanıza bağlıdır.
                // Eğer `product.Slug` varsa, onu kullanmak daha iyi bir SEO dostu URL sağlar.
                // Örneğin: Url.Action("Details", "SaleProducts", new { id = product.Id, slug = product.Slug }, Request.Scheme);
                // Slug yoksa sadece ID ile devam edebiliriz.
                string productUrl = Url.Action("Details", "SaleProducts", new { id = product.Id }, Request.Scheme);
                // Eğer Product modelinizde Slug alanı varsa ve URL'de kullanmak istiyorsanız:
                // string productUrl = Url.Action("Details", "SaleProducts", new { id = product.Id, slug = product.Slug }, Request.Scheme);
                // Veya aşağıdaki gibi sadece ID ile:
                // string productUrl = $"{Request.Scheme}://{Request.Host}/SaleProducts/Details/{product.Id}";


                // Ürün niteliklerini metin olarak ekle
                string attributeString = "";
                if (product.Attributes != null && product.Attributes.Any())
                {
                    attributeString = "Nitelikler: " + string.Join(", ", product.Attributes.Select(attr => $"{attr.Key}: {attr.Value}"));
                }

                productStrings.Add(
                    $"Ürün Adı: {product.Name}\n" +
                    $"Fiyat: {product.Price:C} TL\n" +
                    $"Kategori: {categoryName}\n" +
                    $"Açıklama: {product.Description?.Substring(0, Math.Min(product.Description.Length, 150))}...\n" +
                    $"Stok: {product.StockQuantity}\n" +
                    (string.IsNullOrEmpty(attributeString) ? "" : $"{attributeString}\n") +
                    $"URL: {productUrl}"
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