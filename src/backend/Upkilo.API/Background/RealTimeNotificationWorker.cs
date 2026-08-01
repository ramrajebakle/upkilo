using Microsoft.AspNetCore.SignalR;
using Upkilo.API.Hubs;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using System.Text.Json;

namespace Upkilo.API.Background;

public class RealTimeNotificationWorker : BackgroundService
{
    private readonly EventService _eventService;
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<RealTimeNotificationWorker> _logger;

    public RealTimeNotificationWorker(
        EventService eventService,
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<RealTimeNotificationWorker> logger)
    {
        _eventService = eventService;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RealTimeNotificationWorker started.");

        // Outer loop restarts the reader if the channel completes unexpectedly.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var evt in _eventService.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        if (evt.EventName == "scheduling.availability_changed")
                            await HandleAvailabilityChanged(evt);
                        else if (evt.EventName.StartsWith("booking."))
                            await HandleBookingEvent(evt);
                        else if (evt.EventName.StartsWith("staff."))
                            await HandleStaffEvent(evt);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error broadcasting event {EventName}", evt.EventName);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RealTimeNotificationWorker channel reader faulted. Restarting in 5s.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleStaffEvent(WorkflowEvent evt)
    {
        var json = JsonSerializer.Serialize(evt.Data);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("StaffId", out var staffIdProp)) return;
        var staffId = staffIdProp.GetGuid();

        _logger.LogInformation("Broadcasting staff activity: {EventName} for Staff {StaffId}", evt.EventName, staffId);

        var group = _hubContext.Clients.Group($"tenant_{evt.TenantId}");

        if (evt.EventName == "staff.schedule_updated" || evt.EventName == "staff.shift_updated")
        {
            await group.StaffScheduleUpdated(new ScheduleUpdate(
                staffId.ToString(),
                "Staff Member",
                DateTime.UtcNow,
                evt.EventName.Split('.')[1], // "schedule_updated" or "shift_updated"
                "Schedule changed"
            ));

            // Also broadcast to availability listeners
            await _hubContext.Clients.Group($"staff_calendar_{staffId}")
                .AvailabilityChanged(new AvailabilityUpdate(
                    staffId.ToString(),
                    DateTime.UtcNow.Date,
                    new List<TimeSlot>() // Forces refresh
                ));
        }
        else if (evt.EventName == "staff.clock_in_out")
        {
            var action = root.TryGetProperty("Action", out var actionProp) ? actionProp.GetString() : "clocked";
            await group.ToastMessage(new ToastNotification(
                "Staff Activity",
                $"Staff {staffId} {action}",
                "info"
            ));
        }
    }

    private async Task HandleBookingEvent(WorkflowEvent evt)
    {
        var json = JsonSerializer.Serialize(evt.Data);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Try to extract basic booking info for notifications
        string bookingId = root.TryGetProperty("Id", out var idProp) ? idProp.GetGuid().ToString() : Guid.Empty.ToString();
        string status = root.TryGetProperty("Status", out var statusProp) ? statusProp.ToString() : "unknown";

        // Construct notification message based on event type
        string message = evt.EventName switch
        {
            "booking.created" => "New booking received",
            "booking.updated" => "Booking was updated",
            "booking.completed" => "Booking marked as completed",
            "booking.cancelled" => "Booking was cancelled",
            "booking.walkin" => "New walk-in client",
            _ => $"Booking status: {status}"
        };

        var notification = new BookingNotification(
            bookingId,
            "Client", // Default if not found in root
            "Service", // Default if not found in root
            "Staff", // Default if not found in root
            DateTime.UtcNow, // Default
            status,
            message
        );

        // Try to get richer data if available (e.g. from Includes)
        try
        {
            if (root.TryGetProperty("Client", out var client))
                notification = notification with { ClientName = $"{client.GetProperty("FirstName")} {client.GetProperty("LastName")}" };

            if (root.TryGetProperty("Service", out var service))
                notification = notification with { ServiceName = service.GetProperty("Name").GetString() ?? "Service" };

            if (root.TryGetProperty("Staff", out var staff))
                notification = notification with { StaffName = $"{staff.GetProperty("FirstName")} {staff.GetProperty("LastName")}" };

            if (root.TryGetProperty("StartTime", out var startTime))
                notification = notification with { StartTime = startTime.GetDateTime() };
        }
        catch { /* Suppress mapping errors for extra fields */ }

        _logger.LogInformation("Broadcasting {EventName} for Tenant {TenantId}", evt.EventName, evt.TenantId);

        var group = _hubContext.Clients.Group($"tenant_{evt.TenantId}");

        Task? task = evt.EventName switch
        {
            "booking.created" => group.BookingCreated(notification),
            "booking.updated" => group.BookingUpdated(notification),
            "booking.completed" => group.BookingUpdated(notification),
            "booking.cancelled" => group.BookingCancelled(notification),
            "booking.walkin" => group.BookingCreated(notification),
            "booking.checked_in" => group.NewClientArrival(new ClientArrivalNotification(
                root.TryGetProperty("ClientId", out var cid) ? cid.GetGuid().ToString() : Guid.Empty.ToString(),
                notification.ClientName,
                bookingId,
                notification.ServiceName,
                DateTime.UtcNow
            )),
            _ => group.BookingUpdated(notification)
        };

        if (task != null) await task;

        // Also update dashboard stats if it's a creation/completion
        if (evt.EventName == "booking.created" || evt.EventName == "booking.completed" || evt.EventName == "booking.cancelled")
        {
            await group.DashboardStatsUpdated(new DashboardStats(0, 0, 0, 0)); // Forces re-fetch on client
        }
    }

    private async Task HandleAvailabilityChanged(WorkflowEvent evt)
    {
        // Event Data format from SchedulingService:
        // new { StaffId = staffId, Date = cache.Date, Timestamp = DateTime.UtcNow }

        // We use reflection or dynamic to get data because it's an anonymous object in the channel
        // For production, a shared DTO would be better, but let's parse from JSON for safety.
        var json = JsonSerializer.Serialize(evt.Data);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("StaffId", out var staffIdProp) &&
            root.TryGetProperty("Date", out var dateProp))
        {
            var staffId = staffIdProp.GetGuid();
            var date = dateProp.GetDateTime();

            _logger.LogDebug("Broadcasting availability change for Staff {StaffId} on {Date}", staffId, date);

            await _hubContext.Clients.Group($"staff_calendar_{staffId}")
                .AvailabilityChanged(new AvailabilityUpdate(
                    staffId.ToString(),
                    date,
                    new List<TimeSlot>() // Forces client re-fetch
                ));
        }
    }
}
