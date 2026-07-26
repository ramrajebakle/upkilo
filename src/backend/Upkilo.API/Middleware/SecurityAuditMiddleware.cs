using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Middleware;

/// <summary>
/// Logs all security-relevant events: auth failures, rate limits, tenant access violations, suspicious patterns.
/// </summary>
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;
    private readonly IAuditLogService _auditLogService;

    public SecurityAuditMiddleware(
        RequestDelegate next,
        ILogger<SecurityAuditMiddleware> logger,
        IAuditLogService auditLogService)
    {
        _next = next;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;

        await _next(context);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        var statusCode = context.Response.StatusCode;
        var path = context.Request.Path.Value;

        var userIdStr = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var tenantIdStr = context.Items["TenantId"]?.ToString() ?? context.User?.FindFirst("tenant_id")?.Value;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        Guid? userId = Guid.TryParse(userIdStr, out var uid) ? uid : null;
        Guid? tenantId = Guid.TryParse(tenantIdStr, out var tid) ? tid : null;

        if (statusCode == 401 || statusCode == 403 || statusCode == 429 || elapsed > 10000)
        {
            var eventType = statusCode switch
            {
                401 => "AUTH_FAILURE",
                403 => "FORBIDDEN_ACCESS",
                429 => "RATE_LIMIT_EXCEEDED",
                _ => "SLOW_REQUEST"
            };

            var severity = statusCode == 429 || elapsed > 30000 ? SecuritySeverity.High : SecuritySeverity.Warning;

            _logger.LogWarning(
                "SECURITY: {EventType} [{StatusCode}] on {Path} by User={UserId} IP={IP}",
                eventType, statusCode, path, userIdStr ?? "anonymous", ip);

            // Non-blocking enqueue — never writes to DB in the hot path.
            // BufferedAuditLogService flushes in batches every 5 s.
            _auditLogService.Log(new AuditEntry
            {
                TenantId = tenantId ?? Guid.Empty,
                UserId = userId,
                Action = eventType,
                EntityType = "SecurityEvent",
                EntityId = path ?? string.Empty,
                IpAddress = ip,
                UserAgent = userAgent,
                Details = $"StatusCode={statusCode} Elapsed={elapsed:F0}ms Severity={severity}",
                Timestamp = DateTime.UtcNow
            });

            if (severity == SecuritySeverity.High || severity == SecuritySeverity.Critical)
            {
                // Fire-and-forget the alert — do not await so it never blocks the response.
                // Scoped services are captured via a new scope to avoid scope-disposed errors.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = context.RequestServices.CreateScope();
                        var mediator = scope.ServiceProvider.GetService<MediatR.IMediator>();
                        if (mediator != null)
                        {
                            await mediator.Publish(new Upkilo.Infrastructure.Background.SecurityAlertNotification
                            {
                                TenantId = tenantId ?? Guid.Empty,
                                UserId = userId,
                                EventType = eventType,
                                Severity = severity.ToString(),
                                IpAddress = ip,
                                Description = $"Request to {path} returned {statusCode}"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SecurityAuditMiddleware: failed to dispatch high-severity alert for {EventType}", eventType);
                    }
                });
            }
        }
    }
}
