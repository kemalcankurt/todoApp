using AutoMapper;

using user_service.DTOs;
using user_service.Models;
using user_service.Repositories;

namespace user_service.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly PasswordService _passwordService;
        private readonly JwtService _jwtService;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper, PasswordService passwordService, JwtService jwtService)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto?> GetUserByIdAsync(long id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            var user = _mapper.Map<User>(createUserDto);

            var hashedPassword = _passwordService.HashPassword(createUserDto.Password);
            user.PasswordHash = hashedPassword.Hash;
            user.PasswordSalt = hashedPassword.Salt;

            await _userRepository.AddAsync(user);
            return _mapper.Map<UserDto>(user);
        }

        public async Task UpdateUserAsync(long id, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new Exception("User not found");

            if (!string.IsNullOrWhiteSpace(updateUserDto.Password))
            {
                var hashedPassword = _passwordService.HashPassword(updateUserDto.Password);
                user.PasswordHash = hashedPassword.Hash;
                user.PasswordSalt = hashedPassword.Salt;
            }

            _mapper.Map(updateUserDto, user);
            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(long id)
        {
            await _userRepository.DeleteAsync(id);
        }

        public async Task<string?> VerifyUserCredentialsAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || !_passwordService.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                return null;

            return _jwtService.GenerateJwtToken(user.Id, user.Email, user.Role);
        }
    }
}
