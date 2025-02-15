using System.Security.Claims;

namespace api_gateway.Services
{
    public interface IJwtService
    {
        string GenerateJwtToken(long userId, string email, string role);
        string GenerateRefreshToken();
        public ClaimsPrincipal? DecodeJwtToken(string token);
    }
}