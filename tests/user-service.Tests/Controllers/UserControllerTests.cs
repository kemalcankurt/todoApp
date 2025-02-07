using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using user_service.DTOs;

namespace user_service.Tests.Controllers
{
    public class UserControllerTests : IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public UserControllerTests()
        {
            _factory = new CustomWebApplicationFactory<Program>();
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _factory.Dispose();
        }

        [Fact]
        public async Task Login_ValidCredentials_ShouldReturnTokens()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/user/login", loginDto);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(content);
            Assert.False(string.IsNullOrWhiteSpace(content.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(content.RefreshToken));
        }

        [Fact]
        public async Task Register_ValidUser_ShouldReturn201Created()
        {
            // Arrange
            var createUserDto = new CreateUserDto { Email = "newuser@todoapp.com", Password = "SecurePassword123" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/user/register", createUserDto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ShouldReturn400BadRequest()
        {
            // Arrange
            var createUserDto = new CreateUserDto { Email = "test@todoapp.com", Password = "AnotherPass123" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/user/register", createUserDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);


            var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            Assert.NotNull(content);
            Assert.True(content.ContainsKey("message"));
            Assert.Equal("This email is already in use.", content["message"]);
        }

        [Fact]
        public async Task Logout_ValidToken_ShouldReturn200OK()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.PostAsync("/api/user/logout", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Logout_InvalidToken_ShouldReturn401Unauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.PostAsync("/api/user/logout", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUser_ValidToken_ShouldReturnUser()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            Assert.NotNull(user);
            Assert.Equal("test@todoapp.com", user.Email);
        }
        [Fact]
        public async Task GetUser_OtherUser_ShouldReturn403Forbidden()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user/99");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUser_NoAuth_ShouldReturn401Unauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/user/1");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnList_WhenAuthorizedAsAdmin()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "testAdmin@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user");

            // Assert
            response.EnsureSuccessStatusCode();
            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
            Assert.NotNull(users);
            Assert.NotEmpty(users);
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnForbidden_WhenNotAuthorizedAsAdmin()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUserByUsername_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "testAdmin@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user/username/testUser");

            // Assert
            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            Assert.NotNull(user);
            Assert.Equal("test@todoapp.com", user.Email);
        }

        [Fact]
        public async Task GetUserByUsername_ShouldReturnForbidden_WhenRegularUserIsLoggedIn()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user/username/testUser");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUserByUsername_ShouldReturn404_WhenNotExists()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "testAdmin@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user/username/nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetUserByEmail_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "testAdmin@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user/email/test@todoapp.com");

            // Assert
            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            Assert.NotNull(user);
            Assert.Equal("test@todoapp.com", user.Email);
        }

        [Fact]
        public async Task GetUserByEmail_ShouldReturnForbidden_WhenLoggedInUserIsNotAdmin()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user/email/test@todoapp.com");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUserByEmail_ShouldReturn404_WhenNotExists()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "testAdmin@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.GetAsync("/api/user/email/nonexistent@todoapp.com");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUser_ShouldReturnNoContent_WhenUpdatedSuccessfully()
        {
            // Arrange
            var updateDto = new UpdateUserDto { Username = "updatedUser" };

            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.PutAsJsonAsync("/api/user/1", updateDto);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUser_ShouldReturnBadRequest_WhenUpdateUserDoesNotExist()
        {
            // Arrange
            var updateDto = new UpdateUserDto { Username = "updatedUser" };

            var loginDto = new LoginDto { Email = "testAdmin@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var invalidUserId = "5";
            var response = await _client.PutAsJsonAsync($"/api/user/{invalidUserId}", updateDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnNoContent_WhenDeletedSuccessfully()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "testAdmin@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.DeleteAsync("/api/user/1");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnBadRequest_WhenUserDoesNotExist()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "testAdmin@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            // Act
            var response = await _client.DeleteAsync("/api/user/11");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnNewTokens_WhenValid()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@todoapp.com", Password = "Password123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/user/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

            var refreshTokenDto = new RefreshTokenDto { RefreshToken = authResponse.RefreshToken };

            // Act
            var response = await _client.PostAsJsonAsync("/api/user/refresh-token", refreshTokenDto);

            // Assert
            response.EnsureSuccessStatusCode();
            var newTokens = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(newTokens);
            Assert.False(string.IsNullOrWhiteSpace(newTokens.AccessToken));
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnUnauthorized_WhenInvalid()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto { RefreshToken = "invalid-token" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/user/refresh-token", refreshTokenDto);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }



    }
}
