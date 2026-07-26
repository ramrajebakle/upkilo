using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Middleware;

/// <summary>
/// .NET 8 Global Exception Handler for standardizing all unhandled errors
/// into RFC 7807 ProblemDetails format.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly TelemetryService _telemetry;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, TelemetryService telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        try
        {
            _telemetry.TrackException(exception, new Dictionary<string, string>
            {
                { "Path", httpContext.Request.Path },
                { "Method", httpContext.Request.Method }
            });
        }
        catch (Exception telemetryEx)
        {
            _logger.LogError(telemetryEx, "Failed to track exception in telemetry.");
        }

        // VULN-A05 FIX: exception.Message must never reach callers in production — it can expose
        // connection strings, DB schema, internal hostnames, and query fragments.
        // In development the full message is kept for debuggability.
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var correlationId = httpContext.Items.TryGetValue("CorrelationId", out var cid)
            ? cid?.ToString() ?? httpContext.TraceIdentifier
            : httpContext.TraceIdentifier;
        var safeDetail = env.IsDevelopment()
            ? exception.Message
            : $"An internal error occurred. Reference: {correlationId}";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred while processing your request.",
            Detail = safeDetail,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Instance = httpContext.Request.Path
        };

        // Customize output based on exception type securely
        if (exception is UnauthorizedAccessException)
        {
            problemDetails.Status = StatusCodes.Status401Unauthorized;
            problemDetails.Title = "Unauthorized access";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7235#section-3.1";
        }
        else if (exception is ArgumentException || exception is InvalidOperationException)
        {
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Title = "Bad request";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
