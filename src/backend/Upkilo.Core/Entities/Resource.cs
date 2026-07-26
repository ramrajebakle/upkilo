using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// Bookable resource (rooms, equipment, vehicles, etc.)
/// </summary>
public class Resource : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "room"; // room, equipment, vehicle, other
    public string? Description { get; set; }
    public int Capacity { get; set; } = 1;
    public string? Amenities { get; set; } // JSON array
    public decimal? HourlyRate { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LinkedServiceIds { get; set; } // JSON array of service IDs
}

/// <summary>
/// Schedule block for staff unavailability / time-off / breaks.
/// </summary>
public class ScheduleBlock : TenantEntity
{
    public Guid StaffId { get; set; }
    public string Type { get; set; } = "time_off"; // time_off, break, personal
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool AllDay { get; set; } = true;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public string? RejectionReason { get; set; }
    public virtual StaffMember? Staff { get; set; }
}
