using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;

namespace TODO.IService
{
    public interface ITodoListService
    {
        Task<List<TodoListResponseDto>> GetAllTodoListsAsync();
        Task<TodoListResponseDto> GetTodoListByIdAsync(int id);
        Task<TodoListResponseDto> CreateTodoListAsync(TodoListRequestDto request);
        Task<TodoListResponseDto> UpdateTodoListAsync(int id, TodoListRequestDto request);
        Task DeleteTodoListAsync(int id);
    }
}
