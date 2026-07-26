namespace Upkilo.Core.Entities;

/// <summary>
/// Equipment entity - tracks business assets and equipment
/// </summary>
public class Equipment : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; } // e.g., "Massage Table", "Sound System", "Sterilizer"
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Active;
    public string? Location { get; set; } // Room or area
    public Guid? AssignedToStaffId { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual StaffMember? AssignedToStaff { get; set; }
    public virtual ICollection<EquipmentMaintenance> MaintenanceRecords { get; set; } = new List<EquipmentMaintenance>();
}

/// <summary>
/// Equipment maintenance record
/// </summary>
public class EquipmentMaintenance : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public MaintenanceType Type { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Description { get; set; }
    public decimal? Cost { get; set; }
    public string? PerformedBy { get; set; } // Technician or vendor name
    public DateTime? NextDueDate { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Equipment? Equipment { get; set; }
}

public enum EquipmentStatus
{
    Active,
    InMaintenance,
    OutOfService,
    Retired,
    Loaned
}

public enum MaintenanceType
{
    Routine,
    Repair,
    Inspection,
    Calibration,
    Cleaning,
    Upgrade
}
