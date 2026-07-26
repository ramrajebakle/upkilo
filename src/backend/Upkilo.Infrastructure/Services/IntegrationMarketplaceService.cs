using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class IntegrationMarketplaceService
{
    private readonly ILogger<IntegrationMarketplaceService> _logger;
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public IntegrationMarketplaceService(
        ILogger<IntegrationMarketplaceService> logger,
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<bool> ConnectQuickBooksAsync(Guid tenantId, string authCode)
    {
        _logger.LogInformation("Connecting QuickBooks for tenant {TenantId}", tenantId);

        var clientId = _configuration["Integrations:QuickBooks:ClientId"];
        var clientSecret = _configuration["Integrations:QuickBooks:ClientSecret"];
        var redirectUri = _configuration["Integrations:QuickBooks:RedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogWarning("QuickBooks credentials not configured");
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            // Exchange authorization code for access token
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authCode),
                new KeyValuePair<string, string>("redirect_uri", redirectUri ?? "")
            });

            var tokenResponse = await client.PostAsync("https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer", tokenRequest);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("QuickBooks token exchange failed: {Status}", tokenResponse.StatusCode);
                return false;
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(tokenJson);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var refreshToken = doc.RootElement.GetProperty("refresh_token").GetString();
            var realmId = doc.RootElement.TryGetProperty("x_refresh_token_expires_in", out _) ? null : (string?)null;

            // Persist integration connection
            var existing = await _context.TenantIntegrations
                .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.IntegrationId == "quickbooks");

            if (existing != null)
            {
                existing.AccessToken = accessToken;
                existing.RefreshToken = refreshToken;
                existing.IsConnected = true;
                existing.IsActive = true;
                existing.ConnectedAt = DateTime.UtcNow;
            }
            else
            {
                _context.TenantIntegrations.Add(new TenantIntegration
                {
                    TenantId = tenantId,
                    IntegrationId = "quickbooks",
                    Provider = "Intuit",
                    IntegrationType = "Accounting",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsConnected = true,
                    IsActive = true,
                    ConnectedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("QuickBooks connected for tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickBooks connection failed for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<bool> SyncMailchimpContactsAsync(Guid tenantId)
    {
        _logger.LogInformation("Syncing Mailchimp contacts for tenant {TenantId}", tenantId);

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.IntegrationId == "mailchimp");

        if (integration == null || !integration.IsConnected || string.IsNullOrEmpty(integration.ApiKey))
        {
            _logger.LogWarning("Mailchimp not connected for tenant {TenantId}", tenantId);
            return false;
        }

        var settings = integration.Settings != null
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(integration.Settings)
            : new Dictionary<string, string>();

        if (settings == null || !settings.TryGetValue("listId", out var listId) || !settings.TryGetValue("datacenter", out var dc))
        {
            _logger.LogWarning("Mailchimp listId or datacenter not configured for tenant {TenantId}", tenantId);
            return false;
        }

        try
        {
            // Fetch active clients from DB
            var clients = await _context.Clients
                .Where(c => c.TenantId == tenantId && !c.IsDeleted)
                .Select(c => new { c.Email, c.FirstName, c.LastName })
                .ToListAsync();

            if (!clients.Any())
            {
                _logger.LogInformation("No clients to sync for tenant {TenantId}", tenantId);
                return true;
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"anystring:{integration.ApiKey}")));

            // Mailchimp batch subscribe endpoint
            var members = clients
                .Where(c => !string.IsNullOrEmpty(c.Email))
                .Select(c => new
                {
                    email_address = c.Email,
                    status = "subscribed",
                    merge_fields = new { FNAME = c.FirstName ?? "", LNAME = c.LastName ?? "" }
                })
                .ToArray();

            const int batchSize = 500;
            for (int i = 0; i < members.Length; i += batchSize)
            {
                var batch = members.Skip(i).Take(batchSize).ToArray();
                var body = JsonSerializer.Serialize(new { members = batch, update_existing = true });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var url = $"https://{dc}.api.mailchimp.com/3.0/lists/{listId}";
                var response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Mailchimp batch sync failed: {Error}", err);
                    return false;
                }
            }

            integration.LastSyncAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Synced {Count} contacts to Mailchimp for tenant {TenantId}", members.Length, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mailchimp sync failed for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public List<IntegrationListing> GetMarketplaceListings()
    {
        return new List<IntegrationListing>
        {
            new("QuickBooks", "Cloud accounting & financial management.", "Finance"),
            new("Mailchimp", "Email marketing & automation.", "Marketing"),
            new("Zapier", "Connect to 5000+ apps.", "Automation"),
            new("Google Analytics 4", "Deep traffic and conversion insights.", "Analytics")
        };
    }
}

public record IntegrationListing(string Name, string Description, string Category);
