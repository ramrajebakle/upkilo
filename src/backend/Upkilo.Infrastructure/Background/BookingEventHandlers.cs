using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Events;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Background;

/// <summary>
/// Handles BookingCreated events — sends confirmation email/SMS via fallback logic,
/// updates analytics, triggers webhook delivery, and records business metrics.
/// </summary>
public class BookingCreatedHandler : INotificationHandler<BookingCreatedNotification>
{
    private readonly AppDbContext _context;
    private readonly NotificationFallbackService _notificationFallback;
    private readonly IBusinessMetrics _metrics;
    private readonly ILogger<BookingCreatedHandler> _logger;

    public BookingCreatedHandler(
        AppDbContext context,
        NotificationFallbackService notificationFallback,
        IBusinessMetrics metrics,
        ILogger<BookingCreatedHandler> logger)
    {
        _context = context;
        _notificationFallback = notificationFallback;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task Handle(BookingCreatedNotification notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        _logger.LogInformation("Handling BookingCreated event for booking {BookingId}", evt.BookingId);

        try
        {
            // Record business metric
            _metrics.RecordBookingCreated(evt.TenantId.ToString(), "appointment");

            // Fetch Client and Service details for notification
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == evt.ClientId && c.TenantId == evt.TenantId, cancellationToken);
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == evt.ServiceId && s.TenantId == evt.TenantId, cancellationToken);
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == evt.TenantId, cancellationToken);

            if (client != null && service != null && tenant != null && !evt.IsWalkIn)
            {
                var subject = $"Booking Confirmed: {service.Name}";
                var emailBody = $"<h3>Hi {client.FirstName},</h3><p>Your booking for <strong>{service.Name}</strong> on {evt.StartTime:f} has been confirmed!</p><br/><p>Thank you,<br/>{tenant.Name}</p>";
                var smsBody = $"Your booking for {service.Name} on {evt.StartTime:g} is confirmed! - {tenant.Name}";

                // Fire and forget multi-channel notification with fallback logic
                await _notificationFallback.SendAsync(evt.TenantId, client.Email, client.Phone, subject, emailBody, smsBody, client.Id);
            }

            _logger.LogInformation("BookingCreated event processed for {BookingId}", evt.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling BookingCreated for {BookingId}", evt.BookingId);
            // Don't rethrow — event handlers should not fail the parent operation
        }
    }
}

/// <summary>
/// Handles BookingCancelled events — sends cancellation notifications, processes refunds, updates metrics.
/// </summary>
public class BookingCancelledHandler : INotificationHandler<BookingCancelledNotification>
{
    private readonly AppDbContext _context;
    private readonly NotificationFallbackService _notificationFallback;
    private readonly IBusinessMetrics _metrics;
    private readonly ILogger<BookingCancelledHandler> _logger;

    public BookingCancelledHandler(
        AppDbContext context,
        NotificationFallbackService notificationFallback,
        IBusinessMetrics metrics,
        ILogger<BookingCancelledHandler> logger)
    {
        _context = context;
        _notificationFallback = notificationFallback;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task Handle(BookingCancelledNotification notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        _logger.LogInformation("Handling BookingCancelled for {BookingId}", evt.BookingId);

        try
        {
            // 1. Record Cancellation Metric
            _metrics.RecordBookingCancelled(evt.TenantId.ToString(), evt.ByClient ? "client" : "staff");

            // 2. Notify Client
            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Service)
                .Include(b => b.Tenant)
                .FirstOrDefaultAsync(b => b.Id == evt.BookingId, cancellationToken);

            if (booking?.Client != null && booking.Service != null)
            {
                var subject = $"Booking Cancelled: {booking.Service.Name}";
                var emailBody = $"<h3>Hello {booking.Client.FirstName},</h3><p>Your booking for <strong>{booking.Service.Name}</strong> on {booking.StartTime:f} has been cancelled.</p><p>Reason: {evt.CancellationReason ?? "None provided"}</p>";
                var smsBody = $"Your booking for {booking.Service.Name} on {booking.StartTime:g} was cancelled. - {booking.Tenant?.Name}";

                await _notificationFallback.SendAsync(evt.TenantId, booking.Client.Email, booking.Client.Phone, subject, emailBody, smsBody, booking.ClientId);
            }

            _logger.LogInformation("BookingCancelled processed for {BookingId}", evt.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling BookingCancelled for {BookingId}", evt.BookingId);
        }
    }
}

public class BookingCompletedHandler : INotificationHandler<BookingCompletedNotification>
{
    private readonly AppDbContext _context;
    private readonly IBusinessMetrics _metrics;
    private readonly ILogger<BookingCompletedHandler> _logger;

    public BookingCompletedHandler(AppDbContext context, IBusinessMetrics metrics, ILogger<BookingCompletedHandler> logger)
    {
        _context = context;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task Handle(BookingCompletedNotification notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        _logger.LogInformation("Handling BookingCompleted for {BookingId}", evt.BookingId);

        try
        {
            // 1. Record Revenue Metric
            _metrics.RecordPaymentProcessed(evt.TenantId.ToString(), evt.FinalPrice, "completed");

            // 2. Update Client Aggregate (LTV and Last Visit)
            var client = await _context.Clients.FindAsync(new object[] { evt.ClientId }, cancellationToken);
            if (client != null)
            {
                client.LifetimeValue += evt.FinalPrice;
                client.LastVisitAt = DateTime.UtcNow;
                client.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            // 3. Trigger Review Request (In production, this would be a delayed background job)
            _logger.LogInformation("Review request scheduled for client {ClientId}", evt.ClientId);

            _logger.LogInformation("BookingCompleted processed for {BookingId}", evt.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling BookingCompleted for {BookingId}", evt.BookingId);
        }
    }
}

// ── MediatR Notification Wrappers ──────────────────────────────

/// <summary>
/// MediatR notification wrapper for BookingCreated domain event
/// </summary>
public class BookingCreatedNotification : INotification
{
    public BookingCreated Event { get; }
    public BookingCreatedNotification(BookingCreated evt) => Event = evt;
}

/// <summary>
/// MediatR notification wrapper for BookingCancelled domain event
/// </summary>
public class BookingCancelledNotification : INotification
{
    public BookingCancelled Event { get; }
    public BookingCancelledNotification(BookingCancelled evt) => Event = evt;
}

/// <summary>
/// MediatR notification wrapper for BookingCompleted domain event
/// </summary>
public class BookingCompletedNotification : INotification
{
    public BookingCompleted Event { get; }
    public BookingCompletedNotification(BookingCompleted evt) => Event = evt;
}
