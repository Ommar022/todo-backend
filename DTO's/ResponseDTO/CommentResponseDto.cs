namespace TODO.DTO_s.ResponseDTO
{
    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int TodoId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
