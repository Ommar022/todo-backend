namespace TODO.DTO_s.RequestDTO
{
    public class TodoListRequestDto
    {
        public string ListName { get; set; }
        public List<TodoListAssignmentRequestDto> Assignments { get; set; }
    }
}
