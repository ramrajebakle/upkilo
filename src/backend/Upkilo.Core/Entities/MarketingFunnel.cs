using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;

/// <summary>
/// Multi-step automated marketing funnel definition.
/// Steps are stored via FunnelStep entity linked by FunnelId.
/// </summary>
[Table("marketing_funnels")]
public class MarketingFunnel : TenantEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Status: draft, active, paused, completed, archived
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "draft";

    /// <summary>
    /// Trigger that starts the funnel: form_submit, tag_added, client_created, manual
    /// </summary>
    [MaxLength(100)]
    public string TriggerType { get; set; } = "manual";

    /// <summary>
    /// JSON config for trigger filters
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? TriggerConfig { get; set; }

    /// <summary>
    /// Goal type: booking_made, purchase_completed, form_submitted
    /// </summary>
    [MaxLength(100)]
    public string? ConversionGoal { get; set; }

    public bool IsActive { get; set; } = false;

    // Analytics
    public int TotalEntered { get; set; }
    public int TotalConverted { get; set; }
    public decimal ConversionRate { get; set; }

    public DateTime? ActivatedAt { get; set; }
    public DateTime? PausedAt { get; set; }
}

/// <summary>
/// Resource booking / reservation for rooms, equipment, vehicles
/// </summary>
[Table("resource_bookings")]
public class ResourceBooking : TenantEntity
{
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Optional link to a service booking
    /// </summary>
    public Guid? BookingId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Status: confirmed, pending, cancelled
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "confirmed";

    /// <summary>
    /// User or staff who made the booking
    /// </summary>
    public Guid? BookedByUserId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public virtual Resource? Resource { get; set; }
}
