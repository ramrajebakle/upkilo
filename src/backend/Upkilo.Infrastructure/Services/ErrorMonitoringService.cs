using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

public class ErrorMonitoringService
{
    private readonly ILogger<ErrorMonitoringService> _logger;
    private readonly TelemetryClient? _telemetryClient;
    private readonly string? _environment;
    private readonly PagerDutyService _pagerDuty;

    public ErrorMonitoringService(
        ILogger<ErrorMonitoringService> logger,
        IConfiguration configuration,
        PagerDutyService pagerDuty,
        TelemetryClient? telemetryClient = null)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
        _environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        _pagerDuty = pagerDuty;
    }

    public async Task CaptureErrorAsync(Exception ex, string? userId = null, string? tenantId = null)
    {
        _logger.LogError(ex, "Error captured. User: {User}, Tenant: {Tenant}", userId, tenantId);

        if (_telemetryClient != null)
        {
            var telemetry = new ExceptionTelemetry(ex);
            if (userId != null) telemetry.Properties["UserId"] = userId;
            if (tenantId != null) telemetry.Properties["TenantId"] = tenantId;
            telemetry.Properties["Environment"] = _environment ?? "unknown";
            _telemetryClient.TrackException(telemetry);
            _telemetryClient.Flush();
        }

        await Task.CompletedTask;
    }

    public async Task TriggerAlertAsync(string alertName, string severity)
    {
        _logger.LogCritical("ALERT: {Name} (Severity: {Severity})", alertName, severity);

        if (_telemetryClient != null)
        {
            _telemetryClient.TrackEvent("Alert", new Dictionary<string, string>
            {
                ["AlertName"] = alertName,
                ["Severity"] = severity,
                ["Environment"] = _environment ?? "unknown",
                ["Timestamp"] = DateTime.UtcNow.ToString("O")
            });
            _telemetryClient.Flush();
        }

        // Escalate to PagerDuty for critical and error severities
        var sev = severity.ToLowerInvariant();
        if (sev == "critical" || sev == "error")
        {
            await _pagerDuty.TriggerAlertAsync(
                summary: alertName,
                severity: sev,
                source: $"ErrorMonitoringService ({_environment})",
                details: new { alertName, severity, environment = _environment, timestamp = DateTime.UtcNow });
        }
    }
}
