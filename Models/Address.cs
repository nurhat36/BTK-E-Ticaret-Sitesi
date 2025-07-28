using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTKETicaretSitesi.Models
{
    public class Address
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]  // Explicitly define the foreign key
        public string UserId { get; set; }

        public virtual ApplicationUser User { get; set; }  // Add virtual for lazy loading

        [Required]
        [StringLength(100)]
        public string Title { get; set; } // "Ev Adresi", "İş Adresi" gibi

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string City { get; set; } // İl

        [Required]
        [StringLength(50)]
        public string District { get; set; } // İlçe

        [Required]
        [StringLength(50)]
        public string Neighborhood { get; set; } // Mahalle

        [Required]
        [StringLength(200)]
        public string StreetAddress { get; set; } // Sokak, cadde, apt no vb.

        [StringLength(10)]
        public string ZipCode { get; set; } // Posta kodu

        public bool IsDefault { get; set; } // Varsayılan adres mi?

        public AddressType AddressType { get; set; } // Teslimat/Fatura adresi

        [StringLength(500)]
        public string AdditionalInfo { get; set; } // Ek bilgiler
    }

    public enum AddressType
    {
        Shipping,    // Teslimat adresi
        Billing,     // Fatura adresi
        Both         // Her ikisi için
    }
}