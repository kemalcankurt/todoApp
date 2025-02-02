namespace user_service.Models
{
    public class User
    {
        public long Id { get; set; }

        // User info
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;  // Email (unique)

        // Password Hash
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        // Auth & State
        public string Role { get; set; } = "User";  // Default role
        public bool IsActive { get; set; } = true;  // Hesap aktif mi?
        public bool IsDeleted { get; set; } = false;  // Soft delete

        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Refresh Token
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
    }
}
