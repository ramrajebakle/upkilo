namespace Upkilo.Core.Entities;

/// <summary>
/// Service entity - represents bookable services
/// </summary>
public class Service : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public int Duration { get; set; } // Compatibility alias
    public int BufferBeforeMinutes { get; set; } = 0;
    public int BufferAfterMinutes { get; set; } = 0;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxAttendees { get; set; } = 1;
    public bool RequiresPayment { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? CancellationPolicy { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();

    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<StaffService> StaffServices { get; set; } = new List<StaffService>();
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    [System.ComponentModel.DataAnnotations.Schema.InverseProperty("BundleService")]
    public virtual ICollection<ServiceBundleItem> BundleItems { get; set; } = new List<ServiceBundleItem>();
}

/// <summary>
/// Mapping for service bundles
/// </summary>
public class ServiceBundleItem : BaseEntity
{
    public Guid BundleServiceId { get; set; }
    public Guid ComponentServiceId { get; set; }
    public int Order { get; set; }

    public virtual Service? BundleService { get; set; }
    public virtual Service? ComponentService { get; set; }
}

/// <summary>
/// Upsell suggestions for services
/// </summary>
public class ServiceUpsell : BaseEntity
{
    public Guid MainServiceId { get; set; }
    public Guid UpsellServiceId { get; set; }
    public string? Pitch { get; set; }
    public decimal? DiscountedPrice { get; set; }

    public virtual Service? MainService { get; set; }
    public virtual Service? UpsellService { get; set; }
}

/// <summary>
/// Staff member entity
/// </summary>
public class StaffMember : TenantEntity
{
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Color { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BookingUrl { get; set; }
    public bool CalendarSyncEnabled { get; set; }
    public string? GoogleCalendarId { get; set; }
    public string? OutlookCalendarId { get; set; }
    public string Timezone { get; set; } = "UTC";

    // Professional Details
    public decimal HourlyRate { get; set; }
    public decimal BaseCommissionRate { get; set; }
    public CommissionType CommissionType { get; set; } = CommissionType.Percentage;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    public List<string> Tags { get; set; } = new();

    // Financials & Payouts
    public string? StripeConnectId { get; set; }
    public string? StripePayoutStatus { get; set; } // active, pending, restricted
    public bool PayoutsEnabled { get; set; }

    public Dictionary<string, object> Settings { get; set; } = new();

    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual User? User { get; set; }
    public virtual ICollection<StaffService> StaffServices { get; set; } = new List<StaffService>();
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

/// <summary>
/// Staff-Service mapping
/// </summary>
public class StaffService : BaseEntity
{
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }
    public decimal? CustomPrice { get; set; }
    public int? CustomDuration { get; set; }

    public virtual StaffMember? Staff { get; set; }
    public virtual Service? Service { get; set; }
}
