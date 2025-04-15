namespace TODO.Model
{
    public class Log
    {
        public int Id { get; set; }
        public int? UserId { get; set; } 
        public string Action { get; set; } = string.Empty; 
        public string EntityType { get; set; } = string.Empty; 
        public int EntityId { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
        public string? Details { get; set; } 
        public User User { get; set; } 
    }
}
