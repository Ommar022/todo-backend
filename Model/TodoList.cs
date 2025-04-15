namespace TODO.Model
{
    public class TodoList
    {
        public int Id { get; set; }
        public string ListName { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<Todo> Todos { get; set; } = new List<Todo>();
        public List<TodoListUserAssignment> UserAssignments { get; set; } = new List<TodoListUserAssignment>();
    }
}
