using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// Staff shift for scheduling and tracking
/// </summary>
public class StaffShift : TenantEntity
{
    public Guid StaffId { get; set; }
    public Guid LocationId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Scheduled;
    public string? Notes { get; set; }

    // Navigation
    public virtual StaffMember? Staff { get; set; }
    public virtual Location? Location { get; set; }
}

/// <summary>
/// Time tracking for staff
/// </summary>
public class StaffClockIn : TenantEntity
{
    public Guid StaffId { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public string? IpAddress { get; set; }
    public string? LatLong { get; set; }
    public string? Device { get; set; }

    // Navigation
    public virtual StaffMember? Staff { get; set; }
    public virtual StaffShift? Shift { get; set; }
}

/// <summary>
/// Tracking commissions earned by staff
/// </summary>
public class StaffCommission : TenantEntity
{
    public Guid StaffId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? PaymentId { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal CommissionRate { get; set; } // Percentage or Fixed
    public decimal TotalEarned { get; set; }
    public decimal TipAmount { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public DateTime? PaidAt { get; set; }

    // Navigation
    public virtual StaffMember? Staff { get; set; }
    public virtual Booking? Booking { get; set; }
}

public enum ShiftStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}

public enum CommissionStatus
{
    Pending,
    Approved,
    Paid,
    Voided
}

public enum CommissionType
{
    Percentage,
    FixedAmount
}

public enum EmploymentType
{
    FullTime,
    PartTime,
    Contractor,
    Freelance
}

/// <summary>
/// Request to swap shifts between staff members
/// </summary>
public class StaffShiftSwap : TenantEntity
{
    public Guid RequestingStaffId { get; set; }
    public Guid RequestingShiftId { get; set; }
    public Guid? TargetStaffId { get; set; } // Can be null for open market
    public Guid? TargetShiftId { get; set; }
    public SwapStatus Status { get; set; } = SwapStatus.Pending;
    public string? Reason { get; set; }
    public DateTime? ActionedAt { get; set; }
    public string? AdminNotes { get; set; }

    // Navigation
    public virtual StaffMember? RequestingStaff { get; set; }
    public virtual StaffShift? RequestingShift { get; set; }
    public virtual StaffMember? TargetStaff { get; set; }
    public virtual StaffShift? TargetShift { get; set; }
}

public enum SwapStatus
{
    Pending,
    Offered,   // Offered to a specific person
    Accepted,  // Accepted by target, pending admin
    Approved,  // Finalized
    Rejected,
    Cancelled
}
