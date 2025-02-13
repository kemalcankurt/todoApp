using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

using api_gateway.services;

namespace api_gateway.Middleware
{
    public class RoleToClientIdMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<RoleToClientIdMiddleware> _logger;

        private readonly IJwtService _jwtService;

        public RoleToClientIdMiddleware(RequestDelegate next, ILogger<RoleToClientIdMiddleware> logger, IJwtService jwtService)
        {
            _next = next;
            _logger = logger;
            _jwtService = jwtService;
        }

        public async Task Invoke(HttpContext context)
        {
            string? token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

            _logger.LogInformation($"token:  {token}");

            if (token != null)
            {
                ClaimsPrincipal? jwt = _jwtService.DecodeJwtToken(token);

                if (jwt != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.Preserve,
                        WriteIndented = true
                    };

                    _logger.LogCritical("JWT Claims: {Claims}", JsonSerializer.Serialize(jwt.Claims, options));

                    var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

                    if (roleClaim != null)
                    {
                        context.Request.Headers["Client-Id"] = roleClaim.Value?.ToLower(); // Inject role as Client-Id header
                        _logger.LogCritical("JWT Service Request Headers: {Headers}", JsonSerializer.Serialize(context.Request.Headers, options));
                    }
                }
            }

            await _next(context);
        }
    }
}