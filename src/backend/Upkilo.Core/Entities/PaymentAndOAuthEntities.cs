namespace Upkilo.Core.Entities;

/// <summary>
/// Tenant-scoped credit account for storing account balance,
/// credits from refunds, referrals, and promotional offers.
/// </summary>
public class CreditAccount : TenantEntity
{
    public Guid? ClientId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "usd";
    public DateTime LastTransactionAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual credit transaction — tracks all credit movement.
/// </summary>
public class CreditAccountTransaction : TenantEntity
{
    public Guid CreditAccountId { get; set; }
    public CreditAccount? CreditAccount { get; set; }
    public string Type { get; set; } = "Credit"; // Credit, Debit, Refund, Referral, Promo, Adjustment
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; } // Booking, Invoice, Referral, Promo
    public Guid? ReferenceId { get; set; }
    public Guid? PerformedById { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Split payment / deposit configuration for a booking or invoice.
/// </summary>
public class SplitPayment : TenantEntity
{
    public Guid? BookingId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = "Pending"; // Pending, DepositPaid, FullyPaid, Refunded
    public string SplitType { get; set; } = "Deposit"; // Deposit, Installment, Custom
    public string? InstallmentSchedule { get; set; } // JSON: [{dueDate, amount, status}]
    public decimal DepositAmount { get; set; }
    public decimal DepositPercentage { get; set; } = 50; // Default 50% deposit
    public string? StripePaymentIntentId { get; set; }
    public DateTime? DepositPaidAt { get; set; }
    public DateTime? FullyPaidAt { get; set; }
}

/// <summary>
/// OAuth2 application registration for third-party developer access.
/// </summary>
public class OAuthApp : TenantEntity
{
    public string AppName { get; set; } = string.Empty;
    public string ClientId { get; set; } = Guid.NewGuid().ToString("N");
    public string ClientSecretHash { get; set; } = string.Empty; // Hashed
    public string? Description { get; set; }
    public string RedirectUris { get; set; } = "[]"; // JSON array of allowed redirect URIs
    public string Scopes { get; set; } = "[]"; // JSON array: read, write, bookings, clients, etc.
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? PrivacyPolicyUrl { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; } = true;
    public int RateLimitPerMinute { get; set; } = 60;
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// OAuth2 authorization code / access token for third-party apps.
/// </summary>
public class OAuthToken : TenantEntity
{
    public Guid OAuthAppId { get; set; }
    public OAuthApp? OAuthApp { get; set; }
    public Guid UserId { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public string AccessTokenHash { get; set; } = string.Empty;
    public string? RefreshTokenHash { get; set; }
    public string? AuthorizationCode { get; set; }
    public string Scopes { get; set; } = "[]"; // JSON array of granted scopes
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked => RevokedAt.HasValue;
}
