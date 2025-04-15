namespace TODO.DTO_s.RequestDTO
{
    public class TodoRequestDto
    {
        public string TaskName { get; set; }
        public string TaskDescription { get; set; }
        public int StatusId { get; set; }
        public int TodoListId { get; set; }
        public List<int> AssignedUserIds { get; set; } = new List<int>();
    }
}
