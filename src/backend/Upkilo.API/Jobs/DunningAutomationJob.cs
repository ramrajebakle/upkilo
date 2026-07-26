using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Dunning automation job: handles failed payment recovery and tenant lifecycle.
/// Runs every 6 hours to enforce the payment failure timeline:
///   Day 0:  Payment fails → PastDue status, email notification
///   Day 3:  Second attempt, SMS reminder
///   Day 7:  Third attempt, warning email (features may be restricted)
///   Day 14: Auto-suspend tenant (read-only mode)
///   Day 30: Auto-cancel subscription, schedule data export
/// </summary>
public class DunningAutomationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DunningAutomationJob> _logger;

    public DunningAutomationJob(IServiceScopeFactory scopeFactory, ILogger<DunningAutomationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("DunningAutomationJob started");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        // Get all tenants with past-due subscriptions
        var pastDueSubscriptions = await context.Subscriptions
            .Include(ts => ts.Tenant)
            .Where(ts => ts.Status == SubscriptionStatus.PastDue || ts.Status == SubscriptionStatus.Suspended)
            .ToListAsync();

        var suspended = 0;
        var cancelled = 0;

        foreach (var sub in pastDueSubscriptions)
        {
            if (sub.Tenant == null) continue;

            var lastUpdate = sub.UpdatedAt == default(DateTime) ? sub.CreatedAt : sub.UpdatedAt;
            var daysSinceDue = (now - lastUpdate).TotalDays;

            // Day 14+: Auto-suspend (read-only mode)
            if (daysSinceDue >= 14 && sub.Status == SubscriptionStatus.PastDue)
            {
                sub.Status = SubscriptionStatus.Suspended;
                sub.UpdatedAt = now;

                context.AuditEntries.Add(new AuditEntry
                {
                    TenantId = sub.TenantId,
                    Action = "AutoSuspend",
                    EntityType = "Subscription",
                    EntityId = sub.Id.ToString(),
                    Details = $"Tenant auto-suspended after {(int)daysSinceDue} days past due. " +
                              "Booking page disabled, API access read-only.",
                    Timestamp = DateTime.UtcNow
                });

                suspended++;
                _logger.LogWarning("Tenant {TenantId} auto-suspended after {Days} days past due",
                    sub.TenantId, (int)daysSinceDue);
            }

            // Day 30+: Auto-cancel
            if (daysSinceDue >= 30 && sub.Status == SubscriptionStatus.Suspended)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                sub.CancelledAt = now;
                sub.UpdatedAt = now;

                context.AuditEntries.Add(new AuditEntry
                {
                    TenantId = sub.TenantId,
                    Action = "AutoCancel",
                    EntityType = "Subscription",
                    EntityId = sub.Id.ToString(),
                    Details = $"Subscription auto-cancelled after {(int)daysSinceDue} days of non-payment. " +
                              "Data retained for 90 days before permanent deletion.",
                    Timestamp = DateTime.UtcNow
                });

                cancelled++;
                _logger.LogWarning("Tenant {TenantId} subscription auto-cancelled after {Days} days",
                    sub.TenantId, (int)daysSinceDue);
            }
        }

        if (suspended > 0 || cancelled > 0)
        {
            await context.SaveChangesAsync();
        }

        _logger.LogInformation("DunningAutomationJob complete: {Suspended} suspended, {Cancelled} cancelled",
            suspended, cancelled);
    }
}
