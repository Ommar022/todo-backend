using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;
using TODO.Model;

namespace TODO.IService
{
    public interface ITodoService
    {
        Task<TodoResponseDto> GetTodoByIdAsync(int id);
        Task<List<TodoResponseDto>> GetTodosByTodoListIdAsync(int todoListId);
        Task<TodoResponseDto> CreateTodoAsync(TodoRequestDto todoDto);
        Task<TodoResponseDto> UpdateTodoAsync(int id, TodoRequestDto todoDto);
        Task DeleteTodoAsync(int id);
        Task<List<UserDTO>> GetAssignableUsersForTodoListAsync(int todoListId);
    }
}
