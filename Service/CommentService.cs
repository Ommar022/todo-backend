using Microsoft.EntityFrameworkCore;
using TODO.Model;
using TODO.Data;
using TODO.IService;
using TODO.DTO_s.ResponseDTO;
using TODO.Sockets;
using TODO.DTO_s.RequestDTO;

namespace TODO.Services
{
    public class CommentService : ICommentService
    {
        private readonly AppDbContext _context;
        private readonly WebSocketHandler _webSocketHandler;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogService _logService;

        public CommentService(AppDbContext context, WebSocketHandler webSocketHandler, ILogService logService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _webSocketHandler = webSocketHandler;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CommentResponseDto> CreateCommentAsync(int todoId, int userId, string text)
        {
            var todo = await _context.Todos.FindAsync(todoId);
            if (todo == null)
            {
                throw new KeyNotFoundException($"Todo with ID {todoId} not found.");
            }

            var comment = new Comment
            {
                Text = text,
                TodoId = todoId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var user = await _context.Users
                .Include(u => u.Avatar)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var responseDto = new CommentResponseDto
            {
                Id = comment.Id,
                Text = comment.Text,
                TodoId = comment.TodoId,
                UserId = comment.UserId,
                UserName = user?.UserName ?? "Unknown",
                //AvatarUrl = user?.Avatar?.Base64Image,
                CreatedAt = comment.CreatedAt
            };

            var logRequest = new LogRequestDto(
                userId,
                "Create",
                "Comment",
                comment.Id,
                new { TodoId = todoId, Text = text }
            );
            await _logService.LogActionAsync(logRequest);

            string message = $"{{ \"action\": \"create\", \"comment\": {System.Text.Json.JsonSerializer.Serialize(responseDto)} }}";
            await _webSocketHandler.SendUpdateAsync(message);

            return responseDto;
        }

        public async Task<List<CommentResponseDto>> GetCommentsByTodoAsync(int todoId)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .ThenInclude(u => u.Avatar)
                .Where(c => c.TodoId == todoId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var response = comments.Select(comment => new CommentResponseDto
            {
                Id = comment.Id,
                Text = comment.Text,
                TodoId = comment.TodoId,
                UserId = comment.UserId,
                UserName = comment.User?.UserName ?? "Unknown",
                //AvatarUrl = comment.User?.Avatar?.Base64Image,
                CreatedAt = comment.CreatedAt
            }).ToList();

            var logRequest = new LogRequestDto(
             GetUserId(), 
             "Read",
             "Comment",
             todoId,
             new { Action = "Retrieved comments for todo", TodoId = todoId, Count = response.Count }
         );
            await _logService.LogActionAsync(logRequest);

            return response;
        }

        public async Task<CommentResponseDto> UpdateCommentAsync(int commentId, int userId, string text)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);
            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment with ID {commentId} not found or you are not authorized to update it.");
            }

            var oldText = comment.Text; 
            comment.Text = text;
            _context.Entry(comment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var user = await _context.Users
                .Include(u => u.Avatar)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var responseDto = new CommentResponseDto
            {
                Id = comment.Id,
                Text = comment.Text,
                TodoId = comment.TodoId,
                UserId = comment.UserId,
                UserName = user?.UserName ?? "Unknown",
                //AvatarUrl = user?.Avatar?.Base64Image,
                CreatedAt = comment.CreatedAt
            };

            var logRequest = new LogRequestDto(
                userId,
                "Update",
                "Comment",
                commentId,
                new { TodoId = comment.TodoId, OldText = oldText, NewText = text }
            );
            await _logService.LogActionAsync(logRequest);

            string message = $"{{ \"action\": \"update\", \"comment\": {System.Text.Json.JsonSerializer.Serialize(responseDto)} }}";
            await _webSocketHandler.SendUpdateAsync(message);

            return responseDto;
        }

        public async Task DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);
            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment with ID {commentId} not found or you are not authorized to delete it.");
            }

            var deletedComment = new { comment.TodoId, comment.Text }; 
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            var logRequest = new LogRequestDto(
                userId,
                "Delete",
                "Comment",
                commentId,
                new { DeletedComment = deletedComment }
            );
            await _logService.LogActionAsync(logRequest);

            string message = $"{{ \"action\": \"delete\", \"commentId\": {commentId} }}";
            await _webSocketHandler.SendUpdateAsync(message);
        }

        private int GetUserId()
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return 0; 
            }
            return userId;
        }
    }
}