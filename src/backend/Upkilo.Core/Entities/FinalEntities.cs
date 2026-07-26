using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// PWA/Offline sync configuration and queue tracking.
/// </summary>
public class OfflineSyncQueue : TenantEntity
{
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty; // Booking, Client, etc.
    public Guid EntityId { get; set; }
    public string Action { get; set; } = "Create"; // Create, Update, Delete
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = "Pending"; // Pending, Synced, Conflict
    public string? ConflictResolution { get; set; } // Server, Client, Manual
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SyncedAt { get; set; }
}

/// <summary>
/// Accessibility compliance scan result (WCAG 2.1).
/// </summary>
public class AccessibilityScan : TenantEntity
{
    public string PageUrl { get; set; } = string.Empty;
    public string ScanEngine { get; set; } = "axe-core";
    public int ViolationCount { get; set; }
    public int PassCount { get; set; }
    public int IncompleteCount { get; set; }
    public string? Violations { get; set; } // JSON array of violations
    public string WcagLevel { get; set; } = "AA"; // A, AA, AAA
    public decimal ComplianceScore { get; set; } // 0-100
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// API error code catalog for standardized error responses.
/// </summary>
public class ApiErrorCode : TenantEntity
{
    public string ErrorCode { get; set; } = string.Empty; // e.g. "BOOKING_001"
    public string Category { get; set; } = string.Empty; // Auth, Booking, Payment, etc.
    public int HttpStatusCode { get; set; } = 400;
    public string Message { get; set; } = string.Empty;
    public string? DetailTemplate { get; set; } // Parameterized detail message
    public string? Resolution { get; set; } // Help text for API consumers
    public bool IsDeprecated { get; set; }
    public string? DeprecationNotice { get; set; }
    public string ApiVersion { get; set; } = "v1";
}

/// <summary>
/// Database migration tracking and zero-downtime operation config.
/// </summary>
public class MigrationRecord : TenantEntity
{
    public string MigrationId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty; // SQL
    public string? RollbackScript { get; set; } // SQL
    public string Status { get; set; } = "Pending"; // Pending, Applied, RolledBack, Failed
    public bool IsZeroDowntime { get; set; } = true;
    public DateTime? AppliedAt { get; set; }
    public DateTime? RolledBackAt { get; set; }
}

/// <summary>
/// Deployment record for blue/green and canary releases.
/// </summary>
public class DeploymentRecord : TenantEntity
{
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = "Production"; // Staging, Production
    public string Strategy { get; set; } = "BlueGreen"; // BlueGreen, Canary, Rolling
    public string Status { get; set; } = "Deploying"; // Deploying, Active, RolledBack, Archived
    public int? CanaryPercentage { get; set; }
    public string? HealthCheckUrl { get; set; }
    public bool HealthCheckPassed { get; set; }
    public string? RollbackReason { get; set; }
    public DateTime? DeployedAt { get; set; }
    public DateTime? RolledBackAt { get; set; }
}

/// <summary>
/// Incident and on-call management.
/// </summary>
public class IncidentRecord : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = "P3"; // P1, P2, P3, P4
    public string Status { get; set; } = "Open"; // Open, Investigating, Mitigated, Resolved, PostMortem
    public string? Description { get; set; }
    public string? AffectedServices { get; set; } // JSON array
    public string? RootCause { get; set; }
    public string? ResolutionSteps { get; set; } // JSON array
    public string? PostMortemUrl { get; set; }
    public Guid? OnCallResponderId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? TimeTotResolutionMinutes { get; set; }
}

/// <summary>
/// UI preferences per user (dark mode, shortcuts, etc.).
/// </summary>
public class UserUiPreference : TenantEntity
{
    public Guid UserId { get; set; }
    public bool DarkModeEnabled { get; set; }
    public string? KeyboardShortcuts { get; set; } // JSON map of custom shortcuts
    public string? CalendarColorCoding { get; set; } // JSON: service/staff -> color
    public string? DashboardLayout { get; set; } // JSON: widget order/visibility
    public string? Timezone { get; set; }
    public string? Locale { get; set; }
}
