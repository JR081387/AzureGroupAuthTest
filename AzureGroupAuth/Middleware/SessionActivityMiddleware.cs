using AzureGroupAuth.Services;

namespace AzureGroupAuth.Middleware;

public class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;

    public SessionActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SessionTrackingService sessionTracker)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var oid = context.User.FindFirst("oid")?.Value
                ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (!string.IsNullOrEmpty(oid))
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                sessionTracker.UpdateActivity(oid, ip);
            }
        }

        await _next(context);
    }
}

public static class SessionActivityMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionActivityTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionActivityMiddleware>();
    }
}
