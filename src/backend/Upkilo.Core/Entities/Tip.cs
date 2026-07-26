namespace Upkilo.Core.Entities;

/// <summary>
/// Tip/gratuity record for a booking or staff member.
/// Supports both flat and percentage tips with distribution tracking.
/// </summary>
public class Tip : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid BookingId { get; set; }
    public Guid StaffId { get; set; }
    public Guid? ClientId { get; set; }
    public decimal Amount { get; set; }
    public TipType Type { get; set; } = TipType.Flat;
    public decimal? Percentage { get; set; }      // If Type == Percentage, the % applied
    public string PaymentMethod { get; set; } = "card"; // card, cash, digital
    public bool IsDistributed { get; set; }       // Has been paid out to staff
    public DateTime? DistributedAt { get; set; }
    public string? StripePaymentIntentId { get; set; }

    // Navigation
    public Booking? Booking { get; set; }
    public StaffMember? Staff { get; set; }
}

public enum TipType
{
    Flat,
    Percentage
}
