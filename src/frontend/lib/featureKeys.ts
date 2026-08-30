/**
 * The entitlement vocabulary, mirrored from the backend's FeatureKeys.cs.
 *
 * WHY THIS FILE EXISTS
 * --------------------
 * The five FeatureGate call sites in the app used to pass invented PascalCase names —
 * "AiFeatures", "CustomBranding", "ApiAccess", "WhiteLabelDomain", "Webhooks" — none of which
 * appear in the catalogue the API actually returns. `hasFeature` looks the name up in
 * `enabledFeatures` with a plain object index, which is case- and separator-sensitive, so every
 * one of those lookups returned undefined and the gate rendered "Upgrade your plan" to every
 * customer on every plan, including Enterprise accounts that had paid for the feature.
 *
 * Keys are declared here once so a gate cannot be written against a name that does not exist:
 * `FeatureKey` is a union type, so a typo is a TypeScript error rather than a silent denial.
 *
 * KEEPING IT IN SYNC: these MUST match FeatureKeys.All in
 * src/backend/Upkilo.Core/Entities/Pricing/FeatureKeys.cs. The backend asserts its own list
 * against the seeded database in EntitlementCatalogTests; this file is checked against the
 * live API payload by the featureKeys contract test.
 */
export const FEATURES = {
  // Numeric limits
  MAX_STAFF: 'max_staff',
  MAX_LOCATIONS: 'max_locations',
  MAX_CLIENTS: 'max_clients',
  AI_ACTIONS: 'ai_actions',
  AGENCY_MANAGEMENT: 'agency_management',

  // Boolean capabilities
  ONLINE_BOOKING: 'online_booking',
  SMS_REMINDERS: 'sms_reminders',
  /** AI generation and assistant actions. Enabled on Free, quota-limited. */
  AI_COPILOT: 'ai_copilot',
  /** AI workflow builder and the escalation/approval queue it feeds. */
  AI_WORKFLOWS: 'ai_workflows',
  /** Predictive and analytical AI — forecasting, BI, narrated summaries. */
  AI_INSIGHTS: 'ai_insights',
  /** Custom branding AND custom domains — the catalogue treats them as one feature. */
  WHITE_LABEL: 'white_label',
  /** Public API AND webhooks — "API & Webhooks" is a single catalogue entry. */
  API_ACCESS: 'api_access',
  ADVANCED_SECURITY: 'advanced_security',
  MARKETING_AUTOMATION: 'marketing_automation',
  SHOW_POWERED_BY_BRANDING: 'show_powered_by_branding',
} as const;

export type FeatureKey = (typeof FEATURES)[keyof typeof FEATURES];

/** Every key, for the contract test that compares this list against the API payload. */
export const ALL_FEATURE_KEYS: FeatureKey[] = Object.values(FEATURES);

/** Keys whose value is a quantity rather than an on/off capability. */
export const NUMERIC_FEATURE_KEYS: FeatureKey[] = [
  FEATURES.MAX_STAFF,
  FEATURES.MAX_LOCATIONS,
  FEATURES.MAX_CLIENTS,
  FEATURES.AI_ACTIONS,
  FEATURES.AGENCY_MANAGEMENT,
];

/** Matches EntitlementLimits.Unlimited on the backend. */
export const UNLIMITED = -1;
