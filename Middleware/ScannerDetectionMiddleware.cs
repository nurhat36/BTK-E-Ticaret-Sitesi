using McpService.Services;

namespace McpService.Middleware
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
            var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();

            // Hacker Araçları Listesi (Blacklist)
            var blockedAgents = new[]
            {
                     "sqlmap", "nikto", "dirbuster", "acunetix", "netsparker", "nmap", "wireshark"
            };

            if (blockedAgents.Any(a => userAgent.Contains(a)))
            {
                context.Response.StatusCode = 406; // Not Acceptable
                return; // Direkt bağlantıyı kes
            }

            // 1. GİRİŞ KONTROLÜ: Zaten banlıysa direkt reddet (403)
            if (!string.IsNullOrEmpty(ipAddress) && scannerService.IsBanned(ipAddress))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Erisim engellendi (Security Policy).");
                return;
            }

            // 2. İŞLEMİN YAPILMASINA İZİN VER
            await _next(context);

            // 3. ÇIKIŞ KONTROLÜ: İşlem bitti, sonuç ne?
            // Eğer kullanıcı olmayan sayfalara gidiyorsa (404) veya saçma istek atıyorsa (400)
            if (!string.IsNullOrEmpty(ipAddress))
            {
                scannerService.RecordError(ipAddress, context.Response.StatusCode);
            }
        }
    }
}
