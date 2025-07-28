using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BTKETicaretSitesi.ViewComponents // Projenizin namespace'ine göre düzenleyin
{
    public class ChatbotViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Bu component başlangıçta herhangi bir veriyle çalışmadığı için
            // doğrudan view'ı döndürüyoruz.
            return View();
        }
    }
}