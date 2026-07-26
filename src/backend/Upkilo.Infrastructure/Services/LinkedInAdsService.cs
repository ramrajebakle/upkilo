using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// LinkedIn Marketing API integration service.
/// Uses OAuth2 access tokens stored per-tenant in TenantIntegrations.
/// </summary>
public class LinkedInAdsService : IAdPlatformService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ILogger<LinkedInAdsService> _logger;
    private const string LinkedInApiBase = "https://api.linkedin.com/rest";

    public string PlatformName => "LinkedIn";

    public LinkedInAdsService(
        IHttpClientFactory httpClientFactory,
        AppDbContext context,
        ILogger<LinkedInAdsService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LinkedIn");
        _httpClient.DefaultRequestHeaders.Add("LinkedIn-Version", "202401");
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ConnectAccountAsync(Guid tenantId, string authCode)
    {
        try
        {
            // Store the access token in TenantIntegrations
            var existing = await _context.TenantIntegrations
                .FirstOrDefaultAsync(ti => ti.TenantId == tenantId && ti.Provider == "LinkedIn" && !ti.IsDeleted);

            if (existing != null)
            {
                existing.AccessToken = authCode; // In production: exchange authCode for token via OAuth flow
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.TenantIntegrations.Add(new Core.Entities.TenantIntegration
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Provider = "LinkedIn",
                    IntegrationType = "Ads",
                    AccessToken = authCode,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("LinkedIn Ads account connected for tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect LinkedIn Ads for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<IEnumerable<AdCampaignDto>> GetCampaignsAsync(Guid tenantId)
    {
        var token = await GetAccessTokenAsync(tenantId);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("LinkedIn not connected for tenant {TenantId}", tenantId);
            return Array.Empty<AdCampaignDto>();
        }

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetAsync($"{LinkedInApiBase}/adCampaigns?q=search");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LinkedIn API returned {StatusCode} for campaigns", response.StatusCode);
                return Array.Empty<AdCampaignDto>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var campaigns = new List<AdCampaignDto>();

            if (doc.RootElement.TryGetProperty("elements", out var elements))
            {
                foreach (var el in elements.EnumerateArray())
                {
                    campaigns.Add(new AdCampaignDto
                    {
                        ExternalId = el.TryGetProperty("id", out var id) ? id.ToString() : "",
                        Name = el.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                        Status = el.TryGetProperty("status", out var status) ? status.GetString() ?? "" : "",
                        Budget = el.TryGetProperty("dailyBudget", out var budget) && budget.TryGetProperty("amount", out var amt)
                            ? amt.GetDecimal() / 100m : 0
                    });
                }
            }

            return campaigns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch LinkedIn campaigns for tenant {TenantId}", tenantId);
            return Array.Empty<AdCampaignDto>();
        }
    }

    public async Task<bool> UpdateCampaignStatusAsync(Guid tenantId, string externalId, string status)
    {
        var token = await GetAccessTokenAsync(tenantId);
        if (string.IsNullOrEmpty(token)) return false;

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var content = new StringContent(
                JsonSerializer.Serialize(new { status }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{LinkedInApiBase}/adCampaigns/{externalId}", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update LinkedIn campaign {CampaignId}", externalId);
            return false;
        }
    }

    public async Task<AdMetricsDto> GetMetricsAsync(Guid tenantId, string externalId, DateTime from, DateTime to)
    {
        var token = await GetAccessTokenAsync(tenantId);
        if (string.IsNullOrEmpty(token))
            return new AdMetricsDto();

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var url = $"{LinkedInApiBase}/adAnalytics?" +
                      $"q=analytics&campaigns=urn:li:sponsoredCampaign:{externalId}" +
                      $"&dateRange.start.year={from.Year}&dateRange.start.month={from.Month}&dateRange.start.day={from.Day}" +
                      $"&dateRange.end.year={to.Year}&dateRange.end.month={to.Month}&dateRange.end.day={to.Day}" +
                      "&fields=impressions,clicks,costInLocalCurrency,externalWebsiteConversions";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new AdMetricsDto();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("elements", out var elements) && elements.GetArrayLength() > 0)
            {
                var el = elements[0];
                return new AdMetricsDto
                {
                    Impressions = el.TryGetProperty("impressions", out var imp) ? imp.GetInt32() : 0,
                    Clicks = el.TryGetProperty("clicks", out var clicks) ? clicks.GetInt32() : 0,
                    Spend = el.TryGetProperty("costInLocalCurrency", out var cost) ? cost.GetDecimal() / 100m : 0,
                    Conversions = el.TryGetProperty("externalWebsiteConversions", out var conv) ? conv.GetDecimal() : 0
                };
            }

            return new AdMetricsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch LinkedIn metrics for campaign {CampaignId}", externalId);
            return new AdMetricsDto();
        }
    }

    private async Task<string?> GetAccessTokenAsync(Guid tenantId)
    {
        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.TenantId == tenantId && ti.Provider == "LinkedIn" && ti.IsActive && !ti.IsDeleted);

        return integration?.AccessToken;
    }
}
