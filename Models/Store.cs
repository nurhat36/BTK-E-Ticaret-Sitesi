using Iyzipay.Model.V2.Subscription;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTKETicaretSitesi.Models
{
    public class Store
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public string LogoUrl { get; set; }

        public string BannerUrl { get; set; }

        [Required]
        public string Slug { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [Required]
        public bool IsApproved { get; set; } = false;

        // Mağaza sahibi
        [Required]
        public string OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public ApplicationUser Owner { get; set; }

        // Mağaza çalışanları (opsiyonel)
        public ICollection<StoreStaff> Staff { get; set; }

        // Mağaza ile ilişkili ürünler
        public ICollection<Product> Products { get; set; }

    }
}