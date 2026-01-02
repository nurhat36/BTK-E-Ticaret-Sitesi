using BTKETicaretSitesi.Services;

namespace BTKETicaretSitesi.Middleware
{
    public class DdosProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DdosProtectionMiddleware> _logger;

        public DdosProtectionMiddleware(RequestDelegate next, ILogger<DdosProtectionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, DdosGuardService ddosGuard)
        {
            // 1. IP Adresini Bul
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            // Localhost ise veya IP yoksa geç (Test ortamı kolaylığı)
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
            {
                await _next(context);
                return;
            }

            // 2. IP Yasaklı mı? (KARA LİSTE KONTROLÜ)
            if (ddosGuard.IsBanned(ipAddress))
            {
                _logger.LogWarning($"⛔ YASAKLI IP ERİŞİM DENEMESİ: {ipAddress}");

                context.Response.StatusCode = 403; // Forbidden (Yasak)
                await context.Response.WriteAsync("Sisteme saldiri girisimi tespit edildi. IP adresiniz gecici olarak engellendi.");
                return; // İsteği burada kes! Controller'a gitmesin.
            }

            // 3. İsteği Say ve Limit Kontrolü Yap
            ddosGuard.CheckRequest(ipAddress);

            // 4. Sorun yoksa devam et
            await _next(context);
        }
    }
}
