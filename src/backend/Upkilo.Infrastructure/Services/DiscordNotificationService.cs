using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for sending Discord notifications via Webhooks.
/// </summary>
public class DiscordNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiscordNotificationService> _logger;

    public DiscordNotificationService(HttpClient httpClient, ILogger<DiscordNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendNotificationAsync(string webhookUrl, string content, string? username = "Upkilo Bot")
    {
        // VULN-013: Validate user-configured webhook URL before making outbound request.
        // Inline check mirrors SsrfPreventionMiddleware.ValidateUrlAsync — the middleware
        // lives in Upkilo.API which cannot be referenced from Upkilo.Infrastructure.
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var webhookUri) ||
            (webhookUri.Scheme != "http" && webhookUri.Scheme != "https"))
        {
            _logger.LogWarning("SSRF blocked Discord webhook — invalid URL: {Url}", webhookUrl);
            return false;
        }
        try
        {
            var addrs = await Dns.GetHostAddressesAsync(webhookUri.Host);
            if (addrs.Any(a => IPAddress.IsLoopback(a) || IsPrivateIp(a)))
            {
                _logger.LogWarning("SSRF blocked Discord webhook to private IP: {Url}", webhookUrl);
                return false;
            }
        }
        catch (SocketException) { return false; }

        try
        {
            var payload = new
            {
                content = content,
                username = username
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(webhookUrl, httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Discord notification failed. Status: {Status}, Error: {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Discord notification to {WebhookUrl}", webhookUrl);
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
