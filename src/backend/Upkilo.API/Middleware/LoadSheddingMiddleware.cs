using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Middleware;

/// <summary>
/// Load shedding middleware.
/// Drops incoming requests with 503 if the system load (CPU/memory) is critically high.
/// Bypasses critical endpoints like Webhooks and Health checks.
/// </summary>
public class LoadSheddingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoadSheddingMiddleware> _logger;
    private readonly ISystemLoadMonitorService _loadMonitor;

    public LoadSheddingMiddleware(RequestDelegate next, ILogger<LoadSheddingMiddleware> logger, ISystemLoadMonitorService loadMonitor)
    {
        _next = next;
        _logger = logger;
        _loadMonitor = loadMonitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Always allow critical endpoints
        if (path.Contains("/health") || path.Contains("/webhooks") || path.Contains("/billing/stripe-webhook"))
        {
            await _next(context);
            return;
        }

        if (_loadMonitor.IsSystemOverloaded())
        {
            _logger.LogWarning("Load shedding active. System overloaded. Rejecting request to {Path}", path);
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            context.Response.Headers["Retry-After"] = "10";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "System is currently experiencing high load. Please try again later.",
                retryAfter = 10
            });
            return;
        }

        await _next(context);
    }
}
