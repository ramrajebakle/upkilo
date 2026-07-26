using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Feature flag service for controlled rollouts and canary deployments.
/// Supports tenant-level, percentage-based, and global flags.
/// Optionally syncs from LaunchDarkly REST API.
/// </summary>
public class FeatureFlagService
{
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private static readonly ConcurrentDictionary<string, FeatureFlag> _flags = new();

    public FeatureFlagService(
        ILogger<FeatureFlagService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        SeedDefaults();
    }

    private void SeedDefaults()
    {
        RegisterFlag("ai_chatbot", true, "AI Chatbot feature");
        RegisterFlag("ad_platform_linkedin", true, "LinkedIn Ads integration");
        RegisterFlag("landing_page_builder", true, "Landing page builder");
        RegisterFlag("referral_program", true, "Referral program");
        RegisterFlag("partner_program", true, "Partner/Agency program");
        RegisterFlag("gdpr_export", true, "GDPR data export");
        RegisterFlag("outbox_processing", true, "Outbox pattern for events");
        RegisterFlag("circuit_breaker", true, "Circuit breaker for external APIs");
    }

    public void RegisterFlag(string name, bool defaultValue, string? description = null)
    {
        _flags.TryAdd(name, new FeatureFlag
        {
            Name = name,
            IsEnabled = defaultValue,
            Description = description ?? name
        });
    }

    public bool IsEnabled(string flagName, Guid? tenantId = null)
    {
        if (!_flags.TryGetValue(flagName, out var flag))
            return false;

        // Tenant-level override
        if (tenantId.HasValue && flag.TenantOverrides.TryGetValue(tenantId.Value, out var tenantValue))
            return tenantValue;

        // Percentage rollout
        if (flag.RolloutPercentage < 100 && tenantId.HasValue)
        {
            var hash = Math.Abs(tenantId.Value.GetHashCode()) % 100;
            return hash < flag.RolloutPercentage;
        }

        return flag.IsEnabled;
    }

    public void SetTenantOverride(string flagName, Guid tenantId, bool enabled)
    {
        if (_flags.TryGetValue(flagName, out var flag))
        {
            flag.TenantOverrides[tenantId] = enabled;
            _logger.LogInformation("Feature {Flag} overridden for tenant {Tenant}: {Enabled}", flagName, tenantId, enabled);
        }
    }

    public void SetRolloutPercentage(string flagName, int percentage)
    {
        if (_flags.TryGetValue(flagName, out var flag))
        {
            flag.RolloutPercentage = Math.Clamp(percentage, 0, 100);
            _logger.LogInformation("Feature {Flag} rollout set to {Percent}%", flagName, percentage);
        }
    }

    /// <summary>
    /// Syncs feature flags from LaunchDarkly REST API into the local in-memory dictionary.
    /// Silently skips if SDK key is not configured (dev mode).
    /// </summary>
    public async Task SyncFromLaunchDarklyAsync()
    {
        var sdkKey = _configuration["LaunchDarkly:SdkKey"];
        if (string.IsNullOrWhiteSpace(sdkKey))
        {
            _logger.LogDebug("LaunchDarkly SDK key not configured, skipping sync");
            return;
        }

        var projectKey = _configuration["LaunchDarkly:ProjectKey"] ?? "default";
        var url = $"https://app.launchdarkly.com/api/v2/flags/{projectKey}";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("", sdkKey);
            // LaunchDarkly uses plain Authorization: <sdkKey> (no "Bearer")
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", sdkKey);

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LaunchDarkly flags sync failed: {Status}", response.StatusCode);
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("items", out var items))
                return;

            var envKey = _configuration["LaunchDarkly:EnvironmentKey"] ?? "production";
            int synced = 0;

            foreach (var item in items.EnumerateArray())
            {
                var flagKey = item.TryGetProperty("key", out var keyEl) ? keyEl.GetString() : null;
                if (string.IsNullOrEmpty(flagKey)) continue;

                bool isOn = false;
                if (item.TryGetProperty("environments", out var envs) &&
                    envs.TryGetProperty(envKey, out var env) &&
                    env.TryGetProperty("on", out var onEl))
                {
                    isOn = onEl.GetBoolean();
                }

                var description = item.TryGetProperty("description", out var descEl) ? descEl.GetString() : flagKey;

                if (_flags.TryGetValue(flagKey, out var existing))
                {
                    existing.IsEnabled = isOn;
                }
                else
                {
                    _flags[flagKey] = new FeatureFlag
                    {
                        Name = flagKey,
                        Description = description ?? flagKey,
                        IsEnabled = isOn
                    };
                }
                synced++;
            }

            _logger.LogInformation("LaunchDarkly sync complete: {Count} flags synced", synced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync flags from LaunchDarkly");
        }
    }

    public IEnumerable<object> GetAllFlags() => _flags.Values.Select(f => new
    {
        f.Name,
        f.Description,
        f.IsEnabled,
        f.RolloutPercentage
    }).ToList();

    private class FeatureFlag
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public int RolloutPercentage { get; set; } = 100;
        public ConcurrentDictionary<Guid, bool> TenantOverrides { get; } = new();
    }
}
