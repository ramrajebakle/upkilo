using System;
using System.Collections.Generic;
using System.Linq;
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

/// <summary>
/// Integrates with Klaviyo REST API v2024-02-15 for contact sync and event tracking.
/// </summary>
public class KlaviyoService
{
    private const string BaseUrl = "https://a.klaviyo.com/api";
    private const string ApiRevision = "2024-02-15";
    private const int BatchSize = 100;

    private readonly ILogger<KlaviyoService> _logger;
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    public KlaviyoService(
        ILogger<KlaviyoService> logger,
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["Integrations:Klaviyo:ApiKey"] ?? string.Empty;
    }

    /// <summary>Sync all tenant clients to Klaviyo profiles in batches of 100.</summary>
    public async Task SyncContactsAsync(Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Klaviyo API key not configured, skipping sync for tenant {TenantId}", tenantId);
            return;
        }

        _logger.LogInformation("Syncing Klaviyo contacts for tenant {TenantId}", tenantId);

        var clients = await _context.Clients
            .Where(c => c.TenantId == tenantId && !c.IsDeleted && c.Email != null)
            .ToListAsync();

        var batches = clients.Chunk(BatchSize);
        int synced = 0;

        foreach (var batch in batches)
        {
            var profiles = batch.Select(c => new
            {
                type = "profile",
                attributes = new
                {
                    email = c.Email,
                    first_name = c.FirstName,
                    last_name = c.LastName,
                    phone_number = c.Phone
                }
            }).ToList();

            var payload = JsonSerializer.Serialize(new
            {
                data = new
                {
                    type = "profile-bulk-import-job",
                    attributes = new
                    {
                        profiles = new { data = profiles }
                    }
                }
            });

            await PostAsync("/profile-bulk-import-jobs/", payload);
            synced += batch.Length;
        }

        // Persist last sync time
        await UpdateIntegrationSyncTimeAsync(tenantId);
        _logger.LogInformation("Klaviyo sync complete: {Count} contacts for tenant {TenantId}", synced, tenantId);
    }

    /// <summary>Track a Klaviyo event (metric) for a given email.</summary>
    public async Task TrackEventAsync(Guid tenantId, string email, string eventName, object properties)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return;

        var payload = JsonSerializer.Serialize(new
        {
            data = new
            {
                type = "event",
                attributes = new
                {
                    metric = new { data = new { type = "metric", attributes = new { name = eventName } } },
                    profile = new { data = new { type = "profile", attributes = new { email } } },
                    properties,
                    time = DateTime.UtcNow.ToString("O")
                }
            }
        });

        await PostAsync("/events/", payload);
        _logger.LogDebug("Klaviyo event tracked: {Event} for {Email}", eventName, email);
    }

    private async Task PostAsync(string path, string jsonPayload)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Klaviyo-API-Key", _apiKey);
        client.DefaultRequestHeaders.Add("revision", ApiRevision);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{BaseUrl}{path}", content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Klaviyo API error {Status} for {Path}: {Body}", response.StatusCode, path, body);
        }
    }

    private async Task UpdateIntegrationSyncTimeAsync(Guid tenantId)
    {
        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.IntegrationId == "klaviyo");

        if (integration == null)
        {
            integration = new TenantIntegration
            {
                TenantId = tenantId,
                IntegrationId = "klaviyo",
                Provider = "Klaviyo",
                IntegrationType = "Marketing",
                IsActive = true,
                IsConnected = true
            };
            _context.TenantIntegrations.Add(integration);
        }

        integration.LastSyncAt = DateTime.UtcNow;
        integration.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
