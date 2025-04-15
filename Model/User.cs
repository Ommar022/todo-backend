using Microsoft.AspNetCore.Identity;

namespace TODO.Model
{
    public class User : IdentityUser<int>
    {
        
        public List<Todo> Todos { get; set; }


        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;

        public Avatar Avatar { get; set; }
        public List<TodoList> TodoLists { get; set; }
        public List<TodoUserAssignment> TodoAssignments { get; set; }
        public List<TodoListUserAssignment> AssignedTodoLists { get; set; } = new List<TodoListUserAssignment>();

        public List<Todo> AssignedTodos { get; set; } 
        public List<Todo> CreatedTodos { get; set; }
    }
}
