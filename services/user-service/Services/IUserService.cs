using user_service.DTOs;

namespace user_service.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(long id);
        Task<UserDto?> GetUserByUsernameAsync(string username);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task UpdateUserAsync(long id, UpdateUserDto updateUserDto);
        Task DeleteUserAsync(long id);
        Task<bool> VerifyUserCredentialsAsync(string email, string password);
    }
}
