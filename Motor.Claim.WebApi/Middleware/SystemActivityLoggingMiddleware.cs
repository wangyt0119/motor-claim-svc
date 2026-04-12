using System.Diagnostics;
using System.Security.Claims;
using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.WebApi.Middleware
{
    public class SystemActivityLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public SystemActivityLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, SystemMonitoringService systemMonitoringService)
        {
            var stopwatch = Stopwatch.StartNew();
            string? errorMessage = null;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                if (context.Response.StatusCode < 400)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                throw;
            }
            finally
            {
                stopwatch.Stop();

                var path = context.Request.Path.Value ?? string.Empty;
                if (!path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
                {
                    var log = new SystemActivityLogEntity
                    {
                        LogId = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                        UserId = TryParseGuid(context.User.FindFirstValue("UserId")),
                        UserRole = context.User.FindFirstValue(ClaimTypes.Role) ?? context.User.FindFirstValue("role"),
                        UserEmail = context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email"),
                        Module = ResolveModule(path),
                        Action = ResolveAction(context.Request.Method),
                        HttpMethod = context.Request.Method,
                        Path = path,
                        QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                        StatusCode = context.Response.StatusCode,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        IsSuccess = context.Response.StatusCode < 400,
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        ErrorMessage = errorMessage
                    };

                    await systemMonitoringService.LogAsync(log);
                }
            }
        }

        private static Guid? TryParseGuid(string? value)
        {
            return Guid.TryParse(value, out var result) ? result : null;
        }

        private static string ResolveModule(string path)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase))
            {
                return segments[1];
            }

            return "system";
        }

        private static string ResolveAction(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => "Read",
                "POST" => "Create",
                "PUT" => "Update",
                "PATCH" => "Update",
                "DELETE" => "Delete",
                _ => "Request"
            };
        }
    }
}
