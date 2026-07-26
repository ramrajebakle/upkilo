using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class BrandRegistrationRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty; // e.g. "PRIVATE_PROFIT"
    public string Ein { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
    public string PostalCode { get; set; } = string.Empty;
}

/// <summary>
/// Twilio A2P 10DLC brand and campaign registration via Twilio REST API.
/// </summary>
public class SmsA2pRegistrationService
{
    private const string TwilioBaseUrl = "https://messaging.twilio.com/v1";

    private readonly ILogger<SmsA2pRegistrationService> _logger;
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _accountSid;
    private readonly string _authToken;

    public SmsA2pRegistrationService(
        ILogger<SmsA2pRegistrationService> logger,
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _accountSid = configuration["Twilio:AccountSid"] ?? string.Empty;
        _authToken = configuration["Twilio:AuthToken"] ?? string.Empty;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_accountSid}:{_authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return client;
    }

    /// <summary>Register a TCR brand for the given tenant.</summary>
    public async Task<string?> RegisterBrandAsync(Guid tenantId, BrandRegistrationRequest request)
    {
        _logger.LogInformation("Registering TCR brand for tenant {TenantId}: {Business}", tenantId, request.BusinessName);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("FriendlyName", request.BusinessName),
            new KeyValuePair<string, string>("EntityType", request.BusinessType),
            new KeyValuePair<string, string>("CompanyName", request.BusinessName),
            new KeyValuePair<string, string>("Ein", request.Ein),
            new KeyValuePair<string, string>("Website", request.Website),
            new KeyValuePair<string, string>("Email", request.Email),
            new KeyValuePair<string, string>("PhoneNumber", request.Phone),
            new KeyValuePair<string, string>("City", request.City),
            new KeyValuePair<string, string>("StateProvinceRegion", request.State),
            new KeyValuePair<string, string>("Country", request.Country),
            new KeyValuePair<string, string>("PostalCode", request.PostalCode)
        });

        var client = CreateClient();
        var resp = await client.PostAsync($"{TwilioBaseUrl}/a2p/BrandRegistrations", form);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            _logger.LogError("Brand registration failed: {Status} {Body}", resp.StatusCode, err);
            return null;
        }

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var brandSid = doc.RootElement.TryGetProperty("sid", out var sid) ? sid.GetString() : null;

        // Persist
        await PersistIntegrationSettingsAsync(tenantId, brandSid: brandSid);
        _logger.LogInformation("Brand registered: SID={BrandSid} for tenant {TenantId}", brandSid, tenantId);
        return brandSid;
    }

    /// <summary>Register an A2P campaign for the given brand SID.</summary>
    public async Task<string?> RegisterCampaignAsync(Guid tenantId, string brandSid, string description)
    {
        _logger.LogInformation("Registering A2P campaign for tenant {TenantId}, brand {BrandSid}", tenantId, brandSid);

        // Retrieve the messaging service SID from integration settings
        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.IntegrationId == "twilio-a2p");

        var settings = integration?.Settings != null
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(integration.Settings) ?? new()
            : new Dictionary<string, string>();

        settings.TryGetValue("MessagingServiceSid", out var messagingServiceSid);
        if (string.IsNullOrEmpty(messagingServiceSid))
        {
            _logger.LogWarning("MessagingServiceSid not configured for tenant {TenantId}", tenantId);
            return null;
        }

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Description", description),
            new KeyValuePair<string, string>("MessageFlow", "End users opt-in by signing up on our website."),
            new KeyValuePair<string, string>("BrandRegistrationSid", brandSid),
            new KeyValuePair<string, string>("HasEmbeddedLinks", "false"),
            new KeyValuePair<string, string>("HasEmbeddedPhone", "false"),
            new KeyValuePair<string, string>("UsAppToPersonUsecase", "NOTIFICATIONS")
        });

        var client = CreateClient();
        var resp = await client.PostAsync($"{TwilioBaseUrl}/Services/{messagingServiceSid}/Compliance/Usa2p", form);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            _logger.LogError("Campaign registration failed: {Status} {Body}", resp.StatusCode, err);
            return null;
        }

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var campaignSid = doc.RootElement.TryGetProperty("sid", out var sid) ? sid.GetString() : null;

        await PersistIntegrationSettingsAsync(tenantId, campaignSid: campaignSid);
        _logger.LogInformation("Campaign registered: SID={CampaignSid} for tenant {TenantId}", campaignSid, tenantId);
        return campaignSid;
    }

    /// <summary>Retrieve brand registration status from Twilio.</summary>
    public async Task<string?> GetRegistrationStatusAsync(Guid tenantId)
    {
        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.IntegrationId == "twilio-a2p");

        if (integration?.Settings == null) return "Not registered";

        var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(integration.Settings) ?? new();
        if (!settings.TryGetValue("BrandSid", out var brandSid) || string.IsNullOrEmpty(brandSid))
            return "Not registered";

        var client = CreateClient();
        var resp = await client.GetAsync($"{TwilioBaseUrl}/a2p/BrandRegistrations/{brandSid}");

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("GetRegistrationStatus failed: {Status}", resp.StatusCode);
            return "Unknown";
        }

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("status", out var status) ? status.GetString() : "Unknown";
    }

    private async Task PersistIntegrationSettingsAsync(Guid tenantId, string? brandSid = null, string? campaignSid = null)
    {
        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.IntegrationId == "twilio-a2p");

        if (integration == null)
        {
            integration = new TenantIntegration
            {
                TenantId = tenantId,
                IntegrationId = "twilio-a2p",
                Provider = "Twilio",
                IntegrationType = "SMS",
                IsActive = true,
                IsConnected = true,
                ConnectedAt = DateTime.UtcNow
            };
            _context.TenantIntegrations.Add(integration);
        }

        var settings = integration.Settings != null
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(integration.Settings) ?? new()
            : new Dictionary<string, string>();

        if (brandSid != null) settings["BrandSid"] = brandSid;
        if (campaignSid != null) settings["CampaignSid"] = campaignSid;

        integration.Settings = JsonSerializer.Serialize(settings);
        integration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
