using System.Text;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using user_service.Config;
using user_service.DTOs;
using user_service.Mappings;
using user_service.Models;
using user_service.Repositories;
using user_service.Services;

namespace user_service.Tests.Services
{
    public class UserServiceTests
    {
        private readonly UserService _userService;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IOptions<JwtOptions>> _jwtOptionsMock;
        private readonly IMapper _mapper;
        private readonly Mock<PasswordService> _passwordServiceMock;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _jwtOptionsMock = new Mock<IOptions<JwtOptions>>();
            _passwordServiceMock = new Mock<PasswordService>();

            var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<MappingProfile>();
                });
            _mapper = config.CreateMapper();


            _userService = new UserService(_userRepositoryMock.Object, _mapper, _passwordServiceMock.Object, _jwtOptionsMock.Object);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Email = "test@example.com" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(userId);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = 1;
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(null as User);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }
        [Fact]
        public async Task GetUserByIdAsync_InvalidId_ShouldReturnNull()
        {
            // Arrange
            long invalidUserId = 999;

            _userRepositoryMock.Setup(x => x.GetByIdAsync(invalidUserId))
                .ReturnsAsync(null as User);

            // Act
            var result = await _userService.GetUserByIdAsync(invalidUserId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUserAsync_InvalidId_ShouldThrowException()
        {
            // Arrange
            long invalidUserId = 999;
            var updateUserDto = new UpdateUserDto { Email = "updated@todoapp.com", Password = "NewPass123" };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(invalidUserId))
                .ReturnsAsync(null as User);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _userService.UpdateUserAsync(invalidUserId, updateUserDto));
        }

        [Fact]
        public async Task CreateUserAsync_DuplicateEmail_ShouldThrowException()
        {
            // Arrange
            var createUserDto = new CreateUserDto { Email = "test@todoapp.com", Password = "Password123" };
            var existingUser = new User { Id = 1, Email = createUserDto.Email, PasswordHash = Encoding.UTF8.GetBytes("hashed-pass"), PasswordSalt = Encoding.UTF8.GetBytes("salt") };

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(createUserDto.Email))
                .ReturnsAsync(existingUser);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _userService.CreateUserAsync(createUserDto));
        }


    }
}