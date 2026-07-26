using System.Security.Cryptography;

namespace Upkilo.API.Middleware;

/// <summary>
/// Adds OWASP-recommended security headers.
/// C4: Generates a per-request cryptographic nonce and uses it in Content-Security-Policy
/// instead of 'unsafe-inline', eliminating inline script/style injection vectors.
/// The nonce is stored at HttpContext.Items["csp-nonce"] and exposed as X-CSP-Nonce
/// so Swagger UI and any server-rendered HTML can reference it.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // C4: generate nonce before the request so handlers can read it
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items["csp-nonce"] = nonce;

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            if (!headers.ContainsKey("X-Content-Type-Options"))
                headers.Append("X-Content-Type-Options", "nosniff");

            if (!headers.ContainsKey("X-Frame-Options"))
                headers.Append("X-Frame-Options", "DENY");

            if (!headers.ContainsKey("X-XSS-Protection"))
                headers.Append("X-XSS-Protection", "1; mode=block");

            if (!headers.ContainsKey("Referrer-Policy"))
                headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            if (!headers.ContainsKey("Strict-Transport-Security"))
                headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

            if (!headers.ContainsKey("Permissions-Policy"))
                headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");

            // C4: nonce-based CSP — 'unsafe-inline' removed; only scripts/styles tagged with this nonce execute
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                var csp = $"default-src 'none'; " +
                          $"script-src 'self' 'nonce-{nonce}' https://js.stripe.com; " +
                          $"style-src 'self' 'nonce-{nonce}' https://fonts.googleapis.com; " +
                          $"img-src 'self' data: https:; " +
                          $"font-src 'self' https://fonts.gstatic.com; " +
                          $"connect-src 'self' https://api.upkilo.com https://api.stripe.com; " +
                          $"frame-src https://js.stripe.com https://hooks.stripe.com; " +
                          $"frame-ancestors 'none'; " +
                          $"base-uri 'self'; " +
                          $"form-action 'self';";
                headers.Append("Content-Security-Policy", csp);
            }

            // Expose the nonce so Swagger UI or any partial HTML can read it from the response
            if (!headers.ContainsKey("X-CSP-Nonce"))
                headers.Append("X-CSP-Nonce", nonce);

            return Task.CompletedTask;
        });

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
        }

        await _next(context);
    }
}
