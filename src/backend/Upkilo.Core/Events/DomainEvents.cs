using MediatR;

namespace Upkilo.Core.Events;

/// <summary>
/// Base class for all domain events in the Upkilo platform.
/// Domain events represent something meaningful that happened in the business domain.
/// Published via outbox pattern through OutboxProcessor for guaranteed delivery.
/// </summary>
public abstract class DomainEvent : INotification
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    public string EventType => GetType().Name;
    public int Version { get; set; } = 1;
}

// ── Booking Events ──────────────────────────────────────────

public class BookingCreated : DomainEvent
{
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid StaffId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal Price { get; set; }
    public bool IsWalkIn { get; set; }
}

public class BookingConfirmed : DomainEvent
{
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public string? ConfirmationCode { get; set; }
}

public class BookingCancelled : DomainEvent
{
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public string? CancellationReason { get; set; }
    public bool ByClient { get; set; }
    public decimal? RefundAmount { get; set; }
}

public class BookingCompleted : DomainEvent
{
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public Guid StaffId { get; set; }
    public decimal FinalPrice { get; set; }
    public int DurationMinutes { get; set; }
}

public class BookingNoShow : DomainEvent
{
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public decimal? NoShowFee { get; set; }
}

public class BookingRescheduled : DomainEvent
{
    public Guid BookingId { get; set; }
    public DateTime OldStartTime { get; set; }
    public DateTime NewStartTime { get; set; }
    public DateTime OldEndTime { get; set; }
    public DateTime NewEndTime { get; set; }
}

// ── Client Events ───────────────────────────────────────────

public class ClientCreated : DomainEvent
{
    public Guid ClientId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Source { get; set; } // walk-in, online, referral, import
}

public class ClientUpdated : DomainEvent
{
    public Guid ClientId { get; set; }
    public List<string> ChangedFields { get; set; } = new();
}

// ── Payment Events ──────────────────────────────────────────

public class PaymentReceived : DomainEvent
{
    public Guid PaymentId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ClientId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = "card";
    public string? StripePaymentIntentId { get; set; }
}

public class PaymentFailed : DomainEvent
{
    public Guid? BookingId { get; set; }
    public Guid? ClientId { get; set; }
    public decimal Amount { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public string? StripeDeclineCode { get; set; }
}

public class RefundIssued : DomainEvent
{
    public Guid PaymentId { get; set; }
    public Guid? BookingId { get; set; }
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// ── Staff Events ────────────────────────────────────────────

public class StaffScheduleChanged : DomainEvent
{
    public Guid StaffId { get; set; }
    public string ChangeType { get; set; } = string.Empty; // "WorkingHours", "Break", "TimeOff"
    public DateTime EffectiveDate { get; set; }
}

// ── Subscription Events ─────────────────────────────────────

public class SubscriptionUpgraded : DomainEvent
{
    public Guid SubscriptionId { get; set; }
    public string FromPlan { get; set; } = string.Empty;
    public string ToPlan { get; set; } = string.Empty;
}

public class SubscriptionDowngraded : DomainEvent
{
    public Guid SubscriptionId { get; set; }
    public string FromPlan { get; set; } = string.Empty;
    public string ToPlan { get; set; } = string.Empty;
    public List<string> FeaturesRemoved { get; set; } = new();
}

public class SubscriptionCancelled : DomainEvent
{
    public Guid SubscriptionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
}
