namespace Upkilo.Core.Events;

/// <summary>
/// Catalog of all webhook event types that tenants can subscribe to.
/// Each event corresponds to a domain action that can trigger outbound webhooks.
/// 
/// Naming convention: {entity}.{action}
/// Example: booking.created, client.updated, payment.received
/// </summary>
public static class WebhookEventCatalog
{
    // ── Booking Events (8) ──────────────────────────────
    public const string BookingCreated = "booking.created";
    public const string BookingConfirmed = "booking.confirmed";
    public const string BookingCancelled = "booking.cancelled";
    public const string BookingRescheduled = "booking.rescheduled";
    public const string BookingCompleted = "booking.completed";
    public const string BookingNoShow = "booking.no_show";
    public const string BookingCheckedIn = "booking.checked_in";
    public const string BookingReminder = "booking.reminder";

    // ── Client Events (5) ───────────────────────────────
    public const string ClientCreated = "client.created";
    public const string ClientUpdated = "client.updated";
    public const string ClientDeleted = "client.deleted";
    public const string ClientImported = "client.imported";
    public const string ClientMerged = "client.merged";

    // ── Payment Events (5) ──────────────────────────────
    public const string PaymentReceived = "payment.received";
    public const string PaymentFailed = "payment.failed";
    public const string PaymentRefunded = "payment.refunded";
    public const string InvoiceCreated = "invoice.created";
    public const string InvoicePaid = "invoice.paid";

    // ── Staff Events (4) ────────────────────────────────
    public const string StaffCreated = "staff.created";
    public const string StaffScheduleChanged = "staff.schedule_changed";
    public const string StaffBreakStarted = "staff.break_started";
    public const string StaffBreakEnded = "staff.break_ended";

    // ── Service Events (3) ──────────────────────────────
    public const string ServiceCreated = "service.created";
    public const string ServiceUpdated = "service.updated";
    public const string ServiceDeactivated = "service.deactivated";

    // ── Subscription Events (4) ─────────────────────────
    public const string SubscriptionCreated = "subscription.created";
    public const string SubscriptionUpgraded = "subscription.upgraded";
    public const string SubscriptionCancelled = "subscription.cancelled";
    public const string SubscriptionPastDue = "subscription.past_due";

    // ── Review Events (2) ───────────────────────────────
    public const string ReviewReceived = "review.received";
    public const string ReviewResponseSent = "review.response_sent";

    // ── Form Events (2) ─────────────────────────────────
    public const string FormSubmitted = "form.submitted";
    public const string WaiverSigned = "waiver.signed";

    /// <summary>
    /// All available webhook events for subscription.
    /// </summary>
    public static readonly IReadOnlyList<WebhookEventInfo> AllEvents = new List<WebhookEventInfo>
    {
        // Booking
        new(BookingCreated, "Booking Created", "Fired when a new booking is created"),
        new(BookingConfirmed, "Booking Confirmed", "Fired when a booking is confirmed"),
        new(BookingCancelled, "Booking Cancelled", "Fired when a booking is cancelled"),
        new(BookingRescheduled, "Booking Rescheduled", "Fired when a booking time is changed"),
        new(BookingCompleted, "Booking Completed", "Fired when an appointment finishes"),
        new(BookingNoShow, "No-Show", "Fired when a client doesn't show up"),
        new(BookingCheckedIn, "Client Checked In", "Fired when a client arrives"),
        new(BookingReminder, "Reminder Sent", "Fired when a booking reminder is sent"),

        // Client
        new(ClientCreated, "Client Created", "Fired when a new client is added"),
        new(ClientUpdated, "Client Updated", "Fired when client info changes"),
        new(ClientDeleted, "Client Deleted", "Fired when a client is removed"),
        new(ClientImported, "Client Imported", "Fired when clients are bulk-imported"),
        new(ClientMerged, "Clients Merged", "Fired when duplicate clients are merged"),

        // Payment
        new(PaymentReceived, "Payment Received", "Fired when a payment succeeds"),
        new(PaymentFailed, "Payment Failed", "Fired when a payment fails"),
        new(PaymentRefunded, "Refund Issued", "Fired when a refund is processed"),
        new(InvoiceCreated, "Invoice Created", "Fired when an invoice is generated"),
        new(InvoicePaid, "Invoice Paid", "Fired when an invoice is paid"),

        // Staff
        new(StaffCreated, "Staff Added", "Fired when a new staff member joins"),
        new(StaffScheduleChanged, "Schedule Changed", "Fired when staff schedule updates"),
        new(StaffBreakStarted, "Break Started", "Fired when a staff break begins"),
        new(StaffBreakEnded, "Break Ended", "Fired when a staff break ends"),

        // Service
        new(ServiceCreated, "Service Created", "Fired when a new service is added"),
        new(ServiceUpdated, "Service Updated", "Fired when service details change"),
        new(ServiceDeactivated, "Service Deactivated", "Fired when a service is turned off"),

        // Subscription
        new(SubscriptionCreated, "Subscription Started", "Fired when a new subscription begins"),
        new(SubscriptionUpgraded, "Plan Changed", "Fired when a subscription plan changes"),
        new(SubscriptionCancelled, "Subscription Cancelled", "Fired when a subscription is cancelled"),
        new(SubscriptionPastDue, "Payment Past Due", "Fired when subscription payment fails"),

        // Review
        new(ReviewReceived, "Review Received", "Fired when a client leaves a review"),
        new(ReviewResponseSent, "Review Response", "Fired when business responds to a review"),

        // Forms
        new(FormSubmitted, "Form Submitted", "Fired when a client submits a form"),
        new(WaiverSigned, "Waiver Signed", "Fired when a client signs a waiver"),
    };
}

/// <summary>
/// Metadata for a webhook event type.
/// </summary>
public record WebhookEventInfo(string EventType, string DisplayName, string Description);
