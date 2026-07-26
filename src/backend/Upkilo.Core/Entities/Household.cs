namespace Upkilo.Core.Entities;

/// <summary>
/// Household entity - links multiple clients as a family unit
/// </summary>
public class Household : TenantEntity
{
    public string Name { get; set; } = string.Empty; // "Smith Family"
    public Guid PrimaryClientId { get; set; }
    public string? BillingAddress { get; set; }
    public string? Notes { get; set; }
    public bool SharedBilling { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Client? PrimaryClient { get; set; }
    public virtual ICollection<Client> Members { get; set; } = new List<Client>();
}
