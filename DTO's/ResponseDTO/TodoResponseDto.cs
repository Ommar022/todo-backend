namespace TODO.DTO_s.ResponseDTO
{
    public class TodoResponseDto
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public string TaskDescription { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public int UserId { get; set; }
        public int TodoListId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TodoListAssignmentResponseDto> TodoListAssignments { get; set; }
        public List<TodoUserAssignmentResponseDto> TodoAssignments { get; set; } = new List<TodoUserAssignmentResponseDto>();
    }
}
