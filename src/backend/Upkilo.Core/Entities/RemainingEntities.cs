using System;

namespace Upkilo.Core.Entities;

// ─── Phase 6: Remaining Infrastructure ───

/// <summary>
/// Auto-scaling policy configuration.
/// </summary>
public class ScalingPolicy : TenantEntity
{
    public string ResourceType { get; set; } = "WebApp"; // WebApp, Database, Cache
    public int MinInstances { get; set; } = 1;
    public int MaxInstances { get; set; } = 10;
    public int CurrentInstances { get; set; } = 1;
    public int CpuThresholdPercent { get; set; } = 80;
    public int MemoryThresholdPercent { get; set; } = 80;
    public int CooldownSeconds { get; set; } = 300;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Hangfire cluster worker node configuration.
/// </summary>
public class HangfireWorkerNode : TenantEntity
{
    public string NodeId { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active, Draining, Offline
    public string QueuePriority { get; set; } = "normal"; // high, normal, low
    public int ConcurrencyLimit { get; set; } = 10;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}

// ─── Phase 7: Remaining Features ───

/// <summary>
/// Section/page template for website/landing page builder.
/// </summary>
public class SectionTemplate : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Hero"; // Hero, Features, Testimonials, CTA
    public string HtmlContent { get; set; } = string.Empty;
    public string? CssContent { get; set; }
    public string? JsContent { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsSystem { get; set; } // Built-in vs user-created
    public int UsageCount { get; set; }
}

/// <summary>
/// Page-level analytics tracking.
/// </summary>
public class PageAnalytics : TenantEntity
{
    public string PageUrl { get; set; } = string.Empty;
    public string PageType { get; set; } = "LandingPage"; // LandingPage, Form, Booking
    public int TotalViews { get; set; }
    public int UniqueVisitors { get; set; }
    public decimal BounceRate { get; set; }
    public decimal AvgTimeOnPageSeconds { get; set; }
    public int Conversions { get; set; }
    public decimal ConversionRate { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime Timestamp { get => PeriodStart; set => PeriodStart = value; }
}

/// <summary>
/// Funnel step definition for lead conversion tracking.
/// </summary>
public class FunnelStep : TenantEntity
{
    public Guid FunnelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string StepType { get; set; } = "Page"; // Page, Form, Payment, Booking
    public string? PageUrl { get; set; }
    public int EnteredCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal DropOffRate { get; set; }
}

/// <summary>
/// Gated content access for membership/courses.
/// </summary>
public class GatedContent : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string ContentType { get; set; } = "Video"; // Video, PDF, Course, Download
    public string? FileUrl { get; set; }
    public string? Description { get; set; }
    public bool RequiresAuth { get; set; } = true;
    public string? RequiredMembershipTier { get; set; }
    public int AccessCount { get; set; }
    public bool IsDripContent { get; set; }
    public int? DripDelayDays { get; set; }
    public DateTime? AvailableFrom { get; set; }
}

/// <summary>
/// Member progress tracking for courses/content.
/// </summary>
public class MemberProgress : TenantEntity
{
    public Guid ClientId { get; set; }
    public Guid ContentId { get; set; }
    public decimal ProgressPercent { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    public string? BookmarkData { get; set; } // JSON
}

// ─── Phase 7.13-7.14: Mobile & Integrations ───

/// <summary>
/// Mobile push notification registration.
/// </summary>
public class PushNotificationToken : TenantEntity
{
    public Guid UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "FCM"; // FCM, APNS
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// External accounting integration sync (QuickBooks, Xero).
/// </summary>
public class AccountingSyncConfig : TenantEntity
{
    public string Provider { get; set; } = "QuickBooks"; // QuickBooks, Xero
    public string? AccessToken { get; set; } // Encrypted
    public string? RefreshToken { get; set; } // Encrypted
    public string? CompanyId { get; set; }
    public bool SyncInvoices { get; set; } = true;
    public bool SyncPayments { get; set; } = true;
    public bool SyncContacts { get; set; } = true;
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }
}

/// <summary>
/// Email marketing export config (Mailchimp, Klaviyo).
/// </summary>
public class EmailMarketingSync : TenantEntity
{
    public string Provider { get; set; } = "Mailchimp"; // Mailchimp, Klaviyo
    public string? ApiKey { get; set; } // Encrypted
    public string? ListId { get; set; }
    public bool AutoSync { get; set; } = true;
    public string? FieldMapping { get; set; } // JSON
    public DateTime? LastExportAt { get; set; }
    public int LastExportCount { get; set; }
}

/// <summary>
/// External calendar 2-way sync (Google, Outlook).
/// </summary>
public class CalendarSync : TenantEntity
{
    public Guid StaffMemberId { get; set; }
    public string Provider { get; set; } = "Google"; // Google, Outlook
    public string? AccessToken { get; set; } // Encrypted
    public string? RefreshToken { get; set; } // Encrypted
    public string? CalendarId { get; set; }
    public bool SyncToExternal { get; set; } = true;
    public bool SyncFromExternal { get; set; } = true;
    public DateTime? LastSyncAt { get; set; }
    public string? SyncErrors { get; set; } // JSON
}

// ─── Phase 8: Data Warehouse & SDK ───

/// <summary>
/// Data warehouse export configuration.
/// </summary>
public class DataWarehouseExport : TenantEntity
{
    public string Provider { get; set; } = "BigQuery"; // BigQuery, Snowflake, Redshift
    public string ConnectionString { get; set; } = string.Empty; // Encrypted
    public string? DatasetName { get; set; }
    public bool IncrementalSync { get; set; } = true;
    public string CronSchedule { get; set; } = "0 2 * * *"; // Daily 2AM
    public string? TableMapping { get; set; } // JSON: entity -> table
    public DateTime? LastExportAt { get; set; }
    public long LastExportRowCount { get; set; }
    public string Status { get; set; } = "Active"; // Active, Paused, Error
}

/// <summary>
/// SDK download and sandbox environment tracking.
/// </summary>
public class SdkRelease : TenantEntity
{
    public string Language { get; set; } = "JavaScript"; // JavaScript, Python, PHP
    // `new`: this is a domain version STRING (e.g. "1.2.0"), deliberately distinct from
    // BaseEntity.Version, which is an int concurrency counter. Declaring it `new` documents
    // the shadowing and silences CS0108. NOTE: this entity therefore has no usable
    // BaseEntity.Version concurrency token — see docs/PRODUCTION_DEPLOYMENT.md §4.
    public new string Version { get; set; } = "1.0.0";
    public string? DownloadUrl { get; set; }
    public string? ChangelogUrl { get; set; }
    public int DownloadCount { get; set; }
    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// API sandbox environment for developer testing.
/// </summary>
public class SandboxEnvironment : TenantEntity
{
    public string SandboxId { get; set; } = Guid.NewGuid().ToString("N");
    public Guid ApiKeyId { get; set; }
    public string? SeedDataConfig { get; set; } // JSON: what data to seed
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}

// ─── Phase 9.1: Shipping ───

/// <summary>
/// Shipping provider integration for e-commerce orders.
/// </summary>
public class ShippingProvider : TenantEntity
{
    public string Name { get; set; } = string.Empty; // FedEx, UPS, USPS, DHL
    public string? ApiKey { get; set; } // Encrypted
    public string? AccountNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SupportedRegions { get; set; } // JSON array
    public string? DefaultServiceLevel { get; set; } // Ground, Express, Overnight
}

// ─── Phase 7.15 & 10: Compliance & Testing ───

/// <summary>
/// HIPAA compliance configuration and audit.
/// </summary>
public class HipaaConfig : TenantEntity
{
    public bool IsEnabled { get; set; }
    public bool EncryptionAtRest { get; set; } = true;
    public bool EncryptionInTransit { get; set; } = true;
    public bool AccessLogging { get; set; } = true;
    public string? BaaDocument { get; set; } // Business Associate Agreement URL
    public DateTime? LastAuditAt { get; set; }
    public string? AuditFindings { get; set; } // JSON
}

/// <summary>
/// SOC2 compliance evidence tracking.
/// </summary>
public class Soc2Evidence : TenantEntity
{
    public string ControlId { get; set; } = string.Empty; // CC6.1, CC7.2, etc.
    public string Category { get; set; } = string.Empty; // Security, Availability, Confidentiality
    public string Description { get; set; } = string.Empty;
    public string EvidenceType { get; set; } = "Screenshot"; // Screenshot, Log, Config, Policy
    public string? EvidenceUrl { get; set; }
    public string Status { get; set; } = "Compliant"; // Compliant, NonCompliant, InProgress
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

