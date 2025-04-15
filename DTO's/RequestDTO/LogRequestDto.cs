using System.ComponentModel.DataAnnotations;

namespace TODO.DTO_s.RequestDTO
{
    public class LogRequestDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public string Action { get; set; } = string.Empty;
        [Required]
        public string EntityType { get; set; } = string.Empty;
        [Required]
        public int EntityId { get; set; }
        public object? Details { get; set; }

        public LogRequestDto(int userId, string action, string entityType, int entityId, object? details = null)
        {
            UserId = userId;
            Action = action ?? throw new ArgumentNullException(nameof(action), "Action cannot be null");
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType), "EntityType cannot be null");
            EntityId = entityId;
            Details = details;
        }

        public LogRequestDto() { }
    }
}