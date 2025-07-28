using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTKETicaretSitesi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public string ProfileImageUrl { get; set; }
        public string Slug { get; set; }
        public DateTime? LastActiveAt { get; set; }

        public virtual ICollection<Address> Addresses { get; set; }

        [ForeignKey("DefaultShippingAddress")]
        public int? DefaultShippingAddressId { get; set; }
        public virtual Address DefaultShippingAddress { get; set; }

        [ForeignKey("DefaultBillingAddress")]
        public int? DefaultBillingAddressId { get; set; }
        public virtual Address DefaultBillingAddress { get; set; }


    }
}
