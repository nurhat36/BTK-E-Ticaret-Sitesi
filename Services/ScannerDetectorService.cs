using Microsoft.Extensions.Caching.Memory;

namespace BTKETicaretSitesi.Services
{
    public class ScannerDetectorService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ScannerDetectorService> _logger;

        // Ayarlar
        private const int MaxErrorLimit = 100; // Dakikada 10 tane 404 hatası alırsa BANLA
        private const int BanMinutes = 1;   // 1 Saat ceza

        public ScannerDetectorService(IMemoryCache cache, ILogger<ScannerDetectorService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        // IP'nin "Sabıka Kaydını" artır
        public void RecordError(string ipAddress, int statusCode)
        {
            // Sadece 404 (Bulunamadı) ve 400 (Bad Request - SQL Injection denemeleri genelde 400 verir) takip et
            if (statusCode != 404 && statusCode != 400 || ipAddress == "::1" || ipAddress == "127.0.0.1") return;

            var cacheKey = $"ERRORS_{ipAddress}";

            var errorCount = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            errorCount++;
            _cache.Set(cacheKey, errorCount);

            if (errorCount >= MaxErrorLimit)
            {
                BanIp(ipAddress);
            }
        }

        public bool IsBanned(string ipAddress)
        {
            return _cache.TryGetValue($"BAN_{ipAddress}", out _);
        }

        private void BanIp(string ipAddress)
        {
            _cache.Set($"BAN_{ipAddress}", true, TimeSpan.FromMinutes(BanMinutes));
            _logger.LogWarning($"🚨 SCANNER TESPİT EDİLDİ! IP: {ipAddress} banlandı. (Sebep: Çok fazla hatalı istek)");
        }
    }
}
