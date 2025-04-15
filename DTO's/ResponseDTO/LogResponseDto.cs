using TODO.Model;

namespace TODO.Dtos
{
    public class LogResponseDto
    {
        public int Id { get; set; } 
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Details { get; set; }

        public LogResponseDto(Log log)
        {
            Id = log.Id;
            UserId = log.UserId ?? 0;
            Action = log.Action;
            EntityType = log.EntityType;
            EntityId = log.EntityId;
            CreatedAt = log.CreatedAt;
            Details = log.Details;
        }
    }
}