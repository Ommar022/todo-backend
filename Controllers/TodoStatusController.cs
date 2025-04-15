using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;
using TODO.IService;

namespace eco_m.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TodoStatusController : ControllerBase
    {
        private readonly ITodoStatusService _todoStatusService;

        public TodoStatusController(ITodoStatusService todoStatusService)
        {
            _todoStatusService = todoStatusService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTodoStatuses()
        {
            try
            {
                var statuses = await _todoStatusService.GetAllTodoStatusesAsync();
                return Ok(statuses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving todo statuses", Error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<TodoResponseDto>> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto statusDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedTodo = await _todoStatusService.UpdateStatusAsync(id, statusDto);
                return Ok(updatedTodo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}