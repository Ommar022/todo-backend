using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TODO.Model
{
    public class Avatar
    {
        public int Id { get; set; }

        public string Base64Image { get; set; } = null!;

        public int UserId { get; set; }

        public User User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
