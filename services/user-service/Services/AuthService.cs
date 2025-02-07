using user_service.DTOs;
using user_service.Config;

using Microsoft.Extensions.Options;

namespace user_service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;
        private readonly JwtOptions _jwtOptions;

        public AuthService(IUserService userService, IJwtService jwtService, IPasswordService passwordService, IOptions<JwtOptions> jwtOptions)
        {
            _userService = userService;
            _jwtService = jwtService;
            _passwordService = passwordService;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<AuthResponseDto?> AuthenticateUserAsync(LoginDto loginDto)
        {
            var user = await _userService.GetUserByEmailAsync(loginDto.Email);
            if (user == null || !_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt))
                return null;

            var accessToken = _jwtService.GenerateJwtToken(user.Id, user.Email, user.Role);
            var refreshToken = _jwtService.GenerateRefreshToken();

            await _userService.UpdateRefreshTokenAsync(user.Id, refreshToken);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<RefreshTokenDto> RefreshTokenAsync(string refreshToken)
        {
            var user = await _userService.GetUserByRefreshTokenAsync(refreshToken);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token");

            var newAccessToken = _jwtService.GenerateJwtToken(user.Id, user.Email, user.Role);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            await _userService.UpdateRefreshTokenAsync(user.Id, newRefreshToken);

            return new RefreshTokenDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
        }

        public async Task<bool> LogoutAsync(HttpContext context)
        {
            string? token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrEmpty(token))
                return false;

            long? userId = _jwtService.DecodeJwtToken(token);
            if (userId == null) return false;

            await _userService.RemoveRefreshToken(userId.Value);
            return true;
        }
    }
}
