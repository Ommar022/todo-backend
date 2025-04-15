using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;
using TODO.IService;

namespace TODO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;

        public TodoController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpGet("todos/by-todolist/{todoListId}")]
        public async Task<IActionResult> GetTodosByTodoListId(int todoListId)
        {
            try
            {
                var todos = await _todoService.GetTodosByTodoListIdAsync(todoListId);
                return Ok(todos);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TodoResponseDto>> GetTodo(int id)
        {
            try
            {
                var todo = await _todoService.GetTodoByIdAsync(id);
                return Ok(todo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TodoResponseDto>> CreateTodo([FromBody] TodoRequestDto todoDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdTodo = await _todoService.CreateTodoAsync(todoDto);
                return CreatedAtAction(nameof(GetTodo), new { id = createdTodo.Id }, createdTodo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodo(int id, [FromBody] TodoRequestDto todoDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedTodo = await _todoService.UpdateTodoAsync(id, todoDto);
                return Ok(updatedTodo); 
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            try
            {
                await _todoService.DeleteTodoAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{todoListId}/assignable-users")]
        public async Task<ActionResult<List<UserDTO>>> GetAssignableUsersForTodoList(int todoListId)
        {
            var users = await _todoService.GetAssignableUsersForTodoListAsync(todoListId);
            return Ok(users);
        }
    }
}