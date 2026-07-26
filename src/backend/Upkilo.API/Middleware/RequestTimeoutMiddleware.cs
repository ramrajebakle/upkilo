namespace Upkilo.API.Middleware;

/// <summary>
/// Enforces a maximum request processing timeout.
/// Cancels requests that take longer than the configured duration.
/// Prevents slow queries from consuming thread pool resources.
/// </summary>
public class RequestTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TimeSpan _timeout;

    public RequestTimeoutMiddleware(RequestDelegate next, TimeSpan? timeout = null)
    {
        _next = next;
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalToken = context.RequestAborted;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(originalToken);
        cts.CancelAfter(_timeout);

        // Replace the request cancellation token with our timeout-aware one
        context.RequestAborted = cts.Token;

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !originalToken.IsCancellationRequested)
        {
            // Timeout occurred (not client disconnect)
            context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Request Timeout",
                message = "The request took too long to process. Please try again.",
                correlationId = context.Items["CorrelationId"]?.ToString()
            });
        }
    }
}
