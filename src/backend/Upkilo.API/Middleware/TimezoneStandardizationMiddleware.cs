using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Upkilo.API.Middleware;

/// <summary>
/// Intercepts requests and standardizes timezone info from headers (e.g. 'X-Timezone')
/// injecting it into context or validating offset preferences.
/// </summary>
public class TimezoneStandardizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TimezoneStandardizationMiddleware> _logger;

    public TimezoneStandardizationMiddleware(RequestDelegate next, ILogger<TimezoneStandardizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Timezone", out var tzHeader) && !string.IsNullOrEmpty(tzHeader))
        {
            try
            {
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tzHeader.ToString());
                context.Items["ClientTimeZone"] = tzInfo;
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogWarning("Invalid timezone provided by client: {TimeZone}", tzHeader);
                // Fallback to UTC
                context.Items["ClientTimeZone"] = TimeZoneInfo.Utc;
            }
        }
        else
        {
            context.Items["ClientTimeZone"] = TimeZoneInfo.Utc;
        }

        await _next(context);
    }
}
