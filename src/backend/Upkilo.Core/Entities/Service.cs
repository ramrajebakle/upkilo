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

    /// <summary>
    /// Free-text policy shown to the client. Display only — it is prose and nothing can act on
    /// it. The three fields below are what the refund engine actually reads.
    /// </summary>
    public string? CancellationPolicy { get; set; }

    // ── Refund policy ────────────────────────────────────────────────────────────────────
    // Set per service, because how much notice a cancellation needs is a property of the work,
    // not of the business: a 15-minute consultation and a four-hour laser course cannot
    // reasonably share one window.
    //
    // Two thresholds produce three tiers, evaluated against hours remaining until StartTime:
    //   more than FullRefundHours          → refund 100%
    //   between Partial- and FullRefundHours → refund PartialRefundPercent
    //   less than PartialRefundHours       → refund nothing, deposit is kept
    //
    // Non-nullable with defaults rather than nullable-with-inheritance: a refund rule that
    // silently falls back to a value defined somewhere else is one a tenant cannot read off the
    // service they are looking at, and this decides whether a customer gets their money back.
    // Existing rows take these defaults through the migration.

    /// <summary>Cancel with more notice than this and the deposit is refunded in full.</summary>
    public int FullRefundHours { get; set; } = 18;

    /// <summary>Below this, no refund is given. Must be less than or equal to FullRefundHours.</summary>
    public int PartialRefundHours { get; set; } = 12;

    /// <summary>Percentage refunded when cancelling between the two thresholds. 0–100.</summary>
    public decimal PartialRefundPercent { get; set; } = 50m;

    // ── Rebooking ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// How many days after this service a client is typically due for it again. Null disables
    /// rebooking reminders for the service.
    ///
    /// Deliberately per service and vertical-neutral: a salon colour is due in ~42 days, botox in
    /// ~120, a physio review in ~7, a full detail in ~150. One number per service expresses all
    /// of them, where a single business-wide setting could express none of them.
    /// </summary>
    public int? RebookAfterDays { get; set; }

    // ── Mobile / travel ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// True when the service is performed at the client's location rather than at the business.
    /// Applies well beyond auto detailing — mobile massage, mobile physio and at-home beauty all
    /// have the same shape.
    /// </summary>
    public bool IsMobile { get; set; }

    /// <summary>
    /// Minutes to reserve either side of a mobile job for travel. A flat allowance rather than a
    /// distance calculation: routing needs a mapping provider and per-booking addresses, whereas
    /// most mobile operators already work to a service radius with a known typical drive time.
    /// Booking back-to-back mobile jobs with no travel gap is the failure this prevents.
    /// </summary>
    public int TravelBufferMinutes { get; set; }

    /// <summary>
    /// Buffer actually applied when scheduling: the configured turnaround plus travel time on
    /// mobile services. The scheduler uses these rather than the raw fields, so travel is
    /// enforced by the same conflict detection that already protects turnaround — no second code
    /// path that could disagree with the first.
    /// </summary>
    public int EffectiveBufferBeforeMinutes => BufferBeforeMinutes + (IsMobile ? TravelBufferMinutes : 0);

    /// <inheritdoc cref="EffectiveBufferBeforeMinutes"/>
    public int EffectiveBufferAfterMinutes => BufferAfterMinutes + (IsMobile ? TravelBufferMinutes : 0);

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
