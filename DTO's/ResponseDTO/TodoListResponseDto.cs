namespace TODO.DTO_s.ResponseDTO
{
    public class TodoListResponseDto
    {
        public int Id { get; set; }
        public string ListName { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TodoListAssignmentResponseDto> Assignments { get; set; }
    }
}
