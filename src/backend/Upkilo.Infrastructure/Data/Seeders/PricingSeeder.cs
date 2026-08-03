using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Data.Seeders;

public static class PricingSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.PricingPlans.AnyAsync())
            return; // Already seeded

        // 1. Create Features
        var fMaxStaff = new PricingFeature { Key = "max_staff", Name = "Maximum Staff", Type = FeatureType.Numeric };
        var fMaxLocations = new PricingFeature { Key = "max_locations", Name = "Maximum Locations", Type = FeatureType.Numeric };
        var fMaxClients = new PricingFeature { Key = "max_clients", Name = "Maximum Clients", Type = FeatureType.Numeric };
        var fAiActions = new PricingFeature { Key = "ai_actions", Name = "AI Actions per Month", Type = FeatureType.Numeric };

        var fOnlineBooking = new PricingFeature { Key = "online_booking", Name = "Online Booking", Type = FeatureType.Boolean };
        var fSmsReminders = new PricingFeature { Key = "sms_reminders", Name = "SMS Reminders", Type = FeatureType.Boolean };
        var fAiCopilot = new PricingFeature { Key = "ai_copilot", Name = "AI Copilot Rail", Type = FeatureType.Boolean };
        var fAiWorkflows = new PricingFeature { Key = "ai_workflows", Name = "AI Workflow Builder", Type = FeatureType.Boolean };
        var fAiInsights = new PricingFeature { Key = "ai_insights", Name = "AI Insight Engine", Type = FeatureType.Boolean };
        var fWhiteLabel = new PricingFeature { Key = "white_label", Name = "Custom Branding / White-Label", Type = FeatureType.Boolean };
        var fApiAccess = new PricingFeature { Key = "api_access", Name = "API & Webhooks", Type = FeatureType.Boolean };
        var fAdvancedSecurity = new PricingFeature { Key = "advanced_security", Name = "SSO / SAML / Extended Logs", Type = FeatureType.Boolean };
        var fMarketingAutomation = new PricingFeature { Key = "marketing_automation", Name = "Marketing Automation", Type = FeatureType.Boolean };
        // Controls "Powered by Upkilo" footer branding on public booking pages (viral loop)
        var fShowBranding = new PricingFeature { Key = "show_powered_by_branding", Name = "Show Powered By Upkilo Branding", Type = FeatureType.Boolean };
        // Agency: sub-tenant provisioning and impersonation — NumericLimit = max sub-tenants
        var fAgencyManagement = new PricingFeature { Key = "agency_management", Name = "Agency Sub-Account Management", Type = FeatureType.Numeric };

        context.PricingFeatures.AddRange(
            fMaxStaff, fMaxLocations, fMaxClients, fAiActions,
            fOnlineBooking, fSmsReminders, fAiCopilot, fAiWorkflows,
            fAiInsights, fWhiteLabel, fApiAccess, fAdvancedSecurity,
            fMarketingAutomation, fShowBranding, fAgencyManagement
        );

        // 2. Create Plans
        // Three purchasable tiers, not six. Professional/Business/Agency were consolidated
        // into a single Growth plan: Business and Agency previously carried IDENTICAL limits
        // and feature flags, differing only by 20 sub-accounts for $100/mo more, which is not
        // a defensible tier boundary. Overflow is now sold as add-ons (extra seats, extra
        // locations, AI/SMS credits) rather than by adding tiers.
        var free = new PricingPlan { Name = "Free", Description = "Get started with online booking at no cost", IsActive = true, TrialDays = 14 };
        var starter = new PricingPlan { Name = "Starter", Description = "Everything a small team needs to run bookings and clients", IsActive = true, TrialDays = 14 };
        var growth = new PricingPlan { Name = "Growth", Description = "AI-powered growth, white-label and API for scaling businesses", IsActive = true, TrialDays = 14 };
        // Enterprise: custom quote — no fixed price, sales-led
        var enterprise = new PricingPlan { Name = "Enterprise", Description = "Security, Compliance, and Scale — custom pricing for your team", IsActive = true, TrialDays = 30, IsCustom = true };

        context.PricingPlans.AddRange(free, starter, growth, enterprise);

        // 3. Add Prices (Monthly & Annual).
        // Annual = monthly × 10, i.e. "2 months free" (16.67% off). Pricing it as an exact
        // multiple means the offer states itself on the pricing page and the customer can
        // verify the arithmetic at a glance — clearer than "save 15%" or "save 20%".
        // Free plan has no price rows — $0 = no Stripe subscription needed.
        // Enterprise: IsCustom=true, no fixed price rows — billing page shows "Contact us".
        // Upkilo bills exclusively in USD. Multi-currency subscription pricing was removed:
        // maintaining a price row per currency meant every price change had to be repeated
        // across all of them, and they had already drifted out of step with each other.
        //
        // This is Upkilo -> tenant billing only. It does NOT constrain what a tenant charges
        // their own customers — that settles through the tenant's connected Stripe account in
        // that account's own currency, and stays fully multi-currency.
        //
        // Re-adding a currency later is a data change here plus rows in Stripe; no code change
        // is needed, because GetPlans falls back to USD for any currency it has no rows for.
        // Benchmarked against the market (Aug 2026): Growth $499 sits on Mindbody Ultimate
        // ($479–499) and inside Zenoti's multi-location band ($400–1,000+), while working out
        // to ~$50/location vs Boulevard's $140. Starter $149 undercuts Mindbody Starter
        // ($139–169). $199 was rejected for Starter — it sat above Mindbody's entry tier while
        // the real entry market (Square, Fresha, Vagaro) is $20–49, which would have made
        // Free → paid conversion implausible for the small salons most likely to try us first.
        context.PlanPrices.AddRange(
            new PlanPrice { PricingPlan = starter, Amount = 149, CurrencyCode = "USD", Cycle = BillingCycle.Monthly },
            new PlanPrice { PricingPlan = starter, Amount = 1490, CurrencyCode = "USD", Cycle = BillingCycle.Annual }, // $149×10 → 2 months free ($124.17/mo)
            new PlanPrice { PricingPlan = growth, Amount = 499, CurrencyCode = "USD", Cycle = BillingCycle.Monthly },
            new PlanPrice { PricingPlan = growth, Amount = 4990, CurrencyCode = "USD", Cycle = BillingCycle.Annual }   // $499×10 → 2 months free ($415.83/mo)
        );

        // 4. Feature Mappings — Free
        // Lite AI (50 actions/mo) creates the "aha moment" on free and drives upgrade.
        context.PlanFeatureMappings.AddRange(
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fMaxStaff, NumericLimit = 1, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fMaxLocations, NumericLimit = 1, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fMaxClients, NumericLimit = 150, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fAiActions, NumericLimit = 50, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fOnlineBooking, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fSmsReminders, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fAiCopilot, IsEnabled = true }, // lite AI visible on Free to drive upgrade
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fAiWorkflows, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fAiInsights, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fWhiteLabel, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fApiAccess, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fAdvancedSecurity, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fMarketingAutomation, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fShowBranding, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = free, PricingFeature = fAgencyManagement, NumericLimit = 0, IsEnabled = false }
        );

        // Feature Mappings — Starter ($149/mo)
        // Limits raised from the old $39 tier (3 staff / 1 location / 500 AI / 1,000 clients).
        // At $149 a 3-staff cap would price the entry tier at ~$50/seat — worse value per seat
        // than Growth, and uncompetitive against Fresha (~$14.95/staff). At 10 staff this is
        // $14.90/seat, which reads as fair next to Growth's $19.96.
        // Branding is OFF here: only Free advertises "Powered by Upkilo". A paying customer
        // should not be marketing for us. fAiCopilot is what unlocks the AI actions quota set
        // below it; the advanced AI trio, white-label and API stay off as the Growth upgrade
        // story. (Comments sit above this call rather than between the arguments — dotnet
        // format re-indents comments interleaved in an argument list and fails CI.)
        context.PlanFeatureMappings.AddRange(
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fMaxStaff, NumericLimit = 10, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fMaxLocations, NumericLimit = 3, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fMaxClients, NumericLimit = 5000, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAiActions, NumericLimit = 2000, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fOnlineBooking, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fSmsReminders, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAiCopilot, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAiWorkflows, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAiInsights, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fWhiteLabel, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fApiAccess, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAdvancedSecurity, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fMarketingAutomation, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fShowBranding, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAgencyManagement, NumericLimit = 0, IsEnabled = false }
        );

        // Feature Mappings — Growth ($499/mo)
        // Consolidates the former Professional, Business and Agency tiers. Keeps Business's
        // limits (25 staff / 10 locations / 10,000 AI actions) and unlocks the full feature
        // set: advanced AI, marketing automation, white-label and API.
        //
        // Agency's 20 sub-accounts are NOT bundled here — that becomes a paid add-on, mirroring
        // ExtraStaffCount/ExtraLocationCount. Agency previously differed from Business only by
        // those sub-accounts and +5,000 AI actions, for $100/mo more, which is not a defensible
        // tier boundary. Anything beyond these limits is sold as an add-on rather than a tier.
        context.PlanFeatureMappings.AddRange(
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fMaxStaff, NumericLimit = 25, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fMaxLocations, NumericLimit = 10, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fMaxClients, NumericLimit = null, IsEnabled = true }, // Unlimited
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fAiActions, NumericLimit = 10000, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fOnlineBooking, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fSmsReminders, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fAiCopilot, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fAiWorkflows, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fAiInsights, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fWhiteLabel, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fApiAccess, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fAdvancedSecurity, IsEnabled = false }, // Enterprise only
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fMarketingAutomation, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fShowBranding, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = growth, PricingFeature = fAgencyManagement, NumericLimit = 0, IsEnabled = false }
        );

        // Feature Mappings — Enterprise
        context.PlanFeatureMappings.AddRange(
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fMaxStaff, NumericLimit = null, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fMaxLocations, NumericLimit = null, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fMaxClients, NumericLimit = null, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fAiActions, NumericLimit = 100000, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fOnlineBooking, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fSmsReminders, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fAiCopilot, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fAiWorkflows, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fAiInsights, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fWhiteLabel, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fApiAccess, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fAdvancedSecurity, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fMarketingAutomation, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fShowBranding, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = enterprise, PricingFeature = fAgencyManagement, NumericLimit = null, IsEnabled = true }
        );

        await context.SaveChangesAsync();
    }
}
