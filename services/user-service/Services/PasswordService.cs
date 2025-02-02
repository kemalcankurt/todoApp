using System.Security.Cryptography;
using System.Text;

namespace user_service.Services
{
    public class PasswordService
    {
        public HashedPassword HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            return new HashedPassword
            {
                Hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password)),
                Salt = hmac.Key
            };
        }

        public bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(storedHash);
        }
    }

    public class HashedPassword
    {
        public byte[] Hash { get; set; } = Array.Empty<byte>();
        public byte[] Salt { get; set; } = Array.Empty<byte>();
    }
}
