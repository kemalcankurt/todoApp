using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

using user_service.Authorization.Requirements;

namespace user_service.Authorization.Handlers
{
    public class AdminAuthorizationHandler : AuthorizationHandler<AdminAuthorizationRequirement>
    {
        private readonly ILogger<AdminAuthorizationHandler> _logger;

        public AdminAuthorizationHandler(ILogger<AdminAuthorizationHandler> logger) => _logger = logger;

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminAuthorizationRequirement requirement)
        {
            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation($"Checking user role | UserRole: {userRole}");

            if (userRole == "Admin")
            {
                _logger.LogInformation("Admin access granted.");
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Only Admins are allowed to do this.");
                context.Fail();
            }

            return Task.CompletedTask;
        }

    }
}
