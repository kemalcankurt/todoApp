using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

using user_service.Authorization.Requirements;

namespace user_service.Authorization
{
    public class UserAuthorizationHandler : AuthorizationHandler<UserAuthorizationRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserAuthorizationHandler> _logger;

        public UserAuthorizationHandler(IHttpContextAccessor httpContextAccessor, ILogger<UserAuthorizationHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserAuthorizationRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                LogError("HttpContext is not accessible.");
                return Task.CompletedTask;
            }

            var userIdClaim = GetUserIdClaim(httpContext);
            var userRole = GetUserRole(httpContext);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                LogWarning("User ID claim is missing.");
                return Task.CompletedTask;
            }

            LogInfo($"Policy Check | UserID: {userIdClaim}, Role: {userRole}");

            if (IsAdmin(userRole))
            {
                LogInfo("Admin access granted.");
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (IsUserRequestingOwnData(httpContext, userIdClaim))
            {
                LogInfo($"User authorized for their own data. UserID: {userIdClaim}");
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            LogWarning("Authorization failed.");
            return Task.CompletedTask;
        }

        private string? GetUserIdClaim(HttpContext httpContext)
            => httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private string? GetUserRole(HttpContext httpContext)
            => httpContext.User.FindFirst(ClaimTypes.Role)?.Value;

        private bool IsAdmin(string? role)
            => role == "Admin";

        private bool IsUserRequestingOwnData(HttpContext httpContext, string userIdClaim)
        {
            var routeUserId = httpContext.Request.RouteValues["id"]?.ToString();
            return routeUserId != null && routeUserId == userIdClaim;
        }

        private void LogError(string message)
            => _logger.LogError(message);

        private void LogWarning(string message)
            => _logger.LogWarning(message);

        private void LogInfo(string message)
            => _logger.LogInformation(message);

    }
}
