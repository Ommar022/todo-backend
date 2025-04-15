namespace TODO.Model
{
    public class Todo
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public string TaskDescription { get; set; }
        public int StatusId { get; set; }
        public TodoStatus Status { get; set; }
        public int UserId { get; set; } 
        public User User { get; set; }
        public int? CreatorId { get; set; } 
        public User? Creator { get; set; }
        public int TodoListId { get; set; }
        public TodoList TodoList { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TodoUserAssignment> TodoUserAssignments { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
