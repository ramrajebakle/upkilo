namespace Upkilo.Core.Entities;

public enum GiftCertificateStatus
{
    Active,
    PartiallyRedeemed,
    FullyRedeemed,
    Void,
    Expired
}

public class GiftCertificate : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public decimal InitialAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? ExpiryDate { get; set; }
    public GiftCertificateStatus Status { get; set; } = GiftCertificateStatus.Active;

    // Optional contact info
    public string? RecipientEmail { get; set; }
    public string? SenderName { get; set; }
    public string? Message { get; set; }

    // Link to purchaser if applicable
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }

    public ICollection<GiftCertificateRedemption> Redemptions { get; set; } = new List<GiftCertificateRedemption>();
}
