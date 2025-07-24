using Microsoft.AspNetCore.Identity;

namespace BTKETicaretSitesi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public string ProfileImageUrl { get; set; }
        public string Slug { get; set; }
        public DateTime? LastActiveAt { get; set; }
        

    }
}
