using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;
using TODO.IService;
using TODO.Service;

namespace TODO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class TodoListController : ControllerBase
    {
        private readonly ITodoListService _todoListService;

        public TodoListController(ITodoListService todoListService)
        {
            _todoListService = todoListService;
        }

        [HttpGet]
        public async Task<ActionResult<List<TodoListResponseDto>>> GetAllTodoLists()
        {
            try
            {
                var todoLists = await _todoListService.GetAllTodoListsAsync();
                return Ok(todoLists);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TodoListResponseDto>> GetTodoList(int id)
        {
            try
            {
                var todoList = await _todoListService.GetTodoListByIdAsync(id);
                return Ok(todoList);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [HttpPost]
        public async Task<ActionResult<TodoListResponseDto>> CreateTodoList([FromBody] TodoListRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdTodoList = await _todoListService.CreateTodoListAsync(request);
                return CreatedAtAction(nameof(GetTodoList), new { id = createdTodoList.Id }, createdTodoList);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodoList(int id, [FromBody] TodoListRequestDto request)
        {
            try
            {
                var updatedTodoList = await _todoListService.UpdateTodoListAsync(id, request);
                return Ok(updatedTodoList); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); 
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoList(int id)
        {
            try
            {
                await _todoListService.DeleteTodoListAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}