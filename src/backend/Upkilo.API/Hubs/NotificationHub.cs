using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Upkilo.API.Hubs;

/// <summary>
/// SignalR hub for real-time notifications across the application.
/// Handles live updates for bookings, staff schedules, and system notifications.
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Join user-specific group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            // Join tenant group for business-wide notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
        }

        _logger.LogInformation(
            "Client connected: {ConnectionId}, User: {UserId}, Tenant: {TenantId}",
            Context.ConnectionId, userId, tenantId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation(
            "Client disconnected: {ConnectionId}, User: {UserId}, Exception: {Exception}",
            Context.ConnectionId, userId, exception?.Message);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to real-time calendar updates for a specific staff member
    /// </summary>
    public async Task SubscribeToStaffCalendar(string staffId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"staff_calendar_{staffId}");
        _logger.LogDebug("Connection {ConnectionId} subscribed to staff calendar: {StaffId}",
            Context.ConnectionId, staffId);
    }

    /// <summary>
    /// Unsubscribe from staff calendar updates
    /// </summary>
    public async Task UnsubscribeFromStaffCalendar(string staffId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"staff_calendar_{staffId}");
    }

    /// <summary>
    /// Subscribe to live dashboard updates
    /// </summary>
    public async Task SubscribeToDashboard()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard_{tenantId}");
        }
    }

    /// <summary>
    /// Mark notification as read (client-to-server acknowledgment)
    /// </summary>
    public async Task MarkNotificationRead(string notificationId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogDebug("User {UserId} marked notification {NotificationId} as read",
            userId, notificationId);

        // In production, this would update the database
        // await _notificationService.MarkAsReadAsync(notificationId, userId);
    }

    /// <summary>
    /// Subscribe to system-wide announcements (maintenance, updates)
    /// </summary>
    public async Task SubscribeToSystemAnnouncements()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "system_announcements");
        _logger.LogDebug("Connection {ConnectionId} subscribed to system announcements", Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to subscription/billing updates
    /// </summary>
    public async Task SubscribeToBillingUpdates()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"billing_{tenantId}");
        }
    }
}

/// <summary>
/// Strongly-typed client interface for NotificationHub
/// </summary>
public interface INotificationClient
{
    // Booking notifications
    Task BookingCreated(BookingNotification notification);
    Task BookingUpdated(BookingNotification notification);
    Task BookingCancelled(BookingNotification notification);
    Task BookingReminder(BookingNotification notification);

    // Dashboard updates
    Task DashboardStatsUpdated(DashboardStats stats);
    Task NewClientArrival(ClientArrivalNotification notification);

    // System notifications
    Task SystemNotification(SystemNotification notification);
    Task ToastMessage(ToastNotification notification);
    Task SystemEscalation(EscalationNotification notification);

    // System announcements (maintenance, updates)
    Task MaintenanceAnnouncement(MaintenanceNotification notification);
    Task FeatureAnnouncement(FeatureAnnouncementNotification notification);

    // Billing/subscription updates
    Task UsageLimitWarning(UsageLimitNotification notification);
    Task SubscriptionUpdated(SubscriptionUpdateNotification notification);

    // Calendar updates
    Task AvailabilityChanged(AvailabilityUpdate update);
    Task StaffScheduleUpdated(ScheduleUpdate update);
}

#region Notification DTOs

public record BookingNotification(
    string BookingId,
    string ClientName,
    string ServiceName,
    string StaffName,
    DateTime StartTime,
    string Status,
    string Message
);

public record DashboardStats(
    int TodayBookings,
    decimal TodayRevenue,
    int NewClients,
    int PendingBookings
);

public record ClientArrivalNotification(
    string ClientId,
    string ClientName,
    string BookingId,
    string ServiceName,
    DateTime ArrivalTime
);

public record SystemNotification(
    string Id,
    string Title,
    string Message,
    string Type, // info, warning, error, success
    DateTime Timestamp,
    bool IsUrgent
);

public record ToastNotification(
    string Title,
    string? Message,
    string Type, // success, error, warning, info
    int DurationMs = 5000
);

public record AvailabilityUpdate(
    string StaffId,
    DateTime Date,
    List<TimeSlot> AvailableSlots
);

public record TimeSlot(
    string Time,
    bool Available
);

public record ScheduleUpdate(
    string StaffId,
    string StaffName,
    DateTime Date,
    string ChangeType, // added, modified, removed
    string? Details
);

public record MaintenanceNotification(
    string Id,
    string Title,
    string Message,
    DateTime ScheduledStart,
    DateTime? ScheduledEnd,
    bool IsEmergency
);

public record FeatureAnnouncementNotification(
    string Id,
    string Title,
    string Description,
    string? FeatureUrl,
    string? ImageUrl,
    DateTime Timestamp
);

public record UsageLimitNotification(
    string ResourceType, // bookings, sms, ai_credits, storage
    int CurrentUsage,
    int Limit,
    int PercentUsed,
    DateTime PeriodEnd,
    DateTime Timestamp,
    string? UpgradeUrl
);

public record EscalationNotification(
    string Id,
    string TenantId,
    string Module, // AI, Workflow, Security
    string Reason,
    string Severity, // Low, Medium, High, Critical
    object? Metadata,
    DateTime Timestamp,
    bool RequiresApproval
);

public record SubscriptionUpdateNotification(
    string ChangeType, // upgraded, downgraded, cancelled, renewed, payment_failed
    string? OldPlan,
    string? NewPlan,
    string Message,
    DateTime Timestamp
);

#endregion
