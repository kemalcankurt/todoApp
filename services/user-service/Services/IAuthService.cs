using user_service.DTOs;

namespace user_service.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> AuthenticateUserAsync(LoginDto loginDto);
        Task<RefreshTokenDto> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(HttpContext context);
    }
}
