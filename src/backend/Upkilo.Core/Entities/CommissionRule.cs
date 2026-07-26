namespace Upkilo.Core.Entities;

/// <summary>
/// Defines commission rules for staff members — percentage or fixed amount per service/category.
/// </summary>
public class CommissionRule : TenantEntity
{
    public Guid? StaffId { get; set; }         // null = default rule for all staff
    public Guid? ServiceId { get; set; }       // null = applies to all services
    public string? ServiceCategory { get; set; } // optional category filter
    public CommissionType Type { get; set; } = CommissionType.Percentage;
    public decimal Rate { get; set; }           // percentage (e.g., 15.0) or fixed amount
    public decimal? MinAmount { get; set; }     // minimum commission floor
    public decimal? MaxAmount { get; set; }     // maximum commission cap
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }           // higher = more specific rule wins
    public string? Description { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveUntil { get; set; }

    // Navigation
    public virtual StaffMember? Staff { get; set; }
    public virtual Service? Service { get; set; }
}
