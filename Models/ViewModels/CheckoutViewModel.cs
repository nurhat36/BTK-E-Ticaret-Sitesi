using BTKETicaretSitesi.Models;
using System.ComponentModel.DataAnnotations;

namespace BTKETicaretSitesi.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public ShoppingCart Cart { get; set; }

        // User addresses
        public List<Address> UserAddresses { get; set; }

        // Selected address IDs
        public int? SelectedShippingAddressId { get; set; }
        public int? SelectedBillingAddressId { get; set; }

        // Address form fields
        [Display(Name = "Teslimat Adresi")]
        public string ShippingAddress { get; set; }

        [Display(Name = "Fatura Adresi")]
        public string BillingAddress { get; set; }

        [Display(Name = "Fatura adresi teslimat adresi ile aynı")]
        public bool UseShippingAddressForBilling { get; set; } = true;

        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası girin")]
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin")]
        [Display(Name = "E-posta")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ödeme yöntemi seçin")]
        [Display(Name = "Ödeme Yöntemi")]
        public string PaymentMethod { get; set; } = "Kredi Kartı";

        // New address form (optional)
        public Address NewAddress { get; set; }
    }
}