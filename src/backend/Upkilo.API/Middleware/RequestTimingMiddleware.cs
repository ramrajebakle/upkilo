namespace Upkilo.API.Middleware;

/// <summary>
/// Middleware that adds X-Response-Time header to every outgoing response.
/// Helps frontend clients and monitoring tools measure API latency.
/// </summary>
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestTimingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();

        context.Response.OnStarting(() =>
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp);
            context.Response.Headers["X-Response-Time"] = $"{elapsed.TotalMilliseconds:F0}ms";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
