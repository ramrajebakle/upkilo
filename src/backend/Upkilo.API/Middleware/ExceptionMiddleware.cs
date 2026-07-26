using System.Net;
using System.Text.Json;

namespace Upkilo.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // L-10 FIX: Serilog captures the exception object — no need to interpolate Message
        _logger.LogError(exception, "Unhandled exception occurred");

        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        // C-7 FIX: Never leak internal exception messages to external clients in production.
        // Internal messages may contain SQL fragments, column names, connection strings, or
        // service URLs that aid attacker reconnaissance.
        var safeDetail = _env.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred. Please try again or contact support.";

        var problemDetails = new
        {
            type = $"https://httpstatuses.io/{(int)statusCode}",
            title = GetTitle(statusCode),
            status = (int)statusCode,
            detail = safeDetail,
            instance = context.Request.Path.Value,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow.ToString("O"),
            stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static string GetTitle(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "Bad Request",
        HttpStatusCode.Unauthorized => "Unauthorized",
        HttpStatusCode.NotFound => "Not Found",
        HttpStatusCode.InternalServerError => "Internal Server Error",
        _ => "An error occurred"
    };
}
