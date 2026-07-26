using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Middleware;

/// <summary>
/// Implements Task 1343: Account farming detection
/// Implements Task 1342: Rate limit by user ID/IP (Refined)
/// Implements Task 1348: CSP Enforcement
/// </summary>
public class SecurityEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityEnforcementMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, int> _signupAttempts = new();

    public SecurityEnforcementMiddleware(RequestDelegate next, ILogger<SecurityEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // 1. Account Farming Detection (Task 1343)
        if (context.Request.Path.Value?.Contains("/api/auth/register", StringComparison.OrdinalIgnoreCase) == true)
        {
            var count = _signupAttempts.AddOrUpdate(ip, 1, (key, val) => val + 1);
            if (count > 5) // > 5 signups per IP 
            {
                _logger.LogWarning("Account farming detected from IP: {IP}. Blocking request.", ip);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Signup limit exceeded for this IP. Potential account farming detected.");
                return;
            }
        }

        // 2. CSP Headers (Task 1348)
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        await _next(context);
    }
}
