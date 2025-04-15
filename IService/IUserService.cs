using eco_m.DTO;
using TODO.DTO_s.ResponseDTO;
using TODO.Model;

namespace TODO.IService
{
    public interface IUserService
    {
        Task<object> LoginUserAsync(LoginDTO loginDTO);
        Task<object> SignUpAsync(SignUpDTO signUpDTO);
        Task<List<UserDTO>> GetAllUsersAsync();
        Task<Avatar?> GetUserAvatarAsync(int userId);
        Task<bool> ToggleUserActiveStatusAsync(int userId, bool isActive); 
        Task<bool> DeleteUserAsync(int userId);
    }
}
