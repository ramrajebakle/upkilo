using System;

namespace Upkilo.Core.Entities;

public class ConversionEvent : TenantEntity
{
    public string EventName { get; set; } = string.Empty; // e.g. "booking_completed", "lead_form_submitted"
    public string EventCategory { get; set; } = string.Empty; // e.g. "booking", "lead", "purchase"
    public Guid? ClientId { get; set; }
    public string? Source { get; set; } // utm_source
    public string? Medium { get; set; } // utm_medium
    public string? CampaignTag { get; set; } // utm_campaign
    public string? Platform { get; set; } // Meta, Google, LinkedIn, Organic
    public decimal? Revenue { get; set; }
    public decimal? Value { get => Revenue; set => Revenue = value; } // Alias
    public string? ExternalClickId { get; set; } // fbclid, gclid, li_fat_id
    public bool SentToServer { get; set; } // CAPI flag
    public string? Metadata { get; set; } // JSON extra data
    public bool IsBilled { get; set; } = false; // Prevents double billing for leads
    public DateTime Timestamp { get => CreatedAt; set => CreatedAt = value; }

    // Navigation
    public virtual Client? Client { get; set; }
}
