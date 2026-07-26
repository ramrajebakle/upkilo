using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Upkilo.API.Middleware;

/// <summary>
/// Ensures POST/PUT/PATCH requests with an 'Idempotency-Key' header are not processed twice.
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache, Upkilo.Core.Interfaces.ITenantProvider tenantProvider)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var headerValue))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = headerValue.ToString();
        var tenantId = tenantProvider.GetTenantId()?.ToString() ?? "anonymous";
        var cacheKey = $"IdempotencyKey_{tenantId}_{idempotencyKey}";

        var existingResponse = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(existingResponse))
        {
            _logger.LogInformation("Idempotency key {Key} matched. Returning cached response.", idempotencyKey);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status200OK; // Assumed success if cached
            await context.Response.WriteAsync(existingResponse);
            return;
        }

        // Intercept response body
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context);

        // Only cache successful mutations (200, 201, 202, 204)
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
            memoryStream.Seek(0, SeekOrigin.Begin);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };
            
            await cache.SetStringAsync(cacheKey, responseBody, options);
            
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        else
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBodyStream);
        }
    }
}
