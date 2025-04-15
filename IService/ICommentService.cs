using TODO.DTO_s.ResponseDTO;
using TODO.Model;

namespace TODO.IService
{
    public interface ICommentService
    {
        Task<CommentResponseDto> CreateCommentAsync(int todoId, int userId, string text);
        Task<List<CommentResponseDto>> GetCommentsByTodoAsync(int todoId);
        Task<CommentResponseDto> UpdateCommentAsync(int commentId, int userId, string text);
        Task DeleteCommentAsync(int commentId, int userId);
    }
}
