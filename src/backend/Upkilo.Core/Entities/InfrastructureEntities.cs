using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// Multi-region deployment configuration.
/// </summary>
public class RegionConfig : TenantEntity
{
    public string RegionCode { get; set; } = "us-east"; // us-east, eu-west, asia
    public string RegionName { get; set; } = "US East";
    public string AzureRegion { get; set; } = "eastus";
    public bool IsPrimary { get; set; } = true;
    public string Status { get; set; } = "Active"; // Active, Standby, Failover
    public DateTime? LastSyncAt { get; set; }
    public string DataResidency { get; set; } = "US"; // Data residency compliance
}

/// <summary>
/// Live chat widget configuration and visitor tracking.
/// </summary>
public class ChatWidget : TenantEntity
{
    public bool IsEnabled { get; set; } = true;
    public string? WelcomeMessage { get; set; } = "Hi! How can we help?";
    public string? OfflineMessage { get; set; } = "We're away. Leave a message!";
    public string? PreChatFormFields { get; set; } // JSON: name, email, phone
    public string? CannedResponses { get; set; } // JSON array
    public string? Appearance { get; set; } // JSON: position, color, icon
    public bool EnableAiHandoff { get; set; } = true;
    public string? BusinessHours { get; set; } // JSON
}

/// <summary>
/// Chat visitor session for live chat widget.
/// </summary>
public class ChatVisitor : TenantEntity
{
    public string? VisitorName { get; set; }
    public string? VisitorEmail { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CurrentPage { get; set; }
    public string? Referrer { get; set; }
    public int PageViews { get; set; }
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// QR code check-in for bookings.
/// </summary>
public class BookingCheckIn : TenantEntity
{
    public Guid BookingId { get; set; }
    public string QrCodeData { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, CheckedIn, NoShow
    public DateTime? CheckedInAt { get; set; }
    public string? CheckInMethod { get; set; } // QR, Kiosk, Manual
    public int? EstimatedWaitMinutes { get; set; }
    public bool StaffNotified { get; set; }
}

/// <summary>
/// Two-factor authentication configuration per user.
/// </summary>
public class TwoFactorConfig : TenantEntity
{
    public Guid UserId { get; set; }
    public string Method { get; set; } = "TOTP"; // TOTP, SMS, Email
    public string? TotpSecret { get; set; } // Encrypted
    public string? BackupCodes { get; set; } // JSON array (hashed)
    public bool IsEnabled { get; set; }
    public bool EnforcedByRole { get; set; }
    public string? TrustedDevices { get; set; } // JSON array of device tokens
    public DateTime? LastVerifiedAt { get; set; }
}

/// <summary>
/// Comprehensive audit trail for all entity changes.
/// </summary>
public class AuditEntry : TenantEntity
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Create, Update, Delete
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string? ChangedFields { get; set; } // JSON array
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Compatibility aliases
    public Guid? PerformedById { get => UserId; set => UserId = value; }
    public DateTime PerformedAt { get => Timestamp; set => Timestamp = value; }
}

/// <summary>
/// Import job definition for CSV/Excel imports.
/// </summary>
public class DataImportJob : TenantEntity
{
    public string ImportType { get; set; } = string.Empty; // Clients, Bookings, Services
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Validating, Processing, Completed, Failed
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public int DuplicatesFound { get; set; }
    public string? FieldMapping { get; set; } // JSON: source -> target field mapping
    public string? ValidationErrors { get; set; } // JSON array
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Legal compliance document generator configuration.
/// </summary>
public class LegalDocument : TenantEntity
{
    public string DocumentType { get; set; } = string.Empty; // TermsOfService, PrivacyPolicy, CookiePolicy, CCPA, DPA
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // HTML
    // `new`: this is a domain version STRING (e.g. "1.2.0"), deliberately distinct from
    // BaseEntity.Version, which is an int concurrency counter. Declaring it `new` documents
    // the shadowing and silences CS0108. NOTE: this entity therefore has no usable
    // BaseEntity.Version concurrency token — see docs/PRODUCTION_DEPLOYMENT.md §4.
    public new string Version { get; set; } = "1.0";
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Locale { get; set; } = "en";
}

/// <summary>
/// Localization/translation entry.
/// </summary>
public class TranslationEntry : TenantEntity
{
    public string Locale { get; set; } = "en";
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Category { get; set; } // UI, Email, SMS, Notification
    public bool IsCustom { get; set; } // Tenant-specific override
}

/// <summary>
/// Tenant management — suspension, backup, sandbox.
/// </summary>
public class TenantManagement : TenantEntity
{
    public string Status { get; set; } = "Active"; // Active, Suspended, PendingDeletion, Sandbox
    public string? SuspensionReason { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? ScheduledDeletionAt { get; set; }
    public bool IsSandbox { get; set; }
    public string? LastBackupId { get; set; }
    public DateTime? LastBackupAt { get; set; }
    public string? DataExportUrl { get; set; }
}

/// <summary>
/// Client duplicate detection and merge tracking.
/// </summary>
public class DuplicateClientMatch : TenantEntity
{
    public Guid ClientAId { get; set; }
    public Guid ClientBId { get; set; }
    public decimal ConfidenceScore { get; set; } // 0-100
    public string MatchedFields { get; set; } = "[]"; // JSON: email, phone, name
    public string Status { get; set; } = "Pending"; // Pending, Merged, Dismissed
    public Guid? MergedIntoId { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
