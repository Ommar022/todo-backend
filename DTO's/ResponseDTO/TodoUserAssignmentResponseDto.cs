namespace TODO.DTO_s.ResponseDTO
{
    public class TodoUserAssignmentResponseDto
    {
        public int UserId { get; set; }
        public bool CanEdit { get; set; }
        public string UserName { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
