namespace user_service.Services
{
    public interface IPasswordService
    {
        HashedPassword HashPassword(string password);
        bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt);
    }
}