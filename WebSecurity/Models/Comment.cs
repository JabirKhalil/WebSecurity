namespace WebSecurity.Models
{
    public class Comment
    {
        public Guid Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? Content { get; set; }
    }
}
