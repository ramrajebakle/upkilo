namespace Upkilo.Core.Entities;

/// <summary>
/// Credit transaction entity - tracks all credit additions and deductions
/// </summary>
public class CreditTransaction : TenantEntity
{
    public Guid ClientId { get; set; }
    public decimal Amount { get; set; } // Positive for credits, negative for debits
    public decimal BalanceAfter { get; set; }
    public CreditTransactionType Type { get; set; }
    public string? Description { get; set; }
    public string? ReferenceId { get; set; } // e.g., booking ID, payment ID
    public Guid? CreatedByUserId { get; set; }

    // Navigation
    public virtual Client? Client { get; set; }
}

public enum CreditTransactionType
{
    Purchase,       // Client bought credits
    GiftCard,       // Gift card redemption
    Refund,         // Refund issued as credit
    Booking,        // Used for a booking
    Adjustment,     // Manual adjustment
    Expiry,         // Credits expired
    Bonus,          // Promotional credits
    LoyaltyEarn,    // Points earned via loyalty
    LoyaltyRedeem   // Points redeemed for rewards
}
