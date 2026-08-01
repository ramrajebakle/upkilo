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
        var free = new PricingPlan { Name = "Free", Description = "Get started with online booking at no cost", IsActive = true, TrialDays = 0 };
        var starter = new PricingPlan { Name = "Starter", Description = "Everything you need to run your business", IsActive = true, TrialDays = 14 };
        var pro = new PricingPlan { Name = "Professional", Description = "AI-powered growth for serious businesses", IsActive = true, TrialDays = 14 };
        var business = new PricingPlan { Name = "Business", Description = "Scale faster with autonomous AI and white-label", IsActive = true, TrialDays = 14 };
        var agency = new PricingPlan { Name = "Agency", Description = "Manage multiple client businesses from one account", IsActive = true, TrialDays = 14 };
        // Enterprise: custom quote — no fixed price, sales-led
        var enterprise = new PricingPlan { Name = "Enterprise", Description = "Security, Compliance, and Scale — custom pricing for your team", IsActive = true, TrialDays = 30, IsCustom = true };

        context.PricingPlans.AddRange(free, starter, pro, business, agency, enterprise);

        // 3. Add Prices (Monthly & Annual).
        // Annual prices reflect a 21% discount vs paying monthly for 12 months.
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
        context.PlanPrices.AddRange(
            new PlanPrice { PricingPlan = starter, Amount = 39, CurrencyCode = "USD", Cycle = BillingCycle.Monthly },
            new PlanPrice { PricingPlan = starter, Amount = 370, CurrencyCode = "USD", Cycle = BillingCycle.Annual }, // $39×12=$468 → save $98
            new PlanPrice { PricingPlan = pro, Amount = 89, CurrencyCode = "USD", Cycle = BillingCycle.Monthly },
            new PlanPrice { PricingPlan = pro, Amount = 844, CurrencyCode = "USD", Cycle = BillingCycle.Annual }, // $89×12=$1,068 → save $224
            new PlanPrice { PricingPlan = business, Amount = 199, CurrencyCode = "USD", Cycle = BillingCycle.Monthly },
            new PlanPrice { PricingPlan = business, Amount = 1887, CurrencyCode = "USD", Cycle = BillingCycle.Annual }, // $199×12=$2,388 → save $501
            new PlanPrice { PricingPlan = agency, Amount = 249, CurrencyCode = "USD", Cycle = BillingCycle.Monthly },
            new PlanPrice { PricingPlan = agency, Amount = 2361, CurrencyCode = "USD", Cycle = BillingCycle.Annual }  // $249×12=$2,988 → save $627
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

        // Feature Mappings — Starter
        // fAiCopilot was previously false despite having 500 AI action quota — corrected.
        context.PlanFeatureMappings.AddRange(
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fMaxStaff, NumericLimit = 3, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fMaxLocations, NumericLimit = 1, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fMaxClients, NumericLimit = 1000, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAiActions, NumericLimit = 500, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fOnlineBooking, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fSmsReminders, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAiCopilot, IsEnabled = true }, // unlocks the 500 AI actions quota
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAiWorkflows, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAiInsights, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fWhiteLabel, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fApiAccess, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAdvancedSecurity, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fMarketingAutomation, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fShowBranding, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = starter, PricingFeature = fAgencyManagement, NumericLimit = 0, IsEnabled = false }
        );

        // Feature Mappings — Professional
        // AI Workflow Builder moved down from Business: biggest upgrade trigger from Starter.
        context.PlanFeatureMappings.AddRange(
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fMaxStaff, NumericLimit = 10, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fMaxLocations, NumericLimit = 3, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fMaxClients, NumericLimit = null, IsEnabled = true }, // Unlimited
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fAiActions, NumericLimit = 3000, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fOnlineBooking, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fSmsReminders, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fAiCopilot, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fAiWorkflows, IsEnabled = true }, // moved down from Business
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fAiInsights, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fWhiteLabel, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fApiAccess, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fAdvancedSecurity, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fMarketingAutomation, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fShowBranding, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = pro, PricingFeature = fAgencyManagement, NumericLimit = 0, IsEnabled = false }
        );

        // Feature Mappings — Business
        context.PlanFeatureMappings.AddRange(
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fMaxStaff, NumericLimit = 25, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fMaxLocations, NumericLimit = 10, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fMaxClients, NumericLimit = null, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fAiActions, NumericLimit = 10000, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fOnlineBooking, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fSmsReminders, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fAiCopilot, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fAiWorkflows, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fAiInsights, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fWhiteLabel, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fApiAccess, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fAdvancedSecurity, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fMarketingAutomation, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fShowBranding, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = business, PricingFeature = fAgencyManagement, NumericLimit = 0, IsEnabled = false }
        );

        // Feature Mappings — Agency (Business features + sub-tenant management, up to 20 sub-accounts)
        context.PlanFeatureMappings.AddRange(
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fMaxStaff, NumericLimit = 25, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fMaxLocations, NumericLimit = 10, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fMaxClients, NumericLimit = null, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fAiActions, NumericLimit = 15000, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fOnlineBooking, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fSmsReminders, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fAiCopilot, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fAiWorkflows, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fAiInsights, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fWhiteLabel, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fApiAccess, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fAdvancedSecurity, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fMarketingAutomation, IsEnabled = true },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fShowBranding, IsEnabled = false },
            new PlanFeatureMapping { PricingPlan = agency, PricingFeature = fAgencyManagement, NumericLimit = 20, IsEnabled = true }
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
