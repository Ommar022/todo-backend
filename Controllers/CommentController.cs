using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TODO.Model;
using TODO.Services;
using TODO.IService;
using System.Security.Claims;
using TODO.DTO_s.RequestDTO;

namespace TODO.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService; 

        public CommentController(ICommentService commentService) 
        {
            _commentService = commentService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null ? int.Parse(userIdClaim) : 0;
        }

        [HttpPost]
        public async Task<ActionResult<Comment>> CreateComment([FromBody] CommentRequestDto commentDto)
        {
            try
            {
                var userId = GetUserId();
                var comment = await _commentService.CreateCommentAsync(commentDto.TodoId, userId, commentDto.Text);
                return CreatedAtAction(nameof(GetCommentsByTodo), new { todoId = comment.TodoId }, comment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("todo/{todoId}")]
        public async Task<ActionResult<List<Comment>>> GetCommentsByTodo(int todoId)
        {
            try
            {
                var comments = await _commentService.GetCommentsByTodoAsync(todoId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Comment>> UpdateComment(int id, [FromBody] CommentRequestDto commentDto)
        {
            try
            {
                var userId = GetUserId();
                var updatedComment = await _commentService.UpdateCommentAsync(id, userId, commentDto.Text);
                return Ok(updatedComment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var userId = GetUserId();
                await _commentService.DeleteCommentAsync(id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}