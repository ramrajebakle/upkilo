using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

public class OutlookCalendarService : ICalendarService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OutlookCalendarService> _logger;

    public OutlookCalendarService(AppDbContext context, IConfiguration configuration, ILogger<OutlookCalendarService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public string GetAuthUrl(string provider, Guid staffId)
    {
        var clientId = _configuration["Authentication:Microsoft:ClientId"];
        var redirectUri = _configuration["Authentication:Microsoft:RedirectUri"];
        var tenant = "common"; // Or specific tenant for enterprise
        return $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&response_mode=query&scope=Calendars.ReadWrite Offline_Access&state={staffId}";
    }

    public async Task<CalendarSyncToken> ConnectAsync(string provider, Guid staffId, string code)
    {
        _logger.LogInformation("Connecting Outlook Calendar for staff {StaffId}", staffId);

        var clientId = _configuration["Authentication:Microsoft:ClientId"];
        var clientSecret = _configuration["Authentication:Microsoft:ClientSecret"];
        var redirectUri = _configuration["Authentication:Microsoft:RedirectUri"];

        using var client = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", clientId ?? "" },
            { "client_secret", clientSecret ?? "" },
            { "code", code },
            { "redirect_uri", redirectUri ?? "" },
            { "grant_type", "authorization_code" },
            { "scope", "Calendars.ReadWrite Offline_Access" }
        });

        var response = await client.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to exchange Outlook code: {Error}", error);
            throw new InvalidOperationException("Failed to connect Outlook Calendar: " + error);
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = System.Text.Json.JsonSerializer.Deserialize<OutlookTokenResponse>(json);

        var token = new CalendarSyncToken
        {
            Id = Guid.NewGuid(),
            StaffId = staffId,
            Provider = "outlook",
            AccessToken = tokenData?.AccessToken ?? "",
            RefreshToken = tokenData?.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData?.ExpiresIn ?? 3600),
            CreatedAt = DateTime.UtcNow
        };

        _context.CalendarSyncTokens.Add(token);
        await _context.SaveChangesAsync();

        return token;
    }

    public async Task SyncBookingsAsync(Guid staffId)
    {
        _logger.LogInformation("Syncing Outlook Calendar for staff {StaffId}", staffId);
        var token = await _context.CalendarSyncTokens.FirstOrDefaultAsync(t => t.StaffId == staffId && t.Provider == "outlook");
        if (token == null) return;

        var accessToken = await GetValidAccessTokenAsync(token);

        // Fetch upcoming bookings that haven't been synced or need update
        var upcomingBookings = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Where(b => b.StaffId == staffId && b.StartTime >= DateTime.UtcNow)
            .ToListAsync();

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        foreach (var booking in upcomingBookings)
        {
            try
            {
                var eventPayload = new
                {
                    subject = $"Upkilo: {booking.Service?.Name ?? "Booking"} with {booking.Client?.FirstName ?? "Client"}",
                    body = new
                    {
                        contentType = "HTML",
                        content = $"Booking ID: {booking.Id}<br/>Status: {booking.Status}"
                    },
                    start = new { dateTime = booking.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"), timeZone = "UTC" },
                    end = new { dateTime = booking.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"), timeZone = "UTC" }
                };

                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(eventPayload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                // Ensure Metadata is initialized if it's null
                if (booking.Metadata == null)
                {
                    booking.Metadata = new Dictionary<string, object>();
                }
                booking.Metadata.TryGetValue("outlook_event_id", out var existingEventIdObj);
                var existingEventId = existingEventIdObj?.ToString();

                if (!string.IsNullOrEmpty(existingEventId))
                {
                    // UPDATE existing event (PATCH)
                    var patchResponse = await client.PatchAsync($"https://graph.microsoft.com/v1.0/me/events/{existingEventId}", content);
                    if (patchResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Updated Outlook event {EventId} for booking {BookingId}", existingEventId, booking.Id);
                    }
                    else if (patchResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Event deleted in Outlook, recreate
                        var retryPostResponse = await client.PostAsync("https://graph.microsoft.com/v1.0/me/events", content);
                        if (retryPostResponse.IsSuccessStatusCode)
                        {
                            var responseJson = await retryPostResponse.Content.ReadAsStringAsync();
                            using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
                            var newId = doc.RootElement.GetProperty("id").GetString();
                            booking.Metadata["outlook_event_id"] = newId ?? "";
                            _logger.LogInformation("Re-created Outlook event for booking {BookingId}", booking.Id);
                        }
                    }
                }
                else
                {
                    // CREATE new event (POST)
                    var postResponse = await client.PostAsync("https://graph.microsoft.com/v1.0/me/events", content);
                    if (postResponse.IsSuccessStatusCode)
                    {
                        var responseJson = await postResponse.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
                        var newId = doc.RootElement.GetProperty("id").GetString();
                        booking.Metadata["outlook_event_id"] = newId ?? "";
                        _logger.LogInformation("Created Outlook event {EventId} for booking {BookingId}", newId, booking.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create Outlook event for booking {BookingId}: {Error}", booking.Id, await postResponse.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing booking {BookingId} to Outlook Calendar", booking.Id);
            }
        }

        token.LastSyncAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<string> GetValidAccessTokenAsync(CalendarSyncToken token)
    {
        if (token.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return token.AccessToken;
        }

        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            throw new InvalidOperationException("No refresh token available. Re-authentication required.");
        }

        var clientId = _configuration["Authentication:Microsoft:ClientId"];
        var clientSecret = _configuration["Authentication:Microsoft:ClientSecret"];

        using var client = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", clientId ?? "" },
            { "client_secret", clientSecret ?? "" },
            { "refresh_token", token.RefreshToken },
            { "grant_type", "refresh_token" },
            { "scope", "Calendars.ReadWrite Offline_Access" }
        });

        var response = await client.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to refresh Outlook token: {Error}", error);
            throw new InvalidOperationException("Failed to refresh Outlook Calendar connection.");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = System.Text.Json.JsonSerializer.Deserialize<OutlookTokenResponse>(json);

        if (tokenData != null)
        {
            token.AccessToken = tokenData.AccessToken;
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
            await _context.SaveChangesAsync();
        }

        return token.AccessToken;
    }

    private class OutlookTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
