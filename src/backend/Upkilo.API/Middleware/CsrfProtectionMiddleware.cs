using Microsoft.AspNetCore.Antiforgery;

namespace Upkilo.API.Middleware;

/// <summary>
/// CSRF protection middleware enforcing SameSite cookies and anti-CSRF token validation
/// for state-changing requests (POST, PUT, PATCH, DELETE).
/// API endpoints authenticated via Bearer JWT are exempt (stateless auth).
/// </summary>
public class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS", "TRACE"
    };

    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/login",
        "/api/v1/auth/register",
        "/api/v1/auth/refresh",
        "/api/v1/stripe-webhook",
        "/api/v1/webhooks/incoming",
        "/api/v1/public-booking",
        "/api/v1/public-invitation",
        "/health",
        "/ready"
    };

    public CsrfProtectionMiddleware(RequestDelegate next, ILogger<CsrfProtectionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Safe methods don't need CSRF protection
        if (SafeMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Bearer token / API key authenticated requests are stateless — exempt from CSRF
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) &&
            (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
             authHeader.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Check X-API-KEY header (API key auth)
        if (context.Request.Headers.ContainsKey("X-API-KEY"))
        {
            await _next(context);
            return;
        }

        // Exempt paths (webhooks, public endpoints)
        var path = context.Request.Path.Value ?? "";
        if (ExemptPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // For cookie-authenticated requests: validate Origin/Referer header
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        var referer = context.Request.Headers.Referer.FirstOrDefault();
        var host = context.Request.Host.Value;

        // VULN-007 FIX: Reject cookie-authenticated state-mutating requests that carry
        // neither an Origin nor a Referer header.  Previously the check was silently skipped
        // when both were absent, allowing CSRF from clients that strip those headers
        // (some Tor browsers, certain browser extensions, or old mobile WebViews).
        var hasCookieAuthEarly = context.Request.Cookies.ContainsKey("token") &&
            !context.Request.Headers.ContainsKey("Authorization") &&
            !context.Request.Headers.ContainsKey("X-API-Key");

        if (hasCookieAuthEarly && string.IsNullOrEmpty(origin) && string.IsNullOrEmpty(referer))
        {
            _logger.LogWarning(
                "CSRF: State-changing cookie-auth request with no Origin or Referer. Path={Path}, IP={IP}",
                path, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Forbidden",
                status = 403,
                detail = "Request origin could not be verified."
            });
            return;
        }

        if (!string.IsNullOrEmpty(origin))
        {
            if (!IsValidOrigin(origin, host))
            {
                _logger.LogWarning(
                    "CSRF: Origin mismatch. Origin={Origin}, Host={Host}, Path={Path}",
                    origin, host, path);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    title = "Forbidden",
                    status = 403,
                    detail = "Cross-origin request blocked by CSRF protection."
                });
                return;
            }
        }
        else if (!string.IsNullOrEmpty(referer))
        {
            if (!IsValidReferer(referer, host))
            {
                _logger.LogWarning(
                    "CSRF: Referer mismatch. Referer={Referer}, Host={Host}, Path={Path}",
                    referer, host, path);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    title = "Forbidden",
                    status = 403,
                    detail = "Cross-origin request blocked by CSRF protection."
                });
                return;
            }
        }

        // Check X-CSRF-TOKEN header for cookie-based auth state-changing requests
        var csrfToken = context.Request.Headers["X-CSRF-TOKEN"].FirstOrDefault();
        var csrfCookie = context.Request.Cookies["XSRF-TOKEN"];

        var hasCookieAuth = context.Request.Cookies.ContainsKey("token") &&
            !context.Request.Headers.ContainsKey("Authorization") &&
            !context.Request.Headers.ContainsKey("X-API-Key");

        if (hasCookieAuth)
        {
            if (string.IsNullOrEmpty(csrfCookie))
            {
                _logger.LogWarning("CSRF Validation Failed: Missing CSRF cookie for cookie-authenticated request.");
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Missing CSRF token." });
                return;
            }

            // H-01 FIX: Use constant-time comparison to prevent timing side-channel attacks
            if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(csrfToken ?? ""),
                System.Text.Encoding.UTF8.GetBytes(csrfCookie ?? "")))
            {
                _logger.LogWarning("CSRF Validation Failed: Token mismatch.");
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid CSRF token." });
                return;
            }
        }

        await _next(context);
    }

    private bool IsValidOrigin(string origin, string host)
    {
        try
        {
            var originUri = new Uri(origin);
            var originHost = originUri.Host;
            var isUpkilo = host.Equals("upkilo.com", StringComparison.OrdinalIgnoreCase) ||
                           host.EndsWith(".upkilo.com", StringComparison.OrdinalIgnoreCase) ||
                           originHost.Equals("upkilo.com", StringComparison.OrdinalIgnoreCase) ||
                           originHost.EndsWith(".upkilo.com", StringComparison.OrdinalIgnoreCase);
            // Localhost is only a valid origin in development — never allow it in production or staging.
            var isLocalhost = _environment.IsDevelopment() &&
                              (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                               originHost.Equals("localhost", StringComparison.OrdinalIgnoreCase));
            return isUpkilo || isLocalhost;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidReferer(string referer, string host)
    {
        try
        {
            var refererUri = new Uri(referer);
            var refererHost = refererUri.Host;
            if (refererUri.Port != 80 && refererUri.Port != 443)
                refererHost += ":" + refererUri.Port;

            return string.Equals(refererHost, host, StringComparison.OrdinalIgnoreCase) ||
                   host.EndsWith(".upkilo.com", StringComparison.OrdinalIgnoreCase) ||
                   refererHost.EndsWith(".upkilo.com", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Extension methods for configuring CSRF protection
/// </summary>
public static class CsrfProtectionExtensions
{
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CsrfProtectionMiddleware>();
    }
}
