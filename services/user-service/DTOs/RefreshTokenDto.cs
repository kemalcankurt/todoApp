
namespace user_service.DTOs
{

    public class RefreshTokenDto
    {
        public required string RefreshToken { get; set; }

        public string? AccessToken { get; set; }
    }
}