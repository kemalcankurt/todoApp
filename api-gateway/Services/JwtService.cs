using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

using api_gateway.services;
using api_gateway.Config;

public class JwtService : IJwtService
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtOptions> jwtOptions, ILogger<JwtService> logger)
    {
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public string GenerateJwtToken(long userId, string email, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return GenerateToken(claims, _jwtOptions.ExpiryMinutes);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public ClaimsPrincipal? DecodeJwtToken(string token)
    {
        Console.WriteLine($"Decoding Token: {token}");

        try
        {
            var principal = ValidateToken(token);
            if (principal == null)
            {
                Console.WriteLine("Token validation failed.");
                return null;
            }

            return principal;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token Decode Error: {ex.Message}");
            return null;
        }
    }

    private string GenerateToken(IEnumerable<Claim> claims, int expiryMinutes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(token))
            {
                return null;
            }

            _logger.LogCritical($"_jwtOptions.Secret", _jwtOptions.Secret);


            var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidAudience = _jwtOptions.Audience
            };

            return tokenHandler.ValidateToken(token, parameters, out _);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ValidateToken threw an error: {ex}");

            return null;
        }
    }
}