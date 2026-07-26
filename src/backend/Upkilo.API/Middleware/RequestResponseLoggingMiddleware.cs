using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Upkilo.API.Middleware;

/// <summary>
/// Middleware for logging HTTP request and response bodies.
/// Integrated with Serilog PiiRedactionEnricher for security.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly bool _isDevelopment;
    private const int MaxBodyLength = 10000; // Limit logging to 10KB

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _isDevelopment = env.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for certain paths (e.g., health, swagger, files)
        if (IsIgnoredPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // 1. Log Request
        string requestBody = await ReadRequestBody(context.Request);
        _logger.LogInformation("HTTP Request: {Method} {Path} {Query} Body: {Body}", 
            context.Request.Method, 
            context.Request.Path, 
            context.Request.QueryString,
            Truncate(requestBody, MaxBodyLength));

        // 2. Capture and log response body — development only.
        // In production, response bodies can contain PII (user records, tokens, PII fields).
        // The status code is always logged; body capture is too expensive and risky for prod.
        if (_isDevelopment)
        {
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                string responseBodyText = await ReadResponseBody(context.Response);
                _logger.LogInformation("HTTP Response: {StatusCode} Body: {Body}",
                    context.Response.StatusCode,
                    Truncate(responseBodyText, MaxBodyLength));

                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during request processing");
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
        else
        {
            try
            {
                await _next(context);
                _logger.LogInformation("HTTP Response: {StatusCode}", context.Response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during request processing");
                throw;
            }
        }
    }

    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        if (request.ContentLength == 0) return string.Empty;

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private async Task<string> ReadResponseBody(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    // Auth endpoints carry passwords, tokens, and 2FA codes in their request/response bodies.
    // Exclude any path whose lowercase value contains any of these substrings so that
    // BOTH the unversioned (/api/auth/...) AND versioned (/api/v1/auth/..., /api/v2/auth/...)
    // routes are suppressed regardless of future API version changes.
    private static readonly string[] _sensitivePathSubstrings =
    {
        "/auth/login", "/auth/register", "/auth/reset-password",
        "/auth/change-password", "/auth/verify-2fa", "/auth/forgot-password",
        "/auth/send-2fa", "/auth/social/", "/auth/refresh",
        "/super-admin/login", "/super-admin/verify-2fa", "/super-admin/setup-2fa",
        "/publicinvitation/accept", "/auth/verify-email"
    };

    private static bool IsIgnoredPath(PathString path)
    {
        var p = path.Value?.ToLower() ?? "";
        // Use Contains so versioned (/api/v1/auth/login) and unversioned paths both match
        if (_sensitivePathSubstrings.Any(sub => p.Contains(sub, StringComparison.Ordinal)))
            return true;

        return p.Contains("/health") ||
               p.Contains("/swagger") ||
               p.Contains("/hubs/") ||
               p.Contains("/files/") ||
               p.EndsWith(".js") ||
               p.EndsWith(".css") ||
               p.EndsWith(".png") ||
               p.EndsWith(".jpg");
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "... [TRUNCATED]";
    }
}
