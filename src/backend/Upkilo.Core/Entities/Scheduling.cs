namespace Upkilo.Core.Entities;

/// <summary>
/// Staff working hours template for weekly schedule
/// </summary>
public class WorkingHours : TenantEntity
{
    public Guid StaffId { get; set; }

    /// <summary>
    /// Day of week (0 = Sunday, 6 = Saturday)
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>
    /// Is this day a working day
    /// </summary>
    public bool IsWorkingDay { get; set; } = true;

    /// <summary>
    /// Start time (e.g., 09:00)
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time (e.g., 17:00)
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Break start time (optional)
    /// </summary>
    public TimeSpan? BreakStartTime { get; set; }

    /// <summary>
    /// Break end time (optional)
    /// </summary>
    public TimeSpan? BreakEndTime { get; set; }
}

/// <summary>
/// Schedule exception (vacation, sick day, special hours)
/// </summary>
public class ScheduleException : TenantEntity
{
    public Guid StaffId { get; set; }

    /// <summary>
    /// Date of the exception
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Type: time_off, vacation, sick, holiday, custom_hours
    /// </summary>
    public string Type { get; set; } = "time_off";

    /// <summary>
    /// Is the entire day off
    /// </summary>
    public bool IsAllDay { get; set; } = true;

    /// <summary>
    /// Custom start time (if not all day)
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Custom end time (if not all day)
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Reason/notes
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Slot hold for temporary reservation during booking
/// </summary>
public class SlotHold : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }

    /// <summary>
    /// Date and time of the held slot
    /// </summary>
    public DateTime SlotDateTime { get; set; }

    /// <summary>
    /// Duration in minutes
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Session token of the user holding the slot
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// When the hold expires (typically 10-15 minutes)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Has the hold been released
    /// </summary>
    public bool IsReleased { get; set; }
    public bool IsConverted { get; set; } // Compatibility alias
}

/// <summary>
/// Location/branch for multi-location businesses
/// </summary>
public class Location : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // Contact
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // Geo
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>
    /// Timezone for this location (e.g., "America/New_York")
    /// </summary>
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Is this the primary/main location
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Is location active and accepting bookings
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Color for calendar display
    /// </summary>
    public string Color { get; set; } = "#3B82F6";

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Operating/Business hours (JSON structure)
    /// </summary>
    public string? BusinessHours { get; set; }

    /// <summary>
    /// Holiday schedule (JSON structure)
    /// </summary>
    public string? Holidays { get; set; }
}

/// <summary>
/// Precomputed availability for rapid slot searching
/// </summary>
public class AvailabilityCache : TenantEntity
{
    public Guid StaffId { get; set; }
    public DateOnly Date { get; set; }
    
    /// <summary>
    /// Bitmask or JSON representation of available 15-min slots
    /// 00000000... (96 bits for 24 hours)
    /// </summary>
    public string AvailableSlotsMask { get; set; } = string.Empty;
    
    public DateTime LastUpdatedAt { get; set; }
}
