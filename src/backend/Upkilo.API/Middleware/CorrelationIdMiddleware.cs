using Serilog.Context;

namespace Upkilo.API.Middleware;

/// <summary>
/// Adds a unique correlation ID to every request for distributed tracing.
/// Reads X-Correlation-ID from incoming request or generates a new one.
/// Pushes it into Serilog context and response headers.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use existing correlation ID from upstream or generate new
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12]; // Short 12-char ID

        // Make available throughout the request
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Push into Serilog so all log entries include it
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
