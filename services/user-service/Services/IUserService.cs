using user_service.DTOs;
using user_service.Models;

namespace user_service.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(long id);
        Task<UserDto?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task UpdateUserAsync(long id, UpdateUserDto updateUserDto);
        Task DeleteUserAsync(long id);
        Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
        Task UpdateRefreshTokenAsync(long userId, string refreshToken);
        Task RemoveRefreshToken(long userId);
    }
}
