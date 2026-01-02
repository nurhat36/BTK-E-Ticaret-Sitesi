using BTKETicaretSitesi.Services;

namespace BTKETicaretSitesi.Middleware
{
    public class ScannerDetectionMiddleware
    {
        private readonly RequestDelegate _next;

        public ScannerDetectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ScannerDetectorService scannerService)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            // User-Agent boş gelebilir, null check ekleyelim patlamasın
            var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();

            // --- DÜZELTİLEN KISIM ---
            // Eğer Localhost ise (::1 veya 127.0.0.1)
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
            {
                // ÖNEMLİ: İsteğin siteye ulaşmasına izin ver!
                await _next(context);
                return; // Sonraki güvenlik kodlarını (Ban kontrolü vs.) çalıştırmadan metoddan çık.
            }
            // -------------------------

            // Hacker Araçları Listesi (Blacklist)
            var blockedAgents = new[]
            {
         "sqlmap", "nikto", "dirbuster", "acunetix", "netsparker", "nmap", "wireshark"
    };

            if (blockedAgents.Any(a => userAgent.Contains(a)))
            {
                context.Response.StatusCode = 406; // Not Acceptable
                return; // Burada _next çağırmıyoruz çünkü adamı gerçekten engellemek istiyoruz.
            }

            // 1. GİRİŞ KONTROLÜ: Zaten banlıysa direkt reddet (403)
            // NOT: Eğer ScannerDetectorService'i de Redis/Async yaptıysan başına 'await' koymalısın.
            if (!string.IsNullOrEmpty(ipAddress) && scannerService.IsBanned(ipAddress))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Erisim engellendi (Security Policy).");
                return;
            }

            // 2. İŞLEMİN YAPILMASINA İZİN VER
            await _next(context);

            // 3. ÇIKIŞ KONTROLÜ
            if (!string.IsNullOrEmpty(ipAddress))
            {
                // NOT: Eğer buraları Async yaptıysan 'await' eklemeyi unutma
                scannerService.RecordError(ipAddress, context.Response.StatusCode);
            }
        }
    }
}
