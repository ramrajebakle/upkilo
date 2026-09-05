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
///   Free / Starter  → gpt-5-mini    (GlobalStandard, the larger quota)
///   Growth          → gpt-5.4-mini  (DataZoneStandard, newer generation)
///   Enterprise      → gpt-5.4-mini  (configurable per contract)
///   Business/Agency → gpt-5.4-mini  (legacy tiers, folded into Growth)
///
/// Both paid and free tiers currently run "mini" models because the subscription has
/// ZERO quota for every full-size model — gpt-5.5, gpt-5.4 and gpt-5 are all limit 0.
/// Once a quota increase is granted, StandardModel should move to a full-size model and
/// a deployment of that name must be created in the Azure OpenAI resource.
///
/// IMPORTANT: these strings are used directly as Azure OpenAI **deployment names** —
/// AzureOpenAIService builds `{endpoint}/openai/deployments/{model}/chat/completions`,
/// falling back to the raw model name when no AzureOpenAI:Deployments:{model} mapping
/// exists. So a deployment with each of these exact names must exist in the Azure OpenAI
/// resource, or every call 404s. They previously named Claude models, which no deployment
/// could ever match and for which there is no Anthropic client in this solution.
///
/// Any new value added here must also be allowed in
/// AzureOpenAIService.IsModelAllowedAsync, which rejects unlisted models before dispatch.
///
///
/// Enterprise contracts can override the model via Tenant.Settings["ai_model_override"].
/// </summary>
public class AiModelResolver : IAiModelResolver
{
    private const string EconomyModel = "gpt-5-mini";
    private const string StandardModel = "gpt-5.4-mini";
    private const string DefaultModel = EconomyModel;

    private readonly AppDbContext _db;
    private readonly ILogger<AiModelResolver> _logger;

    public AiModelResolver(AppDbContext db, ILogger<AiModelResolver> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> ResolveAsync(Guid tenantId)
    {
        // Upkilo's own marketing-site assistant has no Tenants row and never will. Without this
        // it would fall through to the not-found branch below and log a warning on every single
        // request - turning a normal condition into recurring noise that buries the real
        // misconfiguration that branch exists to report. The economy model is the right choice
        // for it regardless: short factual answers about the product, on Upkilo's own budget.
        if (tenantId == UpkiloPlatform.TenantId) return EconomyModel;

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
            nameof(SubscriptionTier.Free) => EconomyModel,
            nameof(SubscriptionTier.Starter) => EconomyModel,
            nameof(SubscriptionTier.Growth) => StandardModel,
            nameof(SubscriptionTier.Business) => StandardModel,   // legacy
            nameof(SubscriptionTier.Agency) => StandardModel,     // legacy
            nameof(SubscriptionTier.Enterprise) => StandardModel,
            _ => DefaultModel
        };
    }
}
