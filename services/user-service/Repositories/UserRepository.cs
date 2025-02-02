using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using user_service.Config;
using user_service.Data;
using user_service.Models;

namespace user_service.Repositories
{
    public class UserRepository : IUserRepository

    {
        private readonly UserDbContext _dbContext;
        private readonly JwtOptions _jwtOptions;

        public UserRepository(UserDbContext dbContext, IOptions<JwtOptions> jwtOptions)
        {
            _dbContext = dbContext;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbContext.Users.Where(u => !u.IsDeleted).ToListAsync();
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task AddAsync(User user)
        {
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                user.IsDeleted = true;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        }

        public async Task UpdateRefreshTokenAsync(long userId, string refreshToken)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}