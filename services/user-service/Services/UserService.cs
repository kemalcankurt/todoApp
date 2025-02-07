using Microsoft.Extensions.Options;

using AutoMapper;

using user_service.Config;
using user_service.DTOs;
using user_service.Exceptions;
using user_service.Models;
using user_service.Repositories;

namespace user_service.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly IMapper _mapper;
        private readonly JwtOptions _jwtOptions;

        public UserService(IUserRepository userRepository, IMapper mapper, IPasswordService passwordService, IOptions<JwtOptions> jwtOptions)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _mapper = mapper;
            _jwtOptions = jwtOptions.Value;
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

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            var existingUser = await _userRepository.GetByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                throw new DuplicateEmailException("This email is already in use.");
            }

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
            _ = await _userRepository.GetByIdAsync(id)
                       ?? throw new UserNotFoundException();

            await _userRepository.DeleteAsync(id);
        }

        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            var user = await _userRepository.GetUserByRefreshTokenAsync(refreshToken);
            return user;
        }

        public async Task UpdateRefreshTokenAsync(long userId, string refreshToken)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UserNotFoundException();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);

            await _userRepository.UpdateAsync(user);
        }

        public async Task RemoveRefreshToken(long userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null && user.RefreshToken != null && user.RefreshTokenExpiry != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _userRepository.UpdateAsync(user);
            }
        }
    }
}
