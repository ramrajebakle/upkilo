namespace Upkilo.API.Middleware;

/// <summary>
/// Adds RFC 8594 Deprecation and Sunset headers to deprecated API endpoints,
/// giving clients advance notice before removal.
/// </summary>
public class ApiDeprecationMiddleware
{
    private readonly RequestDelegate _next;

    // Key: path prefix (case-insensitive), Value: sunset date
    private static readonly Dictionary<string, DateTime> DeprecatedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Example: ["/api/v1/legacy-endpoint"] = new DateTime(2026, 12, 31)
    };

    public ApiDeprecationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        foreach (var (prefix, sunsetDate) in DeprecatedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers["Deprecation"] = "true";
                context.Response.Headers["Sunset"] = sunsetDate.ToUniversalTime().ToString("R");
                context.Response.Headers["Cache-Control"] = "no-store";
                break;
            }
        }

        await _next(context);
    }
}
