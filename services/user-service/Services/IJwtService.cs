namespace user_service.Services
{
    public interface IJwtService
    {
        string GenerateJwtToken(long userId, string email, string role);
        string GenerateRefreshToken();
        public long? DecodeJwtToken(string token);
    }
}
