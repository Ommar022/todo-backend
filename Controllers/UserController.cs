using eco_m.DTO;
using eco_m.Service;
using Microsoft.AspNetCore.Mvc;
using TODO.IService;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eco_m.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpDTO signUpDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _userService.SignUpAsync(signUpDTO);

                if (result is List<string> errors)
                    return BadRequest(new { Errors = errors });

                if (result is object error && error.GetType().GetProperty("Message")?.GetValue(error) as string == "Invalid Base64 image data provided")
                    return BadRequest(error);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred during sign up", Error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _userService.LoginUserAsync(loginDTO);

                if (result is object error && error.GetType().GetProperty("Message")?.GetValue(error) is string message)
                {
                    if (message == "Invalid credentials" || message == "Your account is inactive. Please contact an admin to reactivate your account.")
                        return Unauthorized(error);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred during login", Error = ex.Message });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving users", Error = ex.Message });
            }
        }

        [HttpGet("avatar/{userId}")]
        public async Task<IActionResult> GetUserAvatar(int userId)
        {
            try
            {
                var avatar = await _userService.GetUserAvatarAsync(userId);

                if (avatar == null)
                    return NotFound(new { Message = $"No avatar found for user with ID {userId}" });

                string base64String = avatar.Base64Image;
                if (base64String.Contains(","))
                    base64String = base64String.Split(',')[1];

                byte[] imageBytes;
                try
                {
                    imageBytes = Convert.FromBase64String(base64String);
                }
                catch (FormatException)
                {
                    return BadRequest(new { Message = "Invalid Base64 image data" });
                }

                string contentType = DetectContentType(imageBytes) ?? "image/jpeg";

                return File(imageBytes, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving the avatar",
                    Error = ex.Message
                });
            }
        }

        [HttpPut("toggle-active/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleUserActiveStatus(int userId, [FromBody] bool isActive)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                    return Unauthorized(new { Message = "Invalid user ID in token." });

                if (currentUserId == userId)
                    return BadRequest(new { Message = "You cannot deactivate your own account." });

                var success = await _userService.ToggleUserActiveStatusAsync(userId, isActive);
                if (!success)
                    return NotFound(new { Message = $"User with ID {userId} not found." });

                return Ok(new { Message = $"User {(isActive ? "activated" : "deactivated")} successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while toggling user status", Error = ex.Message });
            }
        }

        [HttpDelete("delete/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                    return Unauthorized(new { Message = "Invalid user ID in token." });

                if (currentUserId == userId)
                    return BadRequest(new { Message = "You cannot delete your own account." });

                var success = await _userService.DeleteUserAsync(userId);
                if (!success)
                    return NotFound(new { Message = $"User with ID {userId} not found." });

                return Ok(new { Message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the user", Error = ex.Message });
            }
        }

        private string? DetectContentType(byte[] imageBytes)
        {
            if (imageBytes.Length < 4) return null;

            if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                return "image/jpeg";
            if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                return "image/png";
            if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
                return "image/gif";

            return null;
        }
    }
}