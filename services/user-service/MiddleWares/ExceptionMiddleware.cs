using System.Net;
using System.Text.Json;

using user_service.Exceptions;

namespace user_service.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DuplicateEmailException ex)
            {
                _logger.LogWarning("Duplicate email detected: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
            }
            catch (UserNotFoundException ex)
            {
                _logger.LogWarning("User not found: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception caught by middleware");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)httpStatusCode;

            var response = new { message = exception.Message };
            var json = JsonSerializer.Serialize(response);

            return context.Response.WriteAsync(json);
        }
    }
}
