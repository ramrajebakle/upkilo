using System;

namespace Upkilo.Core.Entities;

public class AdvancedFeatures : TenantEntity
{
    public bool EnableApiAccess { get; set; }
    public bool EnableCustomWebhooks { get; set; }
    public bool EnableWhiteLabel { get; set; }
    public bool EnablePrioritySupport { get; set; }
    public bool EnableCustomSmsSenderId { get; set; }
    public bool EnableAdvancedReporting { get; set; }
    public bool EnableIpAllowlisting { get; set; }
}

/// <summary>
/// AI Voice Agent call tracking and management.
/// </summary>
public class VoiceCall : TenantEntity
{
    public Guid? ClientId { get; set; }
    public string Direction { get; set; } = "Inbound"; // Inbound, Outbound
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Ringing"; // Ringing, InProgress, Completed, Failed, Transferred
    public int DurationSeconds { get; set; }
    public string? RecordingUrl { get; set; }
    public string? TranscriptText { get; set; }
    public string? TranscriptSummary { get; set; }
    public string Purpose { get; set; } = "General"; // General, Booking, Reminder, FollowUp
    public bool WasTransferredToHuman { get; set; }
    public Guid? BookingCreatedId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}

/// <summary>
/// Plugin/extension definition for the marketplace system.
/// </summary>
public class PluginDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // CRM, Marketing, Payments, Analytics
    public string? IconUrl { get; set; }
    public string? ManifestJson { get; set; } // Plugin capabilities and hooks
    public string? SettingsSchema { get; set; } // JSON Schema for plugin settings
    public decimal Price { get; set; }
    public bool IsFree { get; set; } = true;
    public int InstallCount { get; set; }
    public decimal Rating { get; set; }
    public bool IsVerified { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Per-tenant plugin installation.
/// </summary>
public class PluginInstallation : TenantEntity
{
    public Guid PluginId { get; set; }
    public string? SettingsJson { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public virtual PluginDefinition? Plugin { get; set; }
}



/// <summary>
/// Enterprise SSO configuration for SAML/OIDC.
/// </summary>
public class SsoConfig : TenantEntity
{
    public string Provider { get; set; } = string.Empty; // Okta, AzureAD, Google
    public string Protocol { get; set; } = "SAML"; // SAML, OIDC
    public string? EntityId { get; set; }
    public string? MetadataUrl { get; set; }
    public string? SignInUrl { get; set; }
    public string? Certificate { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AttributeMapping { get; set; } // JSON: email, firstName, lastName
    public bool IsEnabled { get; set; }
    public bool EnforceForAllUsers { get; set; }
}

/// <summary>
/// Predictive analytics model and scoring results.
/// </summary>
public class PredictiveScore : TenantEntity
{
    public Guid? ClientId { get; set; }
    public string ScoreType { get; set; } = string.Empty; // ChurnRisk, NoShowRisk, LTV, DemandForecast
    public decimal Score { get; set; } // 0-100
    public decimal Confidence { get; set; }
    public string? Factors { get; set; } // JSON: top contributing factors
    public string? Recommendation { get; set; } // AI-generated recommendation
    public DateTime ScoredAt { get; set; } = DateTime.UtcNow;
}
