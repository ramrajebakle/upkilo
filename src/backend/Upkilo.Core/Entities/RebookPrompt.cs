namespace Upkilo.Core.Entities;

/// <summary>
/// Automated rebooking prompt rule — nudges clients to book again based on a lifecycle trigger.
/// Backs the /bookings/rebook page.
/// </summary>
public class RebookPrompt : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>days_since_last_visit | after_service | birthday_month | seasonal</summary>
    public string Trigger { get; set; } = "days_since_last_visit";

    /// <summary>e.g. number of days for days_since_last_visit.</summary>
    public int? TriggerValue { get; set; }

    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }

    /// <summary>sms | email | push</summary>
    public string Channel { get; set; } = "sms";

    public string Message { get; set; } = string.Empty;
    public string? Subject { get; set; }

    public bool IsActive { get; set; }
    public int SendCount { get; set; }
    public int ConversionCount { get; set; }
    public double ConversionRate { get; set; }
    public DateTime? LastSent { get; set; }
}
