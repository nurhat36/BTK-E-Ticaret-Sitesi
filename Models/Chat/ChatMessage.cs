namespace BTKETicaretSitesi.Models.Chat
{
    public class ChatMessage
    {
        public string Role { get; set; } // "user" veya "model"
        public string Text { get; set; }
    }
}
