using System.Text.Json;
using TODO.Data;
using TODO.DTO_s.RequestDTO;
using TODO.Dtos;
using TODO.IService;
using TODO.Model;
using TODO.IService;

namespace TODO.Services
{
    public class LogService : ILogService
    {
        private readonly AppDbContext _context;

        public LogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LogResponseDto> LogActionAsync(LogRequestDto request)
        {
            var log = new Log
            {
                UserId = request.UserId,
                Action = request.Action,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                CreatedAt = DateTime.UtcNow,
                Details = request.Details != null ? JsonSerializer.Serialize(request.Details) : null
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            return new LogResponseDto(log);
        }
    }
}