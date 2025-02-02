using System.IdentityModel.Tokens.Jwt;

using Microsoft.Extensions.Options;

using todo_service.Config;

namespace todo_service.Services
{
    public class JwtService
    {
        private readonly JwtOptions _jwtOptions;

        public JwtService(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        public long? DecodeJwtToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

            return userIdClaim != null ? long.Parse(userIdClaim.Value) : null;
        }
    }
}