using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public enum WaitlistStatus
{
    Pending,
    Waiting,
    Notified,
    Booked,
    Converted,
    Expired,
    Cancelled
}

public class WaitlistEntry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? ClientId { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;
    
    public DateTime PreferredDate { get; set; }
    public string? PreferredTimeRange { get; set; } // e.g. "Morning", "Afternoon", "Anytime"
    public string? Notes { get; set; }
    
    public bool IsConverted { get; set; } = false; // Converted to booking?
    public Guid? StaffId { get; set; }
    public int Priority { get; set; } = 0;
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Tenant? Tenant { get; set; }
    public Service? Service { get; set; }
    public Client? Client { get; set; }
}
