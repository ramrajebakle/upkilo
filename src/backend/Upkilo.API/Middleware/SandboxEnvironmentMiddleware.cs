using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Upkilo.API.Middleware;

/// <summary>
/// Sandbox Environment Middleware.
/// Detects 'X-Sandbox-Mode' header and overrides environment context for testing.
/// Useful for API Sandbox environments.
/// </summary>
public class SandboxEnvironmentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SandboxEnvironmentMiddleware> _logger;

    public SandboxEnvironmentMiddleware(RequestDelegate next, ILogger<SandboxEnvironmentMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Sandbox-Mode", out var sandboxHeader) &&
            sandboxHeader == "true")
        {
            context.Items["IsSandbox"] = true;
            context.Response.Headers["X-Sandbox-Mode-Active"] = "true";
            _logger.LogInformation("Sandbox mode activated for request {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        await _next(context);
    }
}
