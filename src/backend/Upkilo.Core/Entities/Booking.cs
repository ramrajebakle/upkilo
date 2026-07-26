using Upkilo.Core.Events;

namespace Upkilo.Core.Entities;


/// <summary>
/// Booking entity - appointments/reservations
/// </summary>
public class Booking : TenantEntity
{
    public Guid? ClientId { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? ServiceId { get; set; }
    public Guid? LocationId { get; set; }
    
    // Denormalized customer info for guests and search
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerId { get; set; } // Can store guest guid or client guid as string
    
    public string? ServiceName { get; set; }
    public string? StaffName { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime BookingDate => StartTime.Date;
    public string? Timezone { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public decimal? Price { get; set; }
    public decimal DepositPaid { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public bool ReminderSent { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public BookingSource Source { get; set; } = BookingSource.Manual;
    public string? ExternalId { get; set; }
    public bool IsWalkIn { get; set; }
    public DateTime? CheckedInAt { get; set; }

    // Review automation — set when post-appointment review request SMS is fired
    public DateTime? ReviewRequestSentAt { get; set; }

    // Tracking edits
    public int RescheduleCount { get; set; } = 0;

    // Group booking
    public int GroupSize { get; set; } = 1;
    public decimal? PerParticipantPrice { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();

    // Recurrence
    public Guid? RecurringPatternId { get; set; }
    public virtual RecurringPattern? RecurringPattern { get; set; }

    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual Client? Client { get; set; }
    public virtual StaffMember? Staff { get; set; }
    public virtual Service? Service { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public void MarkAsCancelled(string reason, Guid? cancelledBy = null)
    {
        Status = BookingStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        CancelledBy = cancelledBy;
        
        AddDomainEvent(new BookingCancelled 
        { 
            TenantId = TenantId, 
            BookingId = Id, 
            ClientId = ClientId ?? Guid.Empty,
            CancellationReason = reason 
        });
    }

    public void MarkAsCompleted()
    {
        Status = BookingStatus.Completed;
        
        AddDomainEvent(new BookingCompleted 
        { 
            TenantId = TenantId, 
            BookingId = Id, 
            ClientId = ClientId ?? Guid.Empty,
            StaffId = StaffId ?? Guid.Empty,
            FinalPrice = Price ?? 0
        });
    }
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    InProgress,
    Cancelled,
    Completed,
    NoShow
}

public enum BookingSource
{
    Manual,
    Website,
    Api,
    Chatbot,
    Widget,
    Import,
    Marketplace  // Booking originated from the Upkilo public Discover marketplace; 10% commission applies
}

/// <summary>
/// Payment entity
/// </summary>
public class Payment : TenantEntity
{
    public Guid? BookingId { get; set; }
    public Guid? ClientId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeChargeId { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal TipAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual Booking? Booking { get; set; }
    public virtual Client? Client { get; set; }

    public void MarkAsSucceeded()
    {
        Status = PaymentStatus.Succeeded;
        
        AddDomainEvent(new PaymentReceived 
        { 
            TenantId = TenantId, 
            PaymentId = Id, 
            BookingId = BookingId, 
            ClientId = ClientId, 
            Amount = Amount, 
            Currency = Currency, 
            PaymentMethod = PaymentMethod ?? "unknown" 
        });
    }
}

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded,
    Partial,
    Disputed
}
