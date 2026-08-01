using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Security event logger for SIEM integration (sends events to external webhook/Event Hub)
/// </summary>
public class SiemIntegrationService
{
    private readonly ILogger<SiemIntegrationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _siemEndpoint;

    public SiemIntegrationService(ILogger<SiemIntegrationService> logger, IHttpClientFactory httpClientFactory, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _siemEndpoint = configuration["Security:SiemEndpoint"];
    }

    /// <summary>
    /// Forwards a security event to the configured SIEM endpoint
    /// </summary>
    public async Task LogSecurityEventAsync(SiemEvent siemEvent)
    {
        _logger.LogInformation("SIEM event: {EventType} for tenant {TenantId} — {Details}",
            siemEvent.EventType, siemEvent.TenantId, siemEvent.Details);

        if (string.IsNullOrEmpty(_siemEndpoint))
        {
            _logger.LogDebug("No SIEM endpoint configured. Event logged locally only.");
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("SIEM");
            var payload = System.Text.Json.JsonSerializer.Serialize(siemEvent);
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_siemEndpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SIEM event delivery failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver SIEM event: {EventType}", siemEvent.EventType);
        }
    }
}

public class SiemEvent
{
    public string EventType { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
