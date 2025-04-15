using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;

namespace TODO.IService
{
    public interface ITodoStatusService
    {
        Task<List<TodoStatusDTO>> GetAllTodoStatusesAsync();
        Task<TodoResponseDto> UpdateStatusAsync(int id, UpdateStatusRequestDto statusDto);
    }
}
