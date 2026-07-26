using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for sending Slack notifications to tenant channels (e.g., new booking, payment received).
/// Operates using Slack Incoming Webhooks or Slack Bot Tokens.
/// </summary>
public class SlackNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SlackNotificationService> _logger;

    public SlackNotificationService(HttpClient httpClient, ILogger<SlackNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Sends a message to a Slack channel using an Incoming Webhook URL.
    /// </summary>
    public async Task<bool> SendNotificationAsync(string webhookUrl, string message, string? channel = null)
    {
        // VULN-013: Validate user-configured webhook URL before making outbound request
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var slackUri) ||
            (slackUri.Scheme != "http" && slackUri.Scheme != "https"))
        {
            _logger.LogWarning("SSRF blocked Slack webhook — invalid URL: {Url}", webhookUrl);
            return false;
        }
        try
        {
            var addrs = await Dns.GetHostAddressesAsync(slackUri.Host);
            if (addrs.Any(a => IPAddress.IsLoopback(a) || IsPrivateIp(a)))
            {
                _logger.LogWarning("SSRF blocked Slack webhook to private IP: {Url}", webhookUrl);
                return false;
            }
        }
        catch (SocketException) { return false; }

        try
        {
            var payload = new
            {
                text = message,
                channel = channel
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(webhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send Slack notification. Status: {Status}, Error: {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Successfully sent Slack notification to webhook.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Slack notification to webhook {WebhookUrl}", webhookUrl);
            return false;
        }
    }

    private static bool IsPrivateIp(IPAddress addr)
    {
        var b = addr.GetAddressBytes();
        if (addr.AddressFamily == AddressFamily.InterNetwork && b.Length == 4)
            return b[0] == 10 ||
                   (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                   (b[0] == 192 && b[1] == 168) ||
                   (b[0] == 169 && b[1] == 254);
        return false;
    }
}
