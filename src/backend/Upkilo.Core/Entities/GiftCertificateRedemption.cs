namespace Upkilo.Core.Entities;

public class GiftCertificateRedemption : BaseEntity
{
    public Guid GiftCertificateId { get; set; }
    public GiftCertificate GiftCertificate { get; set; } = null!;

    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public decimal AmountRedeemed { get; set; }
    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
