using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Sends alerts to PagerDuty via Events API v2.
/// Fire-and-forget: never throws; logs failures.
/// </summary>
public class PagerDutyService
{
    private const string EventsUrl = "https://events.pagerduty.com/v2/enqueue";

    private readonly ILogger<PagerDutyService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _integrationKey;

    public PagerDutyService(
        ILogger<PagerDutyService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _integrationKey = configuration["PagerDuty:IntegrationKey"] ?? string.Empty;
    }

    /// <summary>
    /// Triggers a PagerDuty alert.
    /// </summary>
    /// <param name="summary">Human-readable summary.</param>
    /// <param name="severity">critical | error | warning | info</param>
    /// <param name="source">Component/service that fired the alert.</param>
    /// <param name="details">Optional additional context.</param>
    public async Task TriggerAlertAsync(string summary, string severity, string source, object? details = null)
    {
        if (string.IsNullOrWhiteSpace(_integrationKey))
        {
            _logger.LogDebug("PagerDuty integration key not configured, skipping alert: {Summary}", summary);
            return;
        }

        // PagerDuty severity values: critical | error | warning | info
        var pdSeverity = severity.ToLowerInvariant() switch
        {
            "critical" => "critical",
            "error" => "error",
            "warning" => "warning",
            _ => "info"
        };

        var payload = new
        {
            routing_key = _integrationKey,
            event_action = "trigger",
            payload = new
            {
                summary,
                severity = pdSeverity,
                source,
                timestamp = DateTime.UtcNow.ToString("O"),
                custom_details = details
            }
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(EventsUrl, content);
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                _logger.LogInformation("PagerDuty alert sent: {Summary} (severity={Severity})", summary, pdSeverity);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("PagerDuty alert failed: {Status} {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending PagerDuty alert: {Summary}", summary);
        }
    }
}
