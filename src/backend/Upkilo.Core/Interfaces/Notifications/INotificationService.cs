using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Service for sending real-time notifications via SignalR and multi-channel communication
/// </summary>
public interface INotificationService
{
    // User-specific notifications
    Task SendToUserAsync(string userId, string method, object notification);
    Task SendToastAsync(string userId, string title, string? message = null, string type = "info");
    
    // Tenant-wide notifications
    Task SendToTenantAsync(string tenantId, string method, object notification);

    // Multi-Channel Communication
    Task SendEmailAsync(string to, string subject, string content);
    Task<bool> SendSmsAsync(Guid tenantId, string to, string message, Guid? clientId = null);

    // Human-in-the-loop Escalation
    Task EscalateAsync(Guid tenantId, string module, string reason, string severity = "High", object? metadata = null, bool requiresApproval = true);
    Task SendReminderAsync(AIEscalation escalation);
}
