using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using user_service.Authorization;
using user_service.DTOs;
using user_service.Services;

namespace user_service.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UserController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        [Authorize(Policy = Policies.AdminOnly)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [Authorize(Policy = Policies.CanViewUser)]
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUserById(long id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            return Ok(user);
        }

        [Authorize(Policy = Policies.AdminOnly)]
        [HttpGet("username/{username}")]
        public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null) return NotFound();

            return Ok(user);
        }

        [Authorize(Policy = Policies.AdminOnly)]
        [HttpGet("email/{email}")]
        public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null) return NotFound();

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
        {
            var user = await _userService.CreateUserAsync(createUserDto);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        [Authorize(Policy = Policies.CanViewUser)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(long id, UpdateUserDto updateUserDto)
        {
            await _userService.UpdateUserAsync(id, updateUserDto);
            return NoContent();
        }

        [Authorize(Policy = Policies.AdminOnly)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            await _userService.DeleteUserAsync(id);
            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var authResult = await _authService.AuthenticateUserAsync(loginDto);
            if (authResult == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(authResult);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var tokens = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);
                return Ok(tokens);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            bool isLoggedOut = await _authService.LogoutAsync(HttpContext);

            if (!isLoggedOut)
                return Unauthorized();

            return Ok(new { message = "Logged out successfully" });
        }
    }
}
