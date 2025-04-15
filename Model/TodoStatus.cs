namespace TODO.Model
{
    public class TodoStatus
    {
        public int Id { get; set; }
        public string StatusName { get; set; }
        public List<Todo> Todos { get; set; }
    }
}
