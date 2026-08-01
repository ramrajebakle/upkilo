using Upkilo.Core.Events;

namespace Upkilo.Core.Entities;


/// <summary>
/// Client entity - customer records
/// </summary>
public class Client : TenantEntity
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? AvatarUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public decimal LifetimeValue { get; set; }
    public int LeadScore { get; set; }
    public int TotalBookings { get; set; }
    public DateTime? LastBookingAt { get; set; }
    public DateTime? LastVisitAt { get; set; }
    public string? StripeCustomerId { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
    public bool MarketingConsent { get; set; }
    public bool SmsConsent { get; set; }
    public int LoyaltyPoints { get; set; }
    public string LoyaltyTier { get; set; } = "Bronze";
    public Guid? HouseholdId { get; set; }

    // Convenience aliases
    public string? PhoneNumber { get => Phone; set => Phone = value; }
    public string? Address { get => AddressLine1; set => AddressLine1 = value; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual Household? Household { get; set; }
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<ClientNote> DetailedNotes { get; set; } = new List<ClientNote>();
    public virtual ICollection<CommunicationLog> CommunicationLogs { get; set; } = new List<CommunicationLog>();

    public static Client Create(Guid tenantId, string email, string firstName, string lastName, string? source = null)
    {
        var client = new Client
        {
            TenantId = tenantId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Source = source,
            CreatedAt = DateTime.UtcNow
        };

        client.AddDomainEvent(new ClientCreated
        {
            TenantId = tenantId,
            ClientId = client.Id,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Source = source
        });

        return client;
    }
}

/// <summary>
/// Ledger for client loyalty points
/// </summary>
public class LoyaltyTransaction : TenantEntity
{
    public Guid ClientId { get; set; }
    public int Points { get; set; }
    public string? Description { get; set; }
    public LoyaltyTransactionType TransactionType { get; set; }

    public virtual Client? Client { get; set; }
}

public enum LoyaltyTransactionType
{
    Earned,
    Redeemed,
    Adjustment,
    ReferralBonus
}

/// <summary>
/// Client-to-Client referral tracking
/// </summary>
public class ClientReferral : TenantEntity
{
    public Guid ReferrerClientId { get; set; }
    public Guid? ReferredClientId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public ClientReferralStatus Status { get; set; } = ClientReferralStatus.Pending;
    public int RewardPoints { get; set; } = 100;
    public bool RewardIssued { get; set; }

    public virtual Client? Referrer { get; set; }
    public virtual Client? Referred { get; set; }
}

public enum ClientReferralStatus
{
    Pending,
    Completed,
    RewardIssued,
    Expired
}
