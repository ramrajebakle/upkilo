using MediatR;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Events;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Background;

public class SecurityAlertNotification : INotification
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string? IpAddress { get; set; }
    public string? Description { get; set; }
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
}

public class SecurityEventHandlers : INotificationHandler<SecurityAlertNotification>
{
    private readonly AppDbContext _context;
    private readonly Upkilo.Core.Interfaces.IEmailService _emailService;
    private readonly ILogger<SecurityEventHandlers> _logger;

    public SecurityEventHandlers(
        AppDbContext context,
        Upkilo.Core.Interfaces.IEmailService emailService,
        ILogger<SecurityEventHandlers> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(SecurityAlertNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Severity != "High" && notification.Severity != "Critical")
            return;

        // Find tenant owner
        var tenant = await _context.Set<Upkilo.Core.Entities.Tenant>().FindAsync(new object[] { notification.TenantId }, cancellationToken);
        if (tenant == null || string.IsNullOrEmpty(tenant.Email))
        {
            _logger.LogWarning("Cannot send security alert for tenant {TenantId}. No owner email found.", notification.TenantId);
            return;
        }

        var subject = $"[Upkilo Security Alert] {notification.Severity} Event: {notification.EventType}";
        var body = $@"
            <h2>Security Alert</h2>
            <p><strong>Type:</strong> {notification.EventType}</p>
            <p><strong>Severity:</strong> {notification.Severity}</p>
            <p><strong>IP Address:</strong> {notification.IpAddress ?? "Unknown"}</p>
            <p><strong>Description:</strong> {notification.Description}</p>
            <p><strong>Time:</strong> {notification.OccurredOn:yyyy-MM-dd HH:mm:ss UTC}</p>
            <hr/>
            <p>Please review your tenant security logs immediately.</p>
        ";

        try
        {
            await _emailService.SendSystemEmailAsync(
                tenant.Email,
                subject,
                body
            );
            _logger.LogInformation("Sent security alert to {Email} for event {EventType}", tenant.Email, notification.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send security alert to {Email}", tenant.Email);
        }
    }
}
