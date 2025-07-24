namespace BTKETicaretSitesi.Models
{
    public class StoreStaff
    {
        public int StoreId { get; set; }
        public Store Store { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Role { get; set; } // "Manager", "Editor", etc.
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }
}