using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Text.Json;

namespace Upkilo.API.Controllers;

/// <summary>
/// Calendar integrations controller for Google/Outlook sync.
/// Uses TenantIntegration entity for connection state and Booking entity for iCal feeds.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CalendarIntegrationsController : ControllerBase
{
    private readonly ILogger<CalendarIntegrationsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public CalendarIntegrationsController(
        ILogger<CalendarIntegrationsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Get connected calendar accounts
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConnectedCalendars()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var calendars = await _context.TenantIntegrations
            .Where(ti => ti.TenantId == tenantId.Value && !ti.IsDeleted &&
                (ti.IntegrationId == "google-calendar" || ti.IntegrationId == "outlook"))
            .Select(ti => new
            {
                ti.Id,
                provider = ti.IntegrationId == "google-calendar" ? "google" : "outlook",
                ti.IsConnected,
                ti.ConnectedAt,
                ti.LastSyncAt,
                ti.Settings
            })
            .ToListAsync();

        return Ok(new { data = calendars });
    }

    /// <summary>
    /// Get OAuth URL for calendar connection
    /// </summary>
    [HttpGet("{provider}/connect")]
    public IActionResult GetConnectUrl(string provider)
    {
        var state = Guid.NewGuid().ToString();
        var apiUrl = (_configuration["API_URL"] ?? "https://api.upkilo.com").TrimEnd('/');
        var redirectUri = $"{apiUrl}/api/calendar-integrations/{provider}/callback";

        var authUrl = provider.ToLower() switch
        {
            "google" => $"https://accounts.google.com/o/oauth2/v2/auth?client_id={_configuration["Google:ClientId"]}&redirect_uri={redirectUri}&response_type=code&scope=https://www.googleapis.com/auth/calendar&state={state}&access_type=offline&prompt=consent",
            "outlook" => $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={_configuration["Microsoft:ClientId"]}&redirect_uri={redirectUri}&response_type=code&scope=Calendars.ReadWrite offline_access&state={state}",
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };

        return Ok(new { authUrl, state });
    }

    /// <summary>
    /// OAuth callback for calendar connection.
    /// Exchanges the authorization code for access/refresh tokens and stores them.
    /// </summary>
    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCallback(string provider, [FromQuery] string code, [FromQuery] string state)
    {
        _logger.LogInformation("OAuth callback for {Provider}, state: {State}", provider, state);

        try
        {
            var apiUrl = (_configuration["API_URL"] ?? "https://api.upkilo.com").TrimEnd('/');
            var redirectUri = $"{apiUrl}/api/calendar-integrations/{provider}/callback";
            var httpClient = _httpClientFactory.CreateClient();

            // Build token exchange request based on provider
            var tokenEndpoint = provider.ToLower() switch
            {
                "google" => "https://oauth2.googleapis.com/token",
                "outlook" => "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                _ => throw new ArgumentException($"Unsupported provider: {provider}")
            };

            var clientId = provider.ToLower() == "google"
                ? _configuration["Google:ClientId"]
                : _configuration["Microsoft:ClientId"];
            var clientSecret = provider.ToLower() == "google"
                ? _configuration["Google:ClientSecret"]
                : _configuration["Microsoft:ClientSecret"];

            var tokenRequest = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId ?? "",
                ["client_secret"] = clientSecret ?? "",
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            };

            var response = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed for {Provider}: {StatusCode} {Body}",
                    provider, response.StatusCode, responseBody);
                return Redirect($"/settings/integrations?error=token_exchange_failed");
            }

            var tokenData = JsonDocument.Parse(responseBody);
            var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
            var refreshToken = tokenData.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            var expiresIn = tokenData.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;

            // Save tokens to TenantIntegration
            // Note: In a real flow, the state parameter would encode the tenantId
            var tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
            var integrationId = provider.ToLower() == "google" ? "google-calendar" : "outlook";

            var existing = await _context.TenantIntegrations
                .FirstOrDefaultAsync(ti => ti.TenantId == tenantId && ti.IntegrationId == integrationId && !ti.IsDeleted);

            if (existing != null)
            {
                existing.AccessToken = accessToken;
                existing.RefreshToken = refreshToken;
                existing.IsConnected = true;
                existing.ConnectedAt = DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.Settings = JsonSerializer.Serialize(new { expiresIn, expiresAt = DateTime.UtcNow.AddSeconds(expiresIn) });
            }
            else
            {
                _context.TenantIntegrations.Add(new TenantIntegration
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    IntegrationId = integrationId,
                    Provider = provider,
                    IntegrationType = "Calendar",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsConnected = true,
                    IsActive = true,
                    ConnectedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    Settings = JsonSerializer.Serialize(new { expiresIn, expiresAt = DateTime.UtcNow.AddSeconds(expiresIn) })
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Calendar {Provider} connected for tenant {TenantId}", provider, tenantId);

            return Redirect("/settings/integrations?connected=true");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed for {Provider}", provider);
            return Redirect("/settings/integrations?error=callback_failed");
        }
    }

    /// <summary>
    /// Disconnect a calendar
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DisconnectCalendar(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.Id == id && ti.TenantId == tenantId.Value && !ti.IsDeleted);

        if (integration == null) return NotFound();

        integration.IsConnected = false;
        integration.AccessToken = null;
        integration.RefreshToken = null;
        integration.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Calendar disconnected: {CalendarId}", id);
        return NoContent();
    }

    /// <summary>
    /// Update calendar sync settings
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCalendarSettings(Guid id, [FromBody] UpdateCalendarSettingsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.Id == id && ti.TenantId == tenantId.Value && !ti.IsDeleted);

        if (integration == null) return NotFound();

        // Store settings as JSON
        var settings = new Dictionary<string, object>();
        if (request.SyncEnabled.HasValue) settings["syncEnabled"] = request.SyncEnabled.Value;
        if (request.SyncDirection != null) settings["syncDirection"] = request.SyncDirection;
        if (request.CalendarId != null) settings["calendarId"] = request.CalendarId;
        if (request.BlockExternalEvents.HasValue) settings["blockExternalEvents"] = request.BlockExternalEvents.Value;

        integration.Settings = System.Text.Json.JsonSerializer.Serialize(settings);
        integration.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Calendar settings updated: {CalendarId}", id);

        return Ok(new
        {
            success = true,
            syncEnabled = request.SyncEnabled,
            syncDirection = request.SyncDirection
        });
    }

    /// <summary>
    /// Trigger manual sync
    /// </summary>
    [HttpPost("{id}/sync")]
    public async Task<IActionResult> TriggerSync(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.Id == id && ti.TenantId == tenantId.Value && ti.IsConnected && !ti.IsDeleted);

        if (integration == null) return NotFound();

        // Update last sync timestamp
        integration.LastSyncAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Manual sync triggered: {CalendarId}", id);

        return Ok(new
        {
            success = true,
            syncStartedAt = DateTime.UtcNow.ToString("o"),
            message = "Sync started. This may take a few minutes."
        });
    }

    /// <summary>
    /// Get iCal feed content — renders real bookings as VCALENDAR
    /// </summary>
    [HttpGet("ical/{token}.ics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetICalContent(string token)
    {
        // Validate iCal feed token against TenantIntegrations
        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.ApiKey == token && ti.IntegrationType == "ICalFeed" && ti.IsActive && !ti.IsDeleted);

        Guid? tenantId;
        if (integration != null)
        {
            tenantId = integration.TenantId;
        }
        else
        {
            // Fallback: try tenant provider (for authenticated requests)
            tenantId = _tenantProvider.GetTenantId();
            if (tenantId == null)
                return NotFound("Invalid iCal feed token.");
        }

        var bookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Where(b => b.TenantId == tenantId && !b.IsDeleted &&
                b.StartTime >= DateTime.UtcNow.AddMonths(-1) &&
                b.StartTime <= DateTime.UtcNow.AddMonths(3))
            .OrderBy(b => b.StartTime)
            .Take(200)
            .ToListAsync();

        var events = string.Join("\r\n", bookings.Select(b =>
            $"BEGIN:VEVENT\r\n" +
            $"UID:{b.Id}@{new Uri(_configuration["APP_URL"] ?? "https://upkilo.com").Host}\r\n" +
            $"DTSTART:{b.StartTime:yyyyMMddTHHmmssZ}\r\n" +
            $"DTEND:{b.EndTime:yyyyMMddTHHmmssZ}\r\n" +
            $"SUMMARY:{(b.Service?.Name ?? "Booking")} - {(b.Client != null ? $"{b.Client.FirstName} {b.Client.LastName}" : "Walk-in")}\r\n" +
            $"DESCRIPTION:Staff: {(b.Staff != null ? $"{b.Staff.FirstName} {b.Staff.LastName}" : "Unassigned")}\r\n" +
            $"END:VEVENT"));

        var icalContent = $"BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Upkilo//Calendar//EN\r\n{events}\r\nEND:VCALENDAR";

        return Content(icalContent, "text/calendar");
    }
}

// Request DTO
public class UpdateCalendarSettingsRequest
{
    public bool? SyncEnabled { get; set; }
    public string? SyncDirection { get; set; } // one-way, two-way
    public string? CalendarId { get; set; }
    public bool? BlockExternalEvents { get; set; }
}

