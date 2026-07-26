using MediatR;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Events;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Background;

/// <summary>
/// Wrapper for MediatR to handle BookingRescheduled domain events.
/// </summary>
public class BookingRescheduledNotification : INotification
{
    public BookingRescheduled Event { get; }

    public BookingRescheduledNotification(BookingRescheduled evt) => Event = evt;
}

public class CalendarSyncBookingCreatedHandler : INotificationHandler<BookingCreatedNotification>
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<CalendarSyncBookingCreatedHandler> _logger;

    public CalendarSyncBookingCreatedHandler(
        ICalendarService calendarService,
        ILogger<CalendarSyncBookingCreatedHandler> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task Handle(BookingCreatedNotification notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        _logger.LogInformation("Syncing calendar for BookingCreated {BookingId}", evt.BookingId);
        
        try
        {
            await _calendarService.SyncBookingsAsync(evt.StaffId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync calendar after BookingCreated: {BookingId}", evt.BookingId);
        }
    }
}

public class CalendarSyncBookingCancelledHandler : INotificationHandler<BookingCancelledNotification>
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<CalendarSyncBookingCancelledHandler> _logger;

    public CalendarSyncBookingCancelledHandler(
        ICalendarService calendarService,
        ILogger<CalendarSyncBookingCancelledHandler> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task Handle(BookingCancelledNotification notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        _logger.LogInformation("Syncing calendar for BookingCancelled {BookingId}", evt.BookingId);

        try
        {
            // Note: BookingCancelled doesn't have StaffId directly, might need to decouple mapping
            // For now we assume SyncBookingsAsync updates all changes, 
            // In a real system, you'd fetch the Booking or add StaffId to BookingCancelled event.
            // Using a placeholder log until event is augmented, or rely on a Global Sync queue.
            _logger.LogWarning("StaffId is not present on BookingCancelled event. Requires lookup for immediate sync.");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync calendar after BookingCancelled: {BookingId}", evt.BookingId);
        }
    }
}

public class CalendarSyncBookingRescheduledHandler : INotificationHandler<BookingRescheduledNotification>
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<CalendarSyncBookingRescheduledHandler> _logger;

    public CalendarSyncBookingRescheduledHandler(
        ICalendarService calendarService,
        ILogger<CalendarSyncBookingRescheduledHandler> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task Handle(BookingRescheduledNotification notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        _logger.LogInformation("Syncing calendar for BookingRescheduled {BookingId}", evt.BookingId);

        try
        {
             _logger.LogWarning("StaffId is not present on BookingRescheduled event. Requires lookup for immediate sync.");
             await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync calendar after BookingRescheduled: {BookingId}", evt.BookingId);
        }
    }
}
