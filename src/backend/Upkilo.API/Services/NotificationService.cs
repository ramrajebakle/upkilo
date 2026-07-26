using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Hubs;
using Upkilo.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Upkilo.API.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly Upkilo.Infrastructure.Data.AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        IEmailService emailService,
        ISmsService smsService,
        Upkilo.Infrastructure.Data.AppDbContext context,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _emailService = emailService;
        _smsService = smsService;
        _context = context;
        _logger = logger;
    }

    public async Task SendToUserAsync(string userId, string method, object notification)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}").SystemNotification(
                new SystemNotification(
                    Id: Guid.NewGuid().ToString(),
                    Title: "Notification",
                    Message: notification.ToString() ?? "",
                    Type: "info",
                    Timestamp: DateTime.UtcNow,
                    IsUrgent: false
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
        }
    }

    public async Task SendToastAsync(string userId, string title, string? message = null, string type = "info")
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}").ToastMessage(
                new ToastNotification(title, message, type)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send toast to user {UserId}", userId);
        }
    }

    public async Task SendToTenantAsync(string tenantId, string method, object notification)
    {
        try
        {
            await _hubContext.Clients.Group($"tenant_{tenantId}").SystemNotification(
                new SystemNotification(
                    Id: Guid.NewGuid().ToString(),
                    Title: "Notification",
                    Message: notification.ToString() ?? "",
                    Type: "info",
                    Timestamp: DateTime.UtcNow,
                    IsUrgent: false
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to tenant {TenantId}", tenantId);
        }
    }

    public async Task BroadcastDashboardUpdateAsync(string tenantId, DashboardStats stats)
    {
        try
        {
            await _hubContext.Clients.Group($"dashboard_{tenantId}").DashboardStatsUpdated(stats);
            _logger.LogDebug("Dashboard update sent to tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast dashboard update to tenant {TenantId}", tenantId);
        }
    }

    public async Task NotifyBookingCreatedAsync(string tenantId, BookingNotification notification)
    {
        try
        {
            await _hubContext.Clients.Group($"tenant_{tenantId}").BookingCreated(notification);
            _logger.LogDebug("Booking created notification sent for booking {BookingId}", notification.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking created notification");
        }
    }

    public async Task NotifyBookingUpdatedAsync(string tenantId, BookingNotification notification)
    {
        try
        {
            await _hubContext.Clients.Group($"tenant_{tenantId}").BookingUpdated(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking updated notification");
        }
    }

    public async Task NotifyBookingCancelledAsync(string tenantId, BookingNotification notification)
    {
        try
        {
            await _hubContext.Clients.Group($"tenant_{tenantId}").BookingCancelled(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking cancelled notification");
        }
    }

    public async Task NotifyAvailabilityChangedAsync(string staffId, AvailabilityUpdate update)
    {
        try
        {
            await _hubContext.Clients.Group($"staff_calendar_{staffId}").AvailabilityChanged(update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send availability update for staff {StaffId}", staffId);
        }
    }

    public async Task NotifyScheduleUpdatedAsync(string staffId, ScheduleUpdate update)
    {
        try
        {
            await _hubContext.Clients.Group($"staff_calendar_{staffId}").StaffScheduleUpdated(update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send schedule update for staff {StaffId}", staffId);
        }
    }

    public async Task SendEmailAsync(string to, string subject, string content)
    {
        try
        {
            await _emailService.SendSystemEmailAsync(to, subject, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    public async Task<bool> SendSmsAsync(Guid tenantId, string to, string message, Guid? clientId = null)
    {
        try
        {
            var result = await _smsService.SendSmsAsync(tenantId, to, message, clientId);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To}", to);
            return false;
        }
    }

    public async Task EscalateAsync(Guid tenantId, string module, string reason, string severity = "High", object? metadata = null, bool requiresApproval = true)
    {
        try
        {
            var notification = new EscalationNotification(
                Id: Guid.NewGuid().ToString(),
                TenantId: tenantId.ToString(),
                Module: module,
                Reason: reason,
                Severity: severity,
                Metadata: metadata,
                Timestamp: DateTime.UtcNow,
                RequiresApproval: requiresApproval
            );

            // Notify all admins of the tenant
            await _hubContext.Clients.Group($"tenant_{tenantId}").SystemEscalation(notification);
            
            // --- PERSIST TO DATABASE ---
            var dbEscalation = new Upkilo.Core.Entities.AIEscalation
            {
                Id = Guid.Parse(notification.Id),
                TenantId = tenantId,
                Module = module,
                Reason = reason,
                Severity = severity,
                MetadataJson = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null,
                RequiresApproval = requiresApproval,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<Upkilo.Core.Entities.AIEscalation>().Add(dbEscalation);
            await _context.SaveChangesAsync();
            
            _logger.LogWarning("System escalation triggered and persisted for tenant {TenantId}: {Reason} ({Module}, Severity: {Severity})",
                tenantId, reason, module, severity);
            
            // Also send email & SMS for High/Critical severity
            if (severity == "High" || severity == "Critical")
            {
                var admins = await _context.Set<Upkilo.Core.Entities.User>()
                    .Where(u => u.TenantId == tenantId && 
                               (u.Role == Upkilo.Core.Entities.UserRole.Admin || u.Role == Upkilo.Core.Entities.UserRole.Owner) &&
                               u.IsActive)
                    .ToListAsync();

                foreach (var admin in admins)
                {
                    // 1. Send Email
                    string emailSubject = $"[URGENT] System Escalation: {module} - {severity}";
                    string billingCta = module == "Billing" 
                        ? "<div style='margin-top: 20px;'><a href='https://app.upkilo.com/settings/billing' style='background-color: #4f46e5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold;'>Resolve in Billing Dashboard</a></div>"
                        : "";

                    string emailBody = $@"
                        <div style='font-family: sans-serif; max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px;'>
                            <h2 style='color: #1e293b;'>System Escalation Triggered</h2>
                            <p><strong>Module:</strong> {module}</p>
                            <p><strong>Reason:</strong> {reason}</p>
                            <p><strong>Severity:</strong> <span style='color: {(severity == "Critical" ? "#ef4444" : "#f59e0b")}'>{severity}</span></p>
                            <p><strong>Time:</strong> {DateTime.UtcNow:u}</p>
                            <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 20px 0;'/>
                            <p style='color: #64748b;'>Please review this event immediately. High-severity alerts require manual intervention to prevent service disruption.</p>
                            {billingCta}
                        </div>";
                    
                    await SendEmailAsync(admin.Email, emailSubject, emailBody);

                    // 2. Send SMS if phone exists
                    string? phone = admin.PhoneNumber ?? admin.Phone;
                    if (!string.IsNullOrEmpty(phone))
                    {
                        string smsMessage = module == "Billing"
                            ? $"[Upkilo URGENT] {module} Alert: {reason}. Top-up here: https://app.upkilo.com/settings/billing"
                            : $"[Upkilo URGENT] {module} Alert ({severity}): {reason}. Review required in-dashboard.";
                            
                        await SendSmsAsync(tenantId, phone, smsMessage);
                    }
                }

                _logger.LogInformation("Escalation notifications sent to {Count} admins for tenant {TenantId}", admins.Count, tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger escalation for tenant {TenantId}", tenantId);
        }
    }

    public async Task SendReminderAsync(Upkilo.Core.Entities.AIEscalation escalation)
    {
        var admins = await _context.Set<Upkilo.Core.Entities.User>()
            .Where(u => u.TenantId == escalation.TenantId &&
                       (u.Role == Upkilo.Core.Entities.UserRole.Admin || u.Role == Upkilo.Core.Entities.UserRole.Owner) &&
                       u.IsActive)
            .ToListAsync();

        var ageHours = (DateTime.UtcNow - escalation.CreatedAt).TotalHours;
        var prefix   = ageHours >= 72 ? "[2nd REMINDER]" : "[REMINDER]";

        foreach (var admin in admins)
        {
            var subject = $"{prefix} Action Required: {escalation.Module} Escalation";
            var body = $@"
                <div style='font-family:sans-serif;max-width:600px;margin:auto;border:1px solid #cbd5e1;border-radius:12px;padding:24px;background:#f8fafc'>
                    <h2 style='color:#0f172a'>Escalation Reminder</h2>
                    <p>This is a follow-up regarding an unresolved <strong>{escalation.Severity}</strong> event in <strong>{escalation.Module}</strong>.</p>
                    <div style='background:white;padding:16px;border-radius:8px;border:1px solid #e2e8f0;margin:20px 0'>
                        <p><strong>Reason:</strong> {escalation.Reason}</p>
                        <p><strong>Opened:</strong> {escalation.CreatedAt:f}</p>
                    </div>
                    <a href='https://app.upkilo.com/settings/ai-approval'
                       style='background:#4f46e5;color:white;padding:12px 24px;text-decoration:none;border-radius:8px;font-weight:bold'>
                       Review &amp; Resolve
                    </a>
                </div>";

            await SendEmailAsync(admin.Email, subject, body);

            var phone = admin.PhoneNumber ?? admin.Phone;
            if (!string.IsNullOrEmpty(phone))
            {
                await SendSmsAsync(escalation.TenantId, phone,
                    $"[Upkilo {prefix}] Unresolved {escalation.Module} alert: {escalation.Reason}. Action required.");
            }
        }

        _logger.LogInformation(
            "Reminder sent for escalation {EscalationId} to {Count} admins (age: {Age:F1}h)",
            escalation.Id, admins.Count, ageHours);
    }
}
