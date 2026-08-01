using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;

/// <summary>
/// Marketing campaign entity
/// </summary>
[Table("campaigns")]
public class Campaign : TenantEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Campaign type: email, sms, automated
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "email";

    /// <summary>
    /// Status: draft, scheduled, sending, sent, cancelled
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "draft";

    [MaxLength(500)]
    public string? Subject { get; set; }

    [MaxLength(500)]
    public string? Preheader { get; set; }

    public string? Content { get; set; }

    public Guid? TemplateId { get; set; }

    [MaxLength(100)]
    public string? AudienceType { get; set; } // all_clients, segment, list

    [Column(TypeName = "jsonb")]
    public string? AudienceFilters { get; set; }

    [MaxLength(200)]
    public string? TargetSegment { get; set; } // Target audience segment name

    public string? MessageBody { get; set; } // SMS/WhatsApp message body

    public int SentCount { get; set; } // Number of messages sent

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }
}

/// <summary>
/// Detailed analytics for a campaign
/// </summary>
[Table("campaign_analytics")]
public class CampaignAnalytics : TenantEntity
{
    public Guid CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int OpenedCount { get; set; }
    public int ClickedCount { get; set; }
    public int BouncedCount { get; set; }
    public int UnsubscribedCount { get; set; }
    public int ConversionCount { get; set; }
    public decimal RevenueGenerated { get; set; }

    /// <summary>
    /// Hourly breakdown of opens/clicks as JSON
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? TimelineData { get; set; }

    /// <summary>
    /// Device breakdown as JSON
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? DeviceData { get; set; }
}

/// <summary>
/// Automation for specific events (e.g., Welcome, Birthday)
/// </summary>
[Table("marketing_auto_responders")]
public class MarketingAutoResponder : TenantEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string TriggerEvent { get; set; } = "client.created";

    public Guid? EmailTemplateId { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }

    public int DelayMinutes { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
