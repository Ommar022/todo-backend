using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using eco_m.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TODO.IService;
using TODO.Model;
using TODO.Data;
using TODO.DTO_s.ResponseDTO;

namespace eco_m.Service
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _appDbContext;

        public UserService(
            IMapper mapper,
            IConfiguration configuration,
            UserManager<User> userManager,
            AppDbContext appDbContext)
        {
            _mapper = mapper;
            _configuration = configuration;
            _userManager = userManager;
            _appDbContext = appDbContext;
        }

        public async Task<object> LoginUserAsync(LoginDTO loginDTO)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Email == loginDTO.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDTO.Password))
                return new { Message = "Invalid credentials" };

            if (!user.IsActive)
                return new { Message = "Your account is inactive. Please contact an admin to reactivate your account." };

            var avatar = await _appDbContext.Avatars
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            string token = GenerateJwtToken(user);

            return new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.PhoneNumber,
                    user.Role,
                    user.IsActive,
                    Avatar = avatar != null ? new { Base64Image = avatar.Base64Image } : null
                }
            };
        }

        public async Task<object> SignUpAsync(SignUpDTO signUpDTO)
        {
            var user = _mapper.Map<User>(signUpDTO);
            user.UserName = signUpDTO.UserName;
            user.IsActive = true;

            var result = await _userManager.CreateAsync(user, signUpDTO.Password);
            if (!result.Succeeded)
                return result.Errors.Select(e => e.Description).ToList();

            if (!string.IsNullOrEmpty(signUpDTO.ImageBase64))
            {
                string base64Image = signUpDTO.ImageBase64;
                if (base64Image.Contains(","))
                {
                    base64Image = base64Image.Split(',')[1];
                }

                try
                {
                    Convert.FromBase64String(base64Image);
                }
                catch (FormatException)
                {
                    return new { Message = "Invalid Base64 image data provided" };
                }

                var avatar = new Avatar
                {
                    UserId = user.Id,
                    Base64Image = base64Image 
                };
                _appDbContext.Set<Avatar>().Add(avatar);
                await _appDbContext.SaveChangesAsync();
            }

            return new { Success = true, UserId = user.Id };
        }

        public async Task<List<UserDTO>> GetAllUsersAsync()
        {
            var users = await _userManager.Users
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Role = u.Role,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return users;
        }

        public async Task<Avatar?> GetUserAvatarAsync(int userId)
        {
            return await _appDbContext.Avatars
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task<bool> ToggleUserActiveStatusAsync(int userId, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            if (!isActive && user.Role == "Admin")
            {
                var activeAdmins = await _userManager.Users
                    .Where(u => u.Role == "Admin" && u.IsActive && u.Id != userId)
                    .CountAsync();
                if (activeAdmins == 0)
                    throw new InvalidOperationException("Cannot deactivate the last active admin.");
            }

            user.IsActive = isActive;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var avatar = await _appDbContext.Avatars.FirstOrDefaultAsync(a => a.UserId == userId);
            if (avatar != null)
                _appDbContext.Avatars.Remove(avatar);

            var userComments = await _appDbContext.Comments
                .Where(c => c.UserId == userId)
                .ToListAsync();
            if (userComments.Any())
                _appDbContext.Comments.RemoveRange(userComments);

            var createdTodos = await _appDbContext.Todos
                .Where(t => t.UserId == userId)
                .ToListAsync();
            if (createdTodos.Any())
            {
                var todoIds = createdTodos.Select(t => t.Id).ToList();
                var todoAssignments = await _appDbContext.TodoUserAssignments
                    .Where(tua => todoIds.Contains(tua.TodoId))
                    .ToListAsync();
                if (todoAssignments.Any())
                    _appDbContext.TodoUserAssignments.RemoveRange(todoAssignments);

                var todoComments = await _appDbContext.Comments
                    .Where(c => todoIds.Contains(c.TodoId))
                    .ToListAsync();
                if (todoComments.Any())
                    _appDbContext.Comments.RemoveRange(todoComments);

                _appDbContext.Todos.RemoveRange(createdTodos);
            }

            var assignments = await _appDbContext.TodoUserAssignments
                .Where(tua => tua.UserId == userId)
                .ToListAsync();
            if (assignments.Any())
                _appDbContext.TodoUserAssignments.RemoveRange(assignments);

            var todoLists = await _appDbContext.TodoLists
                .Where(tl => tl.UserId == userId)
                .ToListAsync();
            if (todoLists.Any())
                _appDbContext.TodoLists.RemoveRange(todoLists);

            try
            {
                await _appDbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error saving changes: {ex.InnerException?.Message}");
                throw;
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}