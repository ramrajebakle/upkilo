using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Entities;

namespace Upkilo.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _featureName;

    // Static mapping from attribute usage name → PricingFeature.Key in the DB.
    // Explicit dictionary eliminates the fragile PascalCase→snake_case char-by-char loop
    // that breaks for acronyms like "APIAccess" → "a_p_i_access" instead of "api_access".
    private static readonly Dictionary<string, string> _featureKeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "AiFeatures",          "ai_features" },
        { "AiCopilot",           "ai_copilot" },
        { "AiWorkflowBuilder",   "ai_workflow_builder" },
        { "AiReceptionist",      "ai_receptionist" },
        { "VoiceAI",             "voice_ai" },
        { "AdvancedReporting",   "advanced_reporting" },
        { "ApiAccess",           "api_access" },
        { "Webhooks",            "webhooks" },
        { "WhiteLabel",          "white_label" },
        { "WhiteLabelDomain",    "white_label_domain" },
        { "MultiLocation",       "multi_location" },
        { "TeamManagement",      "team_management" },
        { "MarketplaceListing",  "marketplace_listing" },
        { "SmsReminders",        "sms_reminders" },
        { "CalendarSync",        "calendar_sync" },
        { "CustomBranding",      "custom_branding" },
        { "PrioritySupport",     "priority_support" },
        { "SlaGuarantee",        "sla_guarantee" },
        { "OnlineBooking",       "online_booking" },
        { "EmailReminders",      "email_reminders" },
        { "Inventory",           "inventory" },
        { "GiftCards",           "gift_cards" },
        { "Memberships",         "memberships" },
        { "Packages",            "packages" },
        { "ClassScheduling",     "class_scheduling" },
        { "Waitlist",            "waitlist" },
        { "Forms",               "forms" },
        { "Waivers",             "waivers" },
        { "Loyalty",             "loyalty" },
        { "MarketingAutomation", "marketing_automation" },
        { "Referrals",           "referrals" },
        { "Analytics",           "analytics" },
    };

    public RequireFeatureAttribute(string featureName)
    {
        _featureName = featureName;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var tenantProvider = context.HttpContext.RequestServices.GetRequiredService<ITenantProvider>();
        var tenantId = tenantProvider.GetTenantId();

        if (tenantId == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        var subscription = await dbContext.Set<Subscription>()
            .Include(s => s.PricingPlan).ThenInclude(p => p!.FeatureMappings).ThenInclude(m => m.PricingFeature)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (subscription?.PricingPlan == null)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Resolve key: prefer static map, fall back to the passed-in value as-is.
        var featureKey = _featureKeyMap.TryGetValue(_featureName, out var mapped) ? mapped : _featureName;

        var mapping = subscription.PricingPlan.FeatureMappings
            .FirstOrDefault(m => string.Equals(m.PricingFeature?.Key, featureKey, StringComparison.OrdinalIgnoreCase));

        if (mapping?.IsEnabled != true)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
