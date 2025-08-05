using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTKETicaretSitesi.Models
{
    // Models/Notification.cs
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        [StringLength(500)]
        public string Link { get; set; } // Yeni eklenen alan

        public NotificationType Type { get; set; } = NotificationType.Order;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public enum NotificationType
    {
        Order,
        Promotion,
        System,
        Other
    }
}
