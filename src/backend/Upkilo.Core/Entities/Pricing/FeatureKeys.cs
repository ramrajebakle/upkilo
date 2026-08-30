namespace Upkilo.Core.Entities;

/// <summary>
/// The canonical entitlement vocabulary — every key that may legally appear in a
/// <c>PricingFeature.Key</c> column, a <c>[RequiresFeature]</c> attribute, a
/// <c>TenantFeatureOverride.FeatureKey</c>, or a frontend feature gate.
///
/// WHY THIS EXISTS
/// ---------------
/// Before this file the gate names were free-form strings invented at each call site, and
/// they did not match the catalogue PricingSeeder actually writes. Every
/// <c>[RequiresFeature]</c> in the API asked for a PascalCase name — "AiFeatures",
/// "ApiAccess", "WhiteLabelDomain", "AiCopilot" — while the database only ever contained
/// snake_case keys ("ai_copilot", "api_access", "white_label"). CheckFeatureAccessAsync
/// compares with OrdinalIgnoreCase, which folds case but NOT the underscore, so every one of
/// those lookups missed, returned null, and denied the request. The effect was not a leak but
/// its mirror image: AI, API keys, custom domains, webhooks and branding were refused for
/// every tenant on every plan, Enterprise included. The five frontend FeatureGate call sites
/// used the same invented names against the same catalogue and permanently rendered
/// "Upgrade your plan" to customers who had already paid for the feature.
///
/// Nothing caught it because the only tests over the resolver asserted the negative cases
/// (unknown key -> false, no mappings -> false), which pass just as happily when the key
/// vocabulary is entirely wrong.
///
/// So: keys are declared here once, referenced as constants everywhere, and
/// <see cref="All"/> is asserted against the seeded catalogue by EntitlementCatalogTests.
/// A typo is now a compile error, and a key that exists in code but not in the database
/// fails a test rather than silently denying paying customers.
///
/// ADDING A FEATURE: add the constant, add it to <see cref="All"/>, seed a PricingFeature row
/// with the same key in PricingSeeder, and map it on every plan. The catalogue test enforces
/// all four.
/// </summary>
public static class FeatureKeys
{
    // ── Numeric limits ────────────────────────────────────────────────────────
    /// <summary>Max active staff members. NumericLimit null = unlimited.</summary>
    public const string MaxStaff = "max_staff";

    /// <summary>Max active locations. NumericLimit null = unlimited.</summary>
    public const string MaxLocations = "max_locations";

    /// <summary>Max client records. NumericLimit null = unlimited.</summary>
    public const string MaxClients = "max_clients";

    /// <summary>AI actions per billing period. Drives the AiCredits usage quota.</summary>
    public const string AiActions = "ai_actions";

    /// <summary>Agency sub-account provisioning. NumericLimit = max sub-tenants.</summary>
    public const string AgencyManagement = "agency_management";

    // ── Boolean capabilities ──────────────────────────────────────────────────
    /// <summary>Public online booking pages.</summary>
    public const string OnlineBooking = "online_booking";

    /// <summary>SMS reminders. Gates the Sms usage quota.</summary>
    public const string SmsReminders = "sms_reminders";

    /// <summary>
    /// AI generation and assistant actions — copy generation, chat, the copilot rail.
    /// Enabled on Free (quota-limited) deliberately, per PricingSeeder: lite AI is the
    /// upgrade hook. Gates every endpoint that GENERATES with AI.
    /// </summary>
    public const string AiCopilot = "ai_copilot";

    /// <summary>AI workflow builder and the escalation/approval queue it feeds.</summary>
    public const string AiWorkflows = "ai_workflows";

    /// <summary>
    /// Predictive and analytical AI — demand forecasting, business intelligence, narrated
    /// summaries. Gates every endpoint that ANALYSES with AI, as opposed to generating.
    /// </summary>
    public const string AiInsights = "ai_insights";

    /// <summary>
    /// Custom branding and white-label. Covers both the branding settings surface and
    /// custom domains: PricingSeeder names this "Custom Branding / White-Label", so the
    /// domain feature is part of it rather than a separate key.
    /// </summary>
    public const string WhiteLabel = "white_label";

    /// <summary>
    /// Public API and webhooks. PricingSeeder names this "API &amp; Webhooks" — webhooks are
    /// deliberately bundled here and have no separate key.
    /// </summary>
    public const string ApiAccess = "api_access";

    /// <summary>SSO / SAML / extended audit log retention.</summary>
    public const string AdvancedSecurity = "advanced_security";

    /// <summary>Marketing automation — campaigns, drip sequences, journeys.</summary>
    public const string MarketingAutomation = "marketing_automation";

    /// <summary>
    /// Shows "Powered by Upkilo" on public booking pages. Inverted sense: enabled means the
    /// tenant CARRIES our branding (Free), disabled means they do not (paid).
    /// </summary>
    public const string ShowPoweredByBranding = "show_powered_by_branding";

    /// <summary>
    /// Every key above. Kept in sync with the seeded catalogue by EntitlementCatalogTests,
    /// and used by the startup validator to reject unknown gate names.
    /// </summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        MaxStaff,
        MaxLocations,
        MaxClients,
        AiActions,
        AgencyManagement,
        OnlineBooking,
        SmsReminders,
        AiCopilot,
        AiWorkflows,
        AiInsights,
        WhiteLabel,
        ApiAccess,
        AdvancedSecurity,
        MarketingAutomation,
        ShowPoweredByBranding,
    };

    /// <summary>
    /// Keys whose value is a quantity rather than an on/off capability. Used by the admin
    /// override UI to decide whether to ask for a limit, and by the resolver to decide
    /// whether NumericLimit is meaningful.
    /// </summary>
    public static readonly IReadOnlySet<string> Numeric = new HashSet<string>(StringComparer.Ordinal)
    {
        MaxStaff,
        MaxLocations,
        MaxClients,
        AiActions,
        AgencyManagement,
    };

    /// <summary>
    /// True when <paramref name="key"/> is part of the canonical catalogue. Comparison is
    /// ordinal and case-sensitive on purpose: "AiCopilot" must NOT resolve to "ai_copilot".
    /// Silently folding the difference is what would let the original defect back in, this
    /// time as a leak rather than a denial.
    /// </summary>
    public static bool IsKnown(string? key) => key != null && All.Contains(key);
}
