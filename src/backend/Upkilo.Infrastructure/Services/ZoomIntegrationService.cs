using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for managing Zoom video conferencing integration (creating/deleting meetings).
/// Uses OAuth2 Server-to-Server or JWT app credentials.
/// </summary>
public class ZoomIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZoomIntegrationService> _logger;

    public ZoomIntegrationService(HttpClient httpClient, IConfiguration configuration, ILogger<ZoomIntegrationService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.zoom.us/v2/");
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> CreateMeetingAsync(Guid tenantId, string topic, DateTime startTime, int durationMinutes, string timezone)
    {
        try
        {
            // In a real implementation, you'd fetch the tenant's specific Zoom OAuth token.
            // For now, using a configured account-level token or server-to-server OAuth.
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                topic = topic,
                type = 2, // Scheduled meeting
                start_time = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = durationMinutes,
                timezone = timezone,
                settings = new
                {
                    host_video = true,
                    participant_video = true,
                    join_before_host = false,
                    mute_upon_entry = true,
                    waiting_room = true
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("users/me/meetings", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create Zoom meeting. Status: {Status}, Error: {Error}", response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(result);
            return doc.RootElement.GetProperty("join_url").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Zoom meeting for tenant {TenantId}", tenantId);
            return null;
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var accountId = _configuration["Zoom:AccountId"];
        var clientId = _configuration["Zoom:ClientId"];
        var clientSecret = _configuration["Zoom:ClientSecret"];

        if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            throw new InvalidOperationException(
                "Zoom credentials are not configured. Set Zoom:AccountId, Zoom:ClientId, and Zoom:ClientSecret in application settings.");

        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "account_credentials"),
            new KeyValuePair<string, string>("account_id", accountId)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://zoom.us/oauth/token")
        {
            Content = requestContent
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error getting Zoom S2S Token: {Error}", error);
            throw new Exception("Zoom OAuth failed.");
        }

        var result = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(result);
        return doc.RootElement.GetProperty("access_token").GetString() ?? "";
    }
}
