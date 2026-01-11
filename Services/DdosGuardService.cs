using Microsoft.Extensions.Caching.Memory;

namespace BTKETicaretSitesi.Services
{
    public class DdosGuardService
    {
        private readonly IMemoryCache _cache;

        // AYARLAR
        private const int RequestLimit = 1000; // Dakikada 100 istek normal
        private const int BanDurationMinutes = 1; // Cezalı süre

        public DdosGuardService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsBanned(string ipAddress)
        {
            // 1. Bu IP kara listede mi?
            return _cache.TryGetValue($"BAN_{ipAddress}", out _);
        }

        public void CheckRequest(string ipAddress)
        {
            // IP yasaklıysa işlem yapma (Zaten IsBanned ile yakalanacak)
            if (IsBanned(ipAddress)) return;

            var cacheKey = $"REQ_{ipAddress}";

            // 2. İstek sayısını al, yoksa 0'dan başlat
            var requestCount = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1); // 1 dk sonra sıfırla
                return 0;
            });

            // 3. Sayacı artır
            requestCount++;
            _cache.Set(cacheKey, requestCount);

            // 4. SALDIRI TESPİTİ! 🚨
            if (requestCount > RequestLimit)
            {
                BanIp(ipAddress);
            }
        }

        private void BanIp(string ipAddress)
        {
            // IP'yi 10 dakika boyunca "BAN_" anahtarıyla işaretle
            _cache.Set($"BAN_{ipAddress}", true, TimeSpan.FromMinutes(BanDurationMinutes));

            // Log atabilirsin: "IP {ipAddress} DDoS girişimi nedeniyle banlandı!"
            Console.WriteLine($"[DDoS GUARD] ⛔ IP BANLANDI: {ipAddress}");
        }
    }
}
