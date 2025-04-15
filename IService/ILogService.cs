using TODO.DTO_s.RequestDTO;
using TODO.Dtos;
using TODO.IService;

namespace TODO.IService
{
    public interface ILogService
    {
        Task<LogResponseDto> LogActionAsync(LogRequestDto request);
            }
}
