namespace TODO.DTO_s.ResponseDTO
{
    public class TodoAssignmentResponseDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        //public string? AvatarUrl { get; set; }  
        public bool CanEdit { get; set; }
        public DateTime AssignedAt { get; set; }
        
    }
}
