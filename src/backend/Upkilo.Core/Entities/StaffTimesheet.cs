using System;

namespace Upkilo.Core.Entities;

public class StaffTimesheet : TenantEntity
{
    public Guid StaffId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime ClockInTime { get => StartTime; set => StartTime = value; } // Alias
    
    public DateTime? EndTime { get; set; }
    public DateTime? ClockOutTime { get => EndTime; set => EndTime = value; } // Alias
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public decimal? TotalHours { get; set; }
    public bool IsOvertime { get; set; }

    public virtual StaffMember? Staff { get; set; }
}
