namespace Upkilo.Core.Entities;

/// <summary>
/// Custom report definition for tenant-specific analytics.
/// </summary>
public class ReportDefinition : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ReportType { get; set; } = string.Empty; // Sales, Bookings, Clients, Inventory
    public string ConfigJson { get; set; } = "{}"; // JSON for filters, grouping, columns
    public string? CronSchedule { get; set; } // e.g. "0 9 * * 1" (Every Monday at 9 AM)
    public string? Recipients { get; set; } // Comma-separated emails
    public bool IsScheduled { get; set; }
    public bool IsPublic { get; set; }
    public bool IsArchived { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? LastRunAt { get; set; }
    public string? ScheduledEmailRecipients { get; set; } // Comma-separated emails for scheduled delivery
}

/// <summary>
/// Configuration for the client-facing booking portal.
/// </summary>
public class ClientPortalConfig : TenantEntity
{
    public string PortalName { get; set; } = string.Empty;
    public string? WelcomeMessage { get; set; }
    public string PrimaryColor { get; set; } = "#4F46E5";
    public string? LogoUrl { get; set; }
    public string? CustomDomain { get; set; }
    public bool AllowSelfRegistration { get; set; } = true;
    public bool AllowSelfCancellation { get; set; } = true;
    public int CancellationPolicyHours { get; set; } = 24;
    public bool ShowStaffSelection { get; set; } = true;
    public bool ShowPrice { get; set; } = true;
    public string? SocialLinksJson { get; set; } // JSON: facebook, instagram, etc.
}
