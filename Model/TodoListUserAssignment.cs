namespace TODO.Model
{
    public class TodoListUserAssignment
    {
        public int TodoListId { get; set; }
        public TodoList TodoList { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public bool CanEdit { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
