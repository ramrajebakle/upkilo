namespace Upkilo.Core.Entities;

/// <summary>
/// Tenant entity - represents a business/organization
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#06B6D4";
    public string? Industry { get; set; }
    public string? BusinessType { get; set; }
    public string? BusinessName { get; set; }
    public string? Sector { get; set; }
    public string? Tier { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EnforceTwoFactor { get; set; } // New property for security enforcement
    public bool EnforceTwoFactorForStaff { get; set; }
    public bool EnforceTwoFactorForClients { get; set; }
    public string Timezone { get; set; } = "UTC";
    public string Currency { get; set; } = "USD";
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Locale { get; set; } = "en-US";
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Starter;
    public Guid? PricingPlanId { get; set; }
    public Upkilo.Core.Entities.PricingPlan? PricingPlan { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeConnectId { get; set; }
    public string? StripeSubscriptionStatus { get; set; }
    public DateTime? SubscriptionPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Tagline { get; set; }
    public int ReviewCount { get; set; } = 0;
    public decimal AverageRating { get; set; } = 0;

    public Dictionary<string, object> Settings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
    public virtual ICollection<StaffMember> Staff { get; set; } = new List<StaffMember>();
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    // Computed property for suspension check
    public bool IsSuspended => Status == TenantStatus.Suspended;

    // Agency / Franchise Support
    public Guid? ParentTenantId { get; set; }
    public virtual Tenant? ParentTenant { get; set; }
    public virtual ICollection<Tenant> SubTenants { get; set; } = new List<Tenant>();
}

public enum TenantStatus
{
    Active,
    Suspended,
    Cancelled
}

public enum SubscriptionTier
{
    Free,
    Starter,
    Professional,
    Business,
    Agency,
    Enterprise
}
