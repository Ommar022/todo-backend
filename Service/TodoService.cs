using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using TODO.Data;
using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;
using TODO.IService;
using TODO.Model;
using TODO.Sockets;

namespace TODO.Service
{
    public class TodoService : ITodoService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly WebSocketHandler _webSocketHandler;
        private readonly ILogService _logService;

        public TodoService(
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
                throw new UnauthorizedAccessException("Unable to retrieve or parse user ID from token");
            }
            return userId;
        }

        public async Task<TodoResponseDto> GetTodoByIdAsync(int id)
        {
            var userId = GetUserId();
            var todo = await _context.Todos
                .Include(t => t.Status)
                .Include(t => t.TodoList).ThenInclude(tl => tl.UserAssignments)
                .Where(t => t.Id == id && t.UserId == userId)
                .Select(t => new TodoResponseDto
                {
                    Id = t.Id,
                    TaskName = t.TaskName,
                    TaskDescription = t.TaskDescription,
                    StatusId = t.StatusId,
                    StatusName = t.Status.StatusName,
                    UserId = t.UserId,
                    TodoListId = t.TodoListId,
                    CreatedAt = t.CreatedAt,
                    TodoListAssignments = t.TodoList.UserAssignments.Select(ua => new TodoListAssignmentResponseDto
                    {
                        UserId = ua.UserId,
                        CanEdit = ua.CanEdit,
                        AssignedAt = ua.AssignedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (todo == null)
                throw new KeyNotFoundException("Todo not found or unauthorized");

            var logRequest = new LogRequestDto(
                userId,
                "Read",
                "Todo",
                id,
                new { TaskName = todo.TaskName, TodoListId = todo.TodoListId }
            );
            await _logService.LogActionAsync(logRequest);

            return todo;
        }

        public async Task<List<TodoResponseDto>> GetTodosByTodoListIdAsync(int todoListId)
        {
            var userId = GetUserId();

            var todoListExists = await _context.TodoLists.AnyAsync(tl => tl.Id == todoListId);
            if (!todoListExists)
            {
                throw new KeyNotFoundException($"Todo list with ID {todoListId} not found");
            }

            var todos = await _context.Todos
                .Include(t => t.Status)
                .Include(t => t.TodoList).ThenInclude(tl => tl.UserAssignments)
                .Include(t => t.TodoUserAssignments).ThenInclude(tua => tua.User)
                .Where(t => t.TodoListId == todoListId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TodoResponseDto
                {
                    Id = t.Id,
                    TaskName = t.TaskName,
                    TaskDescription = t.TaskDescription,
                    StatusId = t.StatusId,
                    StatusName = t.Status.StatusName,
                    UserId = t.UserId,
                    TodoListId = t.TodoListId,
                    CreatedAt = t.CreatedAt,
                    TodoListAssignments = t.TodoList.UserAssignments.Select(ua => new TodoListAssignmentResponseDto
                    {
                        UserId = ua.UserId,
                        CanEdit = ua.CanEdit,
                        AssignedAt = ua.AssignedAt
                    }).ToList(),
                    TodoAssignments = t.TodoUserAssignments.Select(tua => new TodoUserAssignmentResponseDto
                    {
                        UserId = tua.UserId,
                        CanEdit = tua.CanEdit,
                        UserName = tua.User.UserName,
                        AssignedAt = tua.AssignedAt
                    }).ToList()
                })
                .ToListAsync();

            var logRequest = new LogRequestDto(
                userId,
                "Read",
                "Todo",
                todoListId,
                new { Action = "Retrieved todos by TodoListId", TodoListId = todoListId, Count = todos.Count }
            );
            await _logService.LogActionAsync(logRequest);

            return todos;
        }

        public async Task<TodoResponseDto> CreateTodoAsync(TodoRequestDto todoDto)
        {
            var userId = GetUserId();

            var todoList = await _context.TodoLists
                .Include(tl => tl.UserAssignments)
                .ThenInclude(ua => ua.User)
                .FirstOrDefaultAsync(tl => tl.Id == todoDto.TodoListId);
            if (todoList == null)
            {
                throw new KeyNotFoundException("Todo list not found");
            }

            if (todoList.UserId != userId && !todoList.UserAssignments.Any(ua => ua.UserId == userId))
            {
                throw new UnauthorizedAccessException("You are not authorized to create a todo in this list.");
            }

            var status = await _context.TodoStatuses
                .FirstOrDefaultAsync(s => s.Id == todoDto.StatusId);
            if (status == null)
            {
                throw new KeyNotFoundException($"Status with ID {todoDto.StatusId} not found");
            }

            var assignedUsers = await GetTodoListAssignmentsAsync(todoDto.TodoListId);
            var assignedUserIds = assignedUsers.Select(au => au.UserId).ToList();
            var invalidAssignments = todoDto.AssignedUserIds
                .Where(id => !assignedUserIds.Contains(id))
                .ToList();
            if (invalidAssignments.Any())
            {
                throw new InvalidOperationException($"The following user IDs are not assigned to the TodoList: {string.Join(", ", invalidAssignments)}");
            }

            var todo = new Todo
            {
                TaskName = todoDto.TaskName,
                TaskDescription = todoDto.TaskDescription,
                StatusId = todoDto.StatusId,
                UserId = userId,
                CreatorId = userId,
                TodoListId = todoDto.TodoListId,
                CreatedAt = DateTime.UtcNow,
                TodoUserAssignments = todoDto.AssignedUserIds.Select(assignedUserId => new TodoUserAssignment
                {
                    UserId = assignedUserId,
                    CanEdit = true,
                    AssignedAt = DateTime.UtcNow
                }).ToList()
            };

            _context.Todos.Add(todo);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Failed to save todo: {innerException}");
            }

            // Reload the todo with related data for the response and notification
            await _context.Entry(todo)
                .Collection(t => t.TodoUserAssignments)
                .Query()
                .Include(tua => tua.User)
                .LoadAsync();

            var response = await BuildTodoResponseAsync(todo);

            var logRequest = new LogRequestDto(
                userId,
                "Create",
                "Todo",
                todo.Id,
                new
                {
                    TaskName = todo.TaskName,
                    TaskDescription = todo.TaskDescription,
                    StatusId = todo.StatusId,
                    TodoListId = todo.TodoListId,
                    CreatorId = todo.CreatorId,
                    AssignedUserIds = todoDto.AssignedUserIds
                }
            );
            await _logService.LogActionAsync(logRequest);

            // Real-time update to all connected clients
            var updateMessage = JsonConvert.SerializeObject(new
            {
                action = "todo_created",
                data = response
            });
            await _webSocketHandler.SendUpdateAsync(updateMessage);

            // Send notifications to assigned users
            foreach (var assignedUserId in todoDto.AssignedUserIds)
            {
                var notificationData = new
                {
                    TodoId = todo.Id,
                    TaskName = todo.TaskName,
                    TaskDescription = todo.TaskDescription,
                    StatusId = todo.StatusId,
                    StatusName = status.StatusName,
                    TodoListId = todo.TodoListId,
                    CreatedAt = todo.CreatedAt.ToString("o"), // ISO 8601 format
                    Creator = new
                    {
                        UserId = userId,
                        UserName = _context.Users.FirstOrDefault(u => u.Id == userId)?.UserName ?? "Unknown",
                        AvatarUrl = "" // Add logic to fetch avatar if available
                    },
                    Assignment = new
                    {
                        UserId = assignedUserId,
                        CanEdit = true,
                        AssignedAt = DateTime.UtcNow.ToString("o"), // ISO 8601 format
                        UserName = todo.TodoUserAssignments
                            .FirstOrDefault(tua => tua.UserId == assignedUserId)?.User?.UserName ?? "Unknown"
                    }
                };

                string message = JsonConvert.SerializeObject(notificationData);
                await _webSocketHandler.SendMessageToUserAsync(assignedUserId.ToString(), message);
            }

            return response;
        }

        public async Task<TodoResponseDto> UpdateTodoAsync(int id, TodoRequestDto todoDto)
        {
            var userId = GetUserId();
            var todo = await _context.Todos
                .Include(t => t.Status)
                .Include(t => t.TodoList).ThenInclude(tl => tl.UserAssignments)
                .Include(t => t.TodoUserAssignments).ThenInclude(tua => tua.User)
                .FirstOrDefaultAsync(t => t.Id == id) ?? throw new KeyNotFoundException($"Todo with ID {id} not found");

            var isCreator = todo.CreatorId == userId;
            var canEdit = todo.TodoUserAssignments.Any(tua => tua.UserId == userId && tua.CanEdit);
            if (!isCreator && !canEdit)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this todo.");
            }

            var oldTodo = new { todo.TaskName, todo.TaskDescription, todo.StatusId, todo.TodoListId, AssignedUserIds = todo.TodoUserAssignments.Select(tua => tua.UserId).ToList() };

            var assignedUsers = await GetTodoListAssignmentsAsync(todo.TodoListId);
            var assignedUserIds = assignedUsers.Select(au => au.UserId).ToList();
            var invalidAssignments = todoDto.AssignedUserIds
                .Where(id => !assignedUserIds.Contains(id))
                .ToList();
            if (invalidAssignments.Any())
            {
                throw new InvalidOperationException($"The following user IDs are not assigned to the TodoList: {string.Join(", ", invalidAssignments)}");
            }

            bool hasChanges = UpdateTodoFields(todo, todoDto);
            bool assignmentChanged = false;
            List<int> newlyAssignedUserIds = new List<int>();

            if (todoDto.AssignedUserIds != null)
            {
                var existingAssignments = todo.TodoUserAssignments.ToList();
                var existingUserIds = existingAssignments.Select(tua => tua.UserId).ToList();
                var newUserIds = todoDto.AssignedUserIds;

                if (!existingUserIds.SequenceEqual(newUserIds))
                {
                    // Identify newly assigned users
                    newlyAssignedUserIds = newUserIds.Except(existingUserIds).ToList();
                    assignmentChanged = true;

                    _context.TodoUserAssignments.RemoveRange(existingAssignments);

                    var newAssignments = todoDto.AssignedUserIds.Select(userId => new TodoUserAssignment
                    {
                        TodoId = id,
                        UserId = userId,
                        CanEdit = true,
                        AssignedAt = DateTime.UtcNow
                    });
                    _context.TodoUserAssignments.AddRange(newAssignments);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                _context.Entry(todo).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();

                    await _context.Entry(todo).ReloadAsync();
                    await _context.Entry(todo)
                        .Collection(t => t.TodoUserAssignments)
                        .Query()
                        .Include(tua => tua.User)
                        .LoadAsync();
                }
                catch (DbUpdateException ex)
                {
                    var innerException = ex.InnerException?.Message ?? ex.Message;
                    throw new Exception($"Failed to update todo: {innerException}");
                }

                var logRequest = new LogRequestDto(
                    userId,
                    "Update",
                    "Todo",
                    id,
                    new
                    {
                        OldValues = oldTodo,
                        NewValues = new
                        {
                            TaskName = todo.TaskName,
                            TaskDescription = todo.TaskDescription,
                            StatusId = todo.StatusId,
                            TodoListId = todo.TodoListId,
                            AssignedUserIds = todoDto.AssignedUserIds
                        }
                    }
                );
                await _logService.LogActionAsync(logRequest);
            }

            var response = await BuildTodoResponseAsync(todo);

            if (hasChanges)
            {
                // Real-time update to all connected clients
                var updateMessage = JsonConvert.SerializeObject(new
                {
                    action = "todo_updated",
                    data = response
                });
                await _webSocketHandler.SendUpdateAsync(updateMessage);

                // Send notifications to newly assigned users
                if (assignmentChanged && newlyAssignedUserIds.Any())
                {
                    var status = await _context.TodoStatuses.FindAsync(todo.StatusId);
                    foreach (var assignedUserId in newlyAssignedUserIds)
                    {
                        var notificationData = new
                        {
                            TodoId = todo.Id,
                            TaskName = todo.TaskName,
                            TaskDescription = todo.TaskDescription,
                            StatusId = todo.StatusId,
                            StatusName = status?.StatusName ?? "Unknown",
                            TodoListId = todo.TodoListId,
                            CreatedAt = todo.CreatedAt.ToString("o"), // ISO 8601 format
                            Creator = new
                            {
                                UserId = todo.CreatorId,
                                UserName = _context.Users.FirstOrDefault(u => u.Id == todo.CreatorId)?.UserName ?? "Unknown",
                                AvatarUrl = "" // Add logic to fetch avatar if available
                            },
                            Assignment = new
                            {
                                UserId = assignedUserId,
                                CanEdit = true,
                                AssignedAt = todo.TodoUserAssignments
                                    .FirstOrDefault(tua => tua.UserId == assignedUserId)?.AssignedAt.ToString("o") ?? DateTime.UtcNow.ToString("o"),
                                UserName = todo.TodoUserAssignments
                                    .FirstOrDefault(tua => tua.UserId == assignedUserId)?.User?.UserName ?? "Unknown"
                            }
                        };

                        string message = JsonConvert.SerializeObject(notificationData);
                        await _webSocketHandler.SendMessageToUserAsync(assignedUserId.ToString(), message);
                    }
                }
            }

            return response;
        }

        public async Task DeleteTodoAsync(int id)
        {
            var userId = GetUserId();
            var todo = await _context.Todos
                .Include(t => t.TodoUserAssignments)
                .FirstOrDefaultAsync(t => t.Id == id) ?? throw new KeyNotFoundException($"Todo with ID {id} not found");

            var isCreator = todo.CreatorId == userId;
            var canEdit = todo.TodoUserAssignments.Any(tua => tua.UserId == userId && tua.CanEdit);
            if (!isCreator && !canEdit)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this todo.");
            }

            var deletedTodo = new { todo.TaskName, todo.TodoListId };

            _context.Todos.Remove(todo);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Failed to delete todo: {innerException}");
            }

            var logRequest = new LogRequestDto(
                userId,
                "Delete",
                "Todo",
                id,
                new { DeletedTodo = deletedTodo }
            );
            await _logService.LogActionAsync(logRequest);

            var updateMessage = JsonConvert.SerializeObject(new
            {
                action = "todo_deleted",
                data = new { id }
            });
            await _webSocketHandler.SendUpdateAsync(updateMessage);
        }

        private async Task<TodoResponseDto> BuildTodoResponseAsync(Todo todo)
        {
            var status = await _context.TodoStatuses.FindAsync(todo.StatusId);
            var todoList = await _context.TodoLists
                .Include(tl => tl.UserAssignments)
                .FirstOrDefaultAsync(tl => tl.Id == todo.TodoListId);

            return new TodoResponseDto
            {
                Id = todo.Id,
                TaskName = todo.TaskName,
                TaskDescription = todo.TaskDescription,
                StatusId = todo.StatusId,
                StatusName = status?.StatusName ?? "Unknown",
                UserId = todo.UserId,
                TodoListId = todo.TodoListId,
                CreatedAt = todo.CreatedAt,
                TodoListAssignments = todoList?.UserAssignments.Select(ua => new TodoListAssignmentResponseDto
                {
                    UserId = ua.UserId,
                    CanEdit = ua.CanEdit,
                    AssignedAt = ua.AssignedAt
                }).ToList() ?? new List<TodoListAssignmentResponseDto>(),
                TodoAssignments = todo.TodoUserAssignments?.Select(tua => new TodoUserAssignmentResponseDto
                {
                    UserId = tua.UserId,
                    CanEdit = tua.CanEdit,
                    UserName = tua.User?.UserName ?? "Unknown",
                    AssignedAt = tua.AssignedAt
                }).ToList() ?? new List<TodoUserAssignmentResponseDto>()
            };
        }

        private bool UpdateTodoFields(Todo todo, TodoRequestDto todoDto)
        {
            bool hasChanges = todo.TaskName != todoDto.TaskName ||
                              todo.TaskDescription != todoDto.TaskDescription ||
                              todo.StatusId != todoDto.StatusId ||
                              todo.TodoListId != todoDto.TodoListId;

            todo.TaskName = todoDto.TaskName;
            todo.TaskDescription = todoDto.TaskDescription;
            todo.StatusId = todoDto.StatusId;
            if (todo.TodoListId != todoDto.TodoListId)
            {
                todo.TodoListId = todoDto.TodoListId;
            }

            return hasChanges;
        }

        private async Task<List<TodoUserAssignmentResponseDto>> GetTodoListAssignmentsAsync(int todoListId)
        {
            return await _context.TodoListUserAssignments
                .Where(tlua => tlua.TodoListId == todoListId)
                .Select(tlua => new TodoUserAssignmentResponseDto
                {
                    UserId = tlua.UserId,
                    CanEdit = tlua.CanEdit,
                    AssignedAt = tlua.AssignedAt
                })
                .ToListAsync();
        }

        public async Task<List<UserDTO>> GetAssignableUsersForTodoListAsync(int todoListId)
        {
            var userId = GetUserId();

            var todoList = await _context.TodoLists
                .Include(tl => tl.UserAssignments)
                .FirstOrDefaultAsync(tl => tl.Id == todoListId &&
                                          (tl.UserId == userId || tl.UserAssignments.Any(ua => ua.UserId == userId)));

            if (todoList == null)
            {
                throw new UnauthorizedAccessException("Todo list not found or unauthorized");
            }

            var assignedUsers = await _context.Users
                .Where(u => todoList.UserAssignments.Select(ua => ua.UserId).Contains(u.Id))
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    UserName = u.UserName
                })
                .ToListAsync();

            var logRequest = new LogRequestDto(
                userId,
                "Read",
                "TodoListUsers",
                todoListId,
                new { Action = "Retrieved assignable users for TodoList", Count = assignedUsers.Count }
            );
            await _logService.LogActionAsync(logRequest);

            return assignedUsers;
        }
    }
}