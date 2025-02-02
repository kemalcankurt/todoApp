using System;
using Moq;
using Microsoft.Extensions.Options;
using user_service.Config;
using user_service.Services;
using Xunit;
using user_service.DTOs;
using user_service.Models;
using System.Text;

namespace user_service.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly AuthService _authService;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IOptions<JwtOptions>> _jwtOptionsMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly JwtService _jwtService;

        public AuthServiceTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _jwtOptionsMock = new Mock<IOptions<JwtOptions>>();
            _passwordServiceMock = new Mock<IPasswordService>();

            var jwtOptions = new JwtOptions
            {
                Secret = "your-test-secret-key-your-test-secret-key",
                ExpiryMinutes = 15,
                Issuer = "http://localhost:8000",
                Audience = "http://localhost:8000",
                RefreshTokenExpiryDays = 7
            };

            _jwtOptionsMock.Setup(o => o.Value).Returns(jwtOptions);

            _jwtService = new JwtService(_jwtOptionsMock.Object);

            _authService = new AuthService(
                _userServiceMock.Object,
                _jwtService,
                _passwordServiceMock.Object,
                _jwtOptionsMock.Object
            );
        }

        [Fact]
        public void AuthService_ShouldInitializeCorrectly()
        {
            Assert.NotNull(_authService);
        }

        [Fact]
        public async Task AuthenticateUserAsync_ValidCredentials_ShouldReturnTokens()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };

            var user = new User
            {
                Id = 1,
                Email = loginDto.Email,
                Role = "User",
                PasswordHash = Convert.FromBase64String("cGFzc3dvcmQ="), // Base64 encoded "password"
                PasswordSalt = Convert.FromBase64String("c2FsdA==") // Base64 encoded "salt"
            };

            _userServiceMock.Setup(x => x.GetUserByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _passwordServiceMock.Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt))
                .Returns(true);

            // Act
            var result = await _authService.AuthenticateUserAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        }
        [Fact]
        public async Task RefreshTokenAsync_ValidRefreshToken_ShouldReturnNewTokens()
        {
            // Arrange
            var refreshToken = "valid-refresh-token";
            var userDto = new User { Id = 1, Email = "test@todoapp.com", Role = "User", RefreshToken = refreshToken, RefreshTokenExpiry = DateTime.UtcNow.AddDays(1) };

            _userServiceMock.Setup(x => x.GetUserByRefreshTokenAsync(refreshToken))
                .ReturnsAsync(userDto);

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        }

        [Fact]
        public async Task AuthenticateUserAsync_InvalidPassword_ShouldReturnNull()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "WrongPassword" };
            var user = new User { Id = 1, Email = loginDto.Email, Role = "User", PasswordHash = Encoding.UTF8.GetBytes("hashed-pass"), PasswordSalt = Encoding.UTF8.GetBytes("salt") };

            _userServiceMock.Setup(x => x.GetUserByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _passwordServiceMock.Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt))
                .Returns(false);

            // Act
            var result = await _authService.AuthenticateUserAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateUserAsync_InvalidEmail_ShouldReturnNull()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "notfound@todoapp.com", Password = "Password123" };

            _userServiceMock.Setup(x => x.GetUserByEmailAsync(loginDto.Email))
                .ReturnsAsync(null as User);

            // Act
            var result = await _authService.AuthenticateUserAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_ShouldThrowUnauthorizedException()
        {
            // Arrange
            string expiredRefreshToken = "expired-token";
            var user = new User { Id = 1, Email = "test@todoapp.com", Role = "User", RefreshToken = expiredRefreshToken, RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1) }; // Expired

            _userServiceMock.Setup(x => x.GetUserByRefreshTokenAsync(expiredRefreshToken))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.RefreshTokenAsync(expiredRefreshToken));
        }

        [Fact]
        public async Task RefreshTokenAsync_InvalidToken_ShouldThrowUnauthorizedException()
        {
            // Arrange
            string invalidToken = "invalid-token";

            _userServiceMock.Setup(x => x.GetUserByRefreshTokenAsync(invalidToken))
                .ReturnsAsync(null as User);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.RefreshTokenAsync(invalidToken));
        }


    }
}
