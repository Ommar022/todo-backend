using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TODO.Data;
using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;
using TODO.IService;
using TODO.Model;

namespace TODO.Service
{
    public class TodoListService : ITodoListService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogService _logService;

        public TodoListService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogService logService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
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

        public async Task<List<TodoListResponseDto>> GetAllTodoListsAsync()
        {
            var userId = GetUserId();
            var todoLists = await _context.TodoLists
                .Where(tl => tl.UserId == userId || tl.UserAssignments.Any(ua => ua.UserId == userId))
                .OrderByDescending(tl => tl.CreatedAt)
                .Select(tl => new TodoListResponseDto
                {
                    Id = tl.Id,
                    ListName = tl.ListName,
                    UserId = tl.UserId,
                    CreatedAt = tl.CreatedAt,
                    Assignments = tl.UserAssignments.Select(ua => new TodoListAssignmentResponseDto
                    {
                        UserId = ua.UserId,
                        CanEdit = ua.CanEdit,
                        AssignedAt = ua.AssignedAt
                    }).ToList()
                })
                .ToListAsync();

            var logRequest = new LogRequestDto(
                userId,
                "Read",
                "TodoList",
                0,
                new { Action = "Retrieved all todo lists", Count = todoLists.Count }
            );
            await _logService.LogActionAsync(logRequest);

            return todoLists;
        }

        public async Task<TodoListResponseDto> GetTodoListByIdAsync(int id)
        {
            var userId = GetUserId();
            var todoList = await _context.TodoLists
                .Where(tl => tl.Id == id && (tl.UserId == userId || tl.UserAssignments.Any(ua => ua.UserId == userId)))
                .Select(tl => new TodoListResponseDto
                {
                    Id = tl.Id,
                    ListName = tl.ListName,
                    UserId = tl.UserId,
                    CreatedAt = tl.CreatedAt,
                    Assignments = tl.UserAssignments.Select(ua => new TodoListAssignmentResponseDto
                    {
                        UserId = ua.UserId,
                        CanEdit = ua.CanEdit,
                        AssignedAt = ua.AssignedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync() ?? throw new Exception("Todo list not found or unauthorized");

            var logRequest = new LogRequestDto(
                userId,
                "Read",
                "TodoList",
                id,
                new { ListName = todoList.ListName }
            );
            await _logService.LogActionAsync(logRequest);

            return todoList;
        }

        public async Task<TodoListResponseDto> CreateTodoListAsync(TodoListRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.ListName))
            {
                throw new ArgumentException("ListName cannot be null or empty.", nameof(request.ListName));
            }

            var userId = GetUserId();
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                throw new InvalidOperationException($"User with ID {userId} does not exist.");
            }

            if (request.Assignments != null && request.Assignments.Any())
            {
                var invalidUserIds = request.Assignments
                    .Where(a => !_context.Users.Any(u => u.Id == a.UserId))
                    .Select(a => a.UserId)
                    .ToList();
                if (invalidUserIds.Any())
                {
                    throw new InvalidOperationException($"The following user IDs do not exist: {string.Join(", ", invalidUserIds)}");
                }
            }

            var todoList = new TodoList
            {
                ListName = request.ListName,
                UserId = userId,
                UserAssignments = request.Assignments?.Select(a => new TodoListUserAssignment
                {
                    UserId = a.UserId,
                    CanEdit = a.CanEdit,
                    AssignedAt = DateTime.UtcNow
                }).ToList() ?? new List<TodoListUserAssignment>()
            };

            try
            {
                _context.TodoLists.Add(todoList);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Failed to save TodoList: {innerException}", ex);
            }

            var response = new TodoListResponseDto
            {
                Id = todoList.Id,
                ListName = todoList.ListName,
                UserId = todoList.UserId,
                CreatedAt = todoList.CreatedAt,
                Assignments = todoList.UserAssignments.Select(ua => new TodoListAssignmentResponseDto
                {
                    UserId = ua.UserId,
                    CanEdit = ua.CanEdit,
                    AssignedAt = ua.AssignedAt
                }).ToList()
            };

            var logRequest = new LogRequestDto(
                userId,
                "Create",
                "TodoList",
                todoList.Id,
                new { ListName = request.ListName, Assignments = request.Assignments }
            );
            await _logService.LogActionAsync(logRequest);

            return response;
        }

        public async Task<TodoListResponseDto> UpdateTodoListAsync(int id, TodoListRequestDto request)
        {
            var userId = GetUserId();
            var todoList = await _context.TodoLists
                .Include(tl => tl.UserAssignments)
                .FirstOrDefaultAsync(tl => tl.Id == id && tl.UserId == userId);

            if (todoList == null)
            {
                throw new Exception("Todo list not found or unauthorized");
            }

            var oldListName = todoList.ListName;
            var oldAssignments = todoList.UserAssignments.Select(ua => new { ua.UserId, ua.CanEdit }).ToList();

            todoList.ListName = request.ListName;

            if (request.Assignments != null)
            {
                var invalidUserIds = request.Assignments
                    .Where(a => !_context.Users.Any(u => u.Id == a.UserId))
                    .Select(a => a.UserId)
                    .ToList();
                if (invalidUserIds.Any())
                {
                    throw new InvalidOperationException($"The following user IDs do not exist: {string.Join(", ", invalidUserIds)}");
                }

                var existingAssignments = await _context.TodoListUserAssignments
                    .Where(tlua => tlua.TodoListId == id)
                    .ToListAsync();
                _context.TodoListUserAssignments.RemoveRange(existingAssignments);
                await _context.SaveChangesAsync();

                var newAssignments = request.Assignments.Select(a => new TodoListUserAssignment
                {
                    TodoListId = id,
                    UserId = a.UserId,
                    CanEdit = a.CanEdit,
                    AssignedAt = DateTime.UtcNow
                });
                _context.TodoListUserAssignments.AddRange(newAssignments);
            }

            await _context.SaveChangesAsync();

            var logRequest = new LogRequestDto(
                userId,
                "Update",
                "TodoList",
                id,
                new { OldListName = oldListName, NewListName = request.ListName, OldAssignments = oldAssignments, NewAssignments = request.Assignments }
            );
            await _logService.LogActionAsync(logRequest);

            var response = new TodoListResponseDto
            {
                Id = todoList.Id,
                ListName = todoList.ListName,
                Assignments = todoList.UserAssignments.Select(ua => new TodoListAssignmentResponseDto
                {
                    UserId = ua.UserId,
                    CanEdit = ua.CanEdit,
                    AssignedAt = ua.AssignedAt
                }).ToList(),
                CreatedAt = DateTime.UtcNow
            };

            return response;
        }

        public async Task DeleteTodoListAsync(int id)
        {
            var userId = GetUserId();
            var todoList = await _context.TodoLists
                .Include(tl => tl.Todos)
                .Include(tl => tl.UserAssignments)
                .FirstOrDefaultAsync(tl => tl.Id == id &&
                    (tl.UserId == userId || 
                    tl.UserAssignments.Any(ua => ua.UserId == userId && ua.CanEdit))); 

            if (todoList == null)
            {
                throw new Exception("Todo list not found or unauthorized");
            }

            var deletedListName = todoList.ListName;
            var deletedAssignments = todoList.UserAssignments.Select(ua => new { ua.UserId, ua.CanEdit }).ToList();

            _context.TodoLists.Remove(todoList);
            await _context.SaveChangesAsync();

            var logRequest = new LogRequestDto(
                userId,
                "Delete",
                "TodoList",
                id,
                new { ListName = deletedListName, Assignments = deletedAssignments }
            );
            await _logService.LogActionAsync(logRequest);
        }
    }
}