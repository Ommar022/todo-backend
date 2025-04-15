namespace TODO.Model
{
    public class TodoUserAssignment
    {
        public int TodoId { get; set; }
        public int UserId { get; set; }
        public bool CanEdit { get; set; }
        public DateTime AssignedAt { get; set; }
        public Todo Todo { get; set; }
        public User User { get; set; }
    }
}
