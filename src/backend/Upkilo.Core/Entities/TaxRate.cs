namespace Upkilo.Core.Entities;

/// <summary>
/// Represents a tax rate that can be applied to services or invoices
/// </summary>
public class TaxRate : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    // Navigation
    public virtual Tenant? Tenant { get; set; }
}
