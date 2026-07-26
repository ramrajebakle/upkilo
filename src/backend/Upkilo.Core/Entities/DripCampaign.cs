namespace Upkilo.Core.Entities;

/// <summary>
/// Multi-channel drip campaign — an automated multi-step sequence (email/SMS/push/WhatsApp)
/// triggered by a client lifecycle event. Steps are stored as a JSON document to keep the
/// sequence self-contained without a separate step table.
/// </summary>
public class DripCampaign : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>signup | booking_created | booking_completed | no_show | tag_added | custom_date</summary>
    public string TriggerType { get; set; } = "signup";

    /// <summary>draft | active | paused | archived</summary>
    public string Status { get; set; } = "draft";

    /// <summary>Serialized array of DripStep objects (channel, delayDays, delayHours, subject, body, condition).</summary>
    public string StepsJson { get; set; } = "[]";

    // Aggregate engagement counters (updated by the execution engine when one exists).
    public int EnrolledCount { get; set; }
    public int CompletedCount { get; set; }
    public double OpenRate { get; set; }
    public double ClickRate { get; set; }
}
