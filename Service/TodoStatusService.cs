using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using TODO.Data;
using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;
using TODO.IService;
using TODO.Model;
using TODO.Services;
using TODO.Sockets;

namespace eco_m.Service
{
    public class TodoStatusService : ITodoStatusService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly WebSocketHandler _webSocketHandler;
        private readonly ILogService _logService;

        public TodoStatusService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            WebSocketHandler webSocketHandler,
            ILogService logService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _webSocketHandler = webSocketHandler;
            _logService = logService;
        }

        private int GetUserId()
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                throw new Exception("Unable to retrieve or parse user ID from token");
            }
            return userId;
        }

        public async Task<List<TodoStatusDTO>> GetAllTodoStatusesAsync()
        {
            var userId = GetUserId();
            var statuses = await _context.Set<TodoStatus>()
                .Select(s => new TodoStatusDTO
                {
                    Id = s.Id,
                    StatusName = s.StatusName
                })
                .ToListAsync();

            var logRequest = new LogRequestDto(
                userId,
                "Read",
                "TodoStatus",
                0, 
                new { Action = "Retrieved all todo statuses", Count = statuses.Count }
            );
            await _logService.LogActionAsync(logRequest);

            return statuses;
        }

        public async Task<TodoResponseDto> UpdateStatusAsync(int id, UpdateStatusRequestDto statusDto)
        {
            var userId = GetUserId();
            if (statusDto == null)
            {
                throw new ArgumentNullException(nameof(statusDto), "Status data is required");
            }

            var todo = await _context.Todos
                .Include(t => t.Status)
                .Include(t => t.TodoUserAssignments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (todo == null)
            {
                throw new KeyNotFoundException($"Todo with ID {id} not found");
            }

            bool isOwner = todo.UserId == userId;
            bool canEdit = todo.TodoUserAssignments?.Any(tua => tua.UserId == userId && tua.CanEdit) == true;

            if (!isOwner && !canEdit)
            {
                throw new UnauthorizedAccessException("You are not authorized to update the status of this todo.");
            }

            var oldStatusId = todo.StatusId; 
            todo.StatusId = statusDto.StatusId;
            _context.Entry(todo).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var response = new TodoResponseDto
            {
                Id = todo.Id,
                TaskName = todo.TaskName,
                TaskDescription = todo.TaskDescription,
                StatusId = todo.StatusId,
                StatusName = (await _context.TodoStatuses.FindAsync(todo.StatusId))?.StatusName,
                UserId = todo.UserId,
                TodoListId = todo.TodoListId,
                CreatedAt = todo.CreatedAt
            };

            var logRequest = new LogRequestDto(
                userId,
                "Update",
                "TodoStatus",
                id, 
                new
                {
                    TodoId = id,
                    TaskName = todo.TaskName,
                    OldStatusId = oldStatusId,
                    NewStatusId = statusDto.StatusId,
                    NewStatusName = response.StatusName
                }
            );
            await _logService.LogActionAsync(logRequest);

            await _webSocketHandler.BroadcastMessage(JsonConvert.SerializeObject(new
            {
                action = "status_updated",
                data = response
            }));

            return response;
        }
    }
}