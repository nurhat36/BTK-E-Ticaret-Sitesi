using System.ComponentModel.DataAnnotations;

namespace BTKETicaretSitesi.Models.ViewModels
{
    // StoreViewModel.cs
    public class StoreViewModel
    {
        [Required(ErrorMessage = "Mağaza adı gereklidir.")]
        [Display(Name = "Mağaza Adı")]
        [StringLength(100, ErrorMessage = "Mağaza adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mağaza açıklaması gereklidir.")]
        [Display(Name = "Mağaza Açıklaması")]
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string Description { get; set; }

        [Display(Name = "Mağaza Logosu")]
        public IFormFile LogoImage { get; set; }
    }

    // StoreEditViewModel.cs
    public class StoreEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mağaza adı gereklidir.")]
        [Display(Name = "Mağaza Adı")]
        [StringLength(100, ErrorMessage = "Mağaza adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mağaza açıklaması gereklidir.")]
        [Display(Name = "Mağaza Açıklaması")]
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string Description { get; set; }

        [Display(Name = "Yeni Logo")]
        public IFormFile LogoImage { get; set; }

        public string CurrentLogoUrl { get; set; }
    }

    // StoreStatsViewModel.cs
    public class StoreStatsViewModel
    {
        public Store Store { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        // GitHub benzeri heatmap verisi
        public List<SalesActivity> SalesActivities { get; set; }

        // Aylık satış verileri
        public Dictionary<string, int> MonthlySales { get; set; }

        // En çok satan ürünler
        public List<TopProduct> TopProducts { get; set; }
    }

    public class SalesActivity
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public int ProductCount { get; set; }
    }

    public class TopProduct
    {
        public string ProductName { get; set; }
        public int SalesCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
