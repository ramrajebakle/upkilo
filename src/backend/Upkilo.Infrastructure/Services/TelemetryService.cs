using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for standardized telemetry and custom metrics using Azure Application Insights.
/// </summary>
public class TelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(TelemetryClient telemetryClient, ILogger<TelemetryService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public void TrackEvent(string name, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackEvent(name, properties);
    }

    public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackMetric(name, value, properties);
    }

    public void TrackException(Exception ex, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackException(ex, properties);
    }

    public IOperationHolder<RequestTelemetry> StartOperation(string operationName)
    {
        return _telemetryClient.StartOperation<RequestTelemetry>(operationName);
    }
}
