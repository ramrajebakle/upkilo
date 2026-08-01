using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Services;
using Upkilo.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace Upkilo.API.Jobs;

/// <summary>
/// Monitors unresolved High/Critical escalations and sends reminders at 24h, 72h, and 168h milestones.
/// </summary>
public class EscalationFollowupJob
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EscalationFollowupJob> _logger;

    public EscalationFollowupJob(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<EscalationFollowupJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting EscalationFollowupJob at {Time}", DateTime.UtcNow);

        var now = DateTime.UtcNow;

        var pendingEscalations = await _context.AIEscalations
            .Where(e => !e.IsResolved &&
                        (e.Severity == "High" || e.Severity == "Critical") &&
                        e.CreatedAt < now.AddHours(-24))
            .ToListAsync();

        foreach (var esc in pendingEscalations)
        {
            try
            {
                var ageHours = (now - esc.CreatedAt).TotalHours;

                // Only remind at 24h, 72h, and 168h windows to avoid spamming
                bool shouldRemind =
                    (ageHours >= 24 && ageHours < 48) ||
                    (ageHours >= 72 && ageHours < 96) ||
                    (ageHours >= 168 && ageHours < 192);

                if (!shouldRemind) continue;

                _logger.LogInformation(
                    "Sending reminder for escalation {EscalationId} (age: {Age:F1}h)", esc.Id, ageHours);

                await _notificationService.SendReminderAsync(esc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder for escalation {EscalationId}", esc.Id);
            }
        }

        _logger.LogInformation("Finished EscalationFollowupJob at {Time}", DateTime.UtcNow);
    }
}
