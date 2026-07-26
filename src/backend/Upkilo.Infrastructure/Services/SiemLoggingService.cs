using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for forwarding security events to a SIEM (Security Information and Event Management) system.
/// </summary>
public class SiemLoggingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SiemLoggingService> _logger;
    private readonly string? _siemEndpoint;

    public SiemLoggingService(HttpClient httpClient, IConfiguration configuration, ILogger<SiemLoggingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _siemEndpoint = configuration["Siem:Endpoint"];
    }

    public async Task ForwardEventAsync(string eventType, object details, Guid? userId = null, Guid? tenantId = null)
    {
        var payload = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = eventType,
            Details = details,
            UserId = userId,
            TenantId = tenantId,
            Source = "Upkilo-Backend"
        };

        _logger.LogInformation("Security Event: {EventType} for User: {UserId}, Tenant: {TenantId}", eventType, userId, tenantId);

        if (string.IsNullOrEmpty(_siemEndpoint))
        {
            _logger.LogWarning("SIEM endpoint not configured. Event not forwarded.");
            return;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync(_siemEndpoint, payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to forward event to SIEM. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding security event to SIEM.");
        }
    }

    /// <summary>
    /// Convenience method for logging security events with severity.
    /// </summary>
    public void LogSecurityEvent(string eventType, string details, SecurityEventSeverity severity)
    {
        _logger.LogInformation("Security Event [{Severity}]: {EventType} — {Details}", severity, eventType, details);
        _ = ForwardEventAsync(eventType, new { Details = details, Severity = severity.ToString() });
    }
}

public enum SecurityEventSeverity
{
    Low,
    Medium,
    High,
    Critical
}
