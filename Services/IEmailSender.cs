using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace BTKETicaretSitesi.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Buraya gerçek e-posta gönderme kodu yazabilirsin.
            // Şimdilik boş geçiyoruz.
            return Task.CompletedTask;
        }
    }
}
