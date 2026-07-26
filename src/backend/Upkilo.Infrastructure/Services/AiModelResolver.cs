using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Routes AI requests to the correct model based on subscription tier.
///
/// Cost tiers:
///   Free / Starter  → claude-haiku-4-5-20251001  (~10x cheaper than Sonnet)
///   Professional    → claude-sonnet-4-6           (balanced cost/quality)
///   Business        → claude-sonnet-4-6           (same as Pro, higher quota)
///   Enterprise      → claude-sonnet-4-6           (configurable per contract)
///
/// Enterprise contracts can override the model via Tenant.Settings["ai_model_override"].
/// </summary>
public class AiModelResolver : IAiModelResolver
{
    private const string HaikuModel   = "claude-haiku-4-5-20251001";
    private const string SonnetModel  = "claude-sonnet-4-6";
    private const string DefaultModel = HaikuModel;

    private readonly AppDbContext _db;
    private readonly ILogger<AiModelResolver> _logger;

    public AiModelResolver(AppDbContext db, ILogger<AiModelResolver> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> ResolveAsync(Guid tenantId)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Select(t => new { t.Id, t.SubscriptionTier, t.Settings })
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null)
        {
            _logger.LogWarning("AiModelResolver: tenant {TenantId} not found, using default model", tenantId);
            return DefaultModel;
        }

        // Enterprise tenants may have a per-contract model override
        if (tenant.SubscriptionTier == SubscriptionTier.Enterprise
            && tenant.Settings.TryGetValue("ai_model_override", out var overrideObj)
            && overrideObj is string overrideModel
            && !string.IsNullOrWhiteSpace(overrideModel))
        {
            _logger.LogDebug("AiModelResolver: tenant {TenantId} using override model {Model}", tenantId, overrideModel);
            return overrideModel;
        }

        return ResolveForTier(tenant.SubscriptionTier.ToString());
    }

    public string ResolveForTier(string tier)
    {
        return tier switch
        {
            nameof(SubscriptionTier.Free)         => HaikuModel,
            nameof(SubscriptionTier.Starter)      => HaikuModel,
            nameof(SubscriptionTier.Professional) => SonnetModel,
            nameof(SubscriptionTier.Business)     => SonnetModel,
            nameof(SubscriptionTier.Enterprise)   => SonnetModel,
            _ => DefaultModel
        };
    }
}
