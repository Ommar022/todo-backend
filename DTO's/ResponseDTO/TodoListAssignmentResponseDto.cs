namespace TODO.DTO_s.ResponseDTO
{
    public class TodoListAssignmentResponseDto
    {
        public int UserId { get; set; }
        public bool CanEdit { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
