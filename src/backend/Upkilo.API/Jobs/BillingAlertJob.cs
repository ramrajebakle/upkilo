using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace Upkilo.API.Jobs;

/// <summary>
/// Background job to check tenant resource usage and send alerts when thresholds are reached.
/// </summary>
public class BillingAlertJob
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BillingAlertJob> _logger;
    private readonly DbContext _dbContext;

    public BillingAlertJob(
        ISubscriptionService subscriptionService,
        INotificationService notificationService,
        ILogger<BillingAlertJob> logger,
        // Using DbContext directly to update LastAlertThreshold
        Upkilo.Infrastructure.Data.AppDbContext dbContext)
    {
        _subscriptionService = subscriptionService;
        _notificationService = notificationService;
        _logger = logger;
        _dbContext = dbContext;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting BillingAlertJob at {Time}", DateTime.UtcNow);

        var subscriptions = await _dbContext.Set<Subscription>()
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing)
            .ToListAsync();

        foreach (var sub in subscriptions)
        {
            try
            {
                await CheckAndAlertTenantAsync(sub);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing billing alerts for tenant {TenantId}", sub.TenantId);
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Finished BillingAlertJob at {Time}", DateTime.UtcNow);
    }

    private async Task CheckAndAlertTenantAsync(Subscription sub)
    {
        var usage = await _subscriptionService.GetUsageAsync(sub.TenantId);

        // Calculate max usage percentage across all tracked resources
        double maxPercentage = 0;
        string criticalResource = "";

        // Check Bookings
        if (usage.BookingsLimit > 0)
        {
            double p = (double)usage.BookingsUsed / usage.BookingsLimit * 100;
            if (p > maxPercentage) { maxPercentage = p; criticalResource = "Bookings"; }
        }

        // Check SMS
        if (usage.SmsLimit > 0)
        {
            double p = (double)usage.SmsUsed / usage.SmsLimit * 100;
            if (p > maxPercentage) { maxPercentage = p; criticalResource = "SMS Credits"; }
        }

        // Check AI Credits
        if (usage.AiCreditsLimit > 0)
        {
            double p = (double)usage.AiCreditsUsed / usage.AiCreditsLimit * 100;
            if (p > maxPercentage) { maxPercentage = p; criticalResource = "AI Credits"; }
        }

        // Check Storage
        if (usage.StorageLimitBytes > 0)
        {
            double p = (double)usage.StorageUsedBytes / usage.StorageLimitBytes * 100;
            if (p > maxPercentage) { maxPercentage = p; criticalResource = "Storage"; }
        }

        int currentThreshold = 0;
        if (maxPercentage >= 100) currentThreshold = 100;
        else if (maxPercentage >= 90) currentThreshold = 90;
        else if (maxPercentage >= 80) currentThreshold = 80;

        // --- NEW: AUTO-RESOLUTION & THRESHOLD RESET ---
        // If usage has dropped significantly below the alert zone, reset thresholds so new alerts can fire later
        if (maxPercentage < 50 && sub.LastAlertThreshold >= 80)
        {
            sub.LastAlertThreshold = 0;
            await ResolveBillingEscalationAsync(sub.TenantId, "Resource Quota Restored");
        }

        // Only alert if we've reached a new higher threshold in this period
        if (currentThreshold > sub.LastAlertThreshold)
        {
            await SendAlertAsync(sub.TenantId, currentThreshold, criticalResource, Math.Round(maxPercentage, 1));
            sub.LastAlertThreshold = currentThreshold;
        }

        // AI Cost specific alerts
        if (sub.AiMonthlyBudget > 0)
        {
            double aiPercent = (double)(usage.AiCostUsed / sub.AiMonthlyBudget) * 100;
            int aiThreshold = 0;
            if (aiPercent >= 100) aiThreshold = 100;
            else if (aiPercent >= 90) aiThreshold = 90;
            else if (aiPercent >= 80) aiThreshold = 80;

            // Auto-resolve AI threshold if usage is low
            if (aiPercent < 50 && sub.AiLastAlertThreshold >= 80)
            {
                sub.AiLastAlertThreshold = 0;
                await ResolveBillingEscalationAsync(sub.TenantId, "AI Budget Restored");
            }

            if (aiThreshold > sub.AiLastAlertThreshold)
            {
                await SendAlertAsync(sub.TenantId, aiThreshold, "AI Budget", Math.Round(aiPercent, 1));
                sub.AiLastAlertThreshold = aiThreshold;
            }
        }
    }

    private async Task ResolveBillingEscalationAsync(Guid tenantId, string note)
    {
        var activeEscalations = await _dbContext.Set<AIEscalation>()
            .Where(e => e.TenantId == tenantId && e.Module == "Billing" && !e.IsResolved)
            .ToListAsync();

        if (activeEscalations.Any())
        {
            foreach (var esc in activeEscalations)
            {
                esc.IsResolved = true;
                esc.ResolvedAt = DateTime.UtcNow;
                esc.ResolvedBy = "System (Auto-Sync)";
                esc.ResolutionNotes = note;
            }

            _logger.LogInformation("Auto-resolved {Count} billing escalations for tenant {TenantId} due to credit restoration.",
                activeEscalations.Count, tenantId);
        }
    }

    private async Task SendAlertAsync(Guid tenantId, int threshold, string resource, double percentage)
    {
        string title = threshold == 100 ? "🚨 Quota Reached!" : "⚠️ Usage Warning";
        string type = threshold == 100 ? "error" : "warning";

        string actionableAdvice = threshold == 100
            ? "Please UPGRADE your plan or TOP-UP your credits immediately to restore service."
            : "We recommend adding a top-up or upgrading your plan to avoid any disruption.";

        string message = threshold == 100
            ? $"You have reached 100% of your {resource} limit. {actionableAdvice}"
            : $"Your {resource} usage is at {percentage}%. {actionableAdvice}";

        // Send in-app notification
        await _notificationService.SendToTenantAsync(tenantId.ToString(), "SystemNotification", new
        {
            Title = title,
            Message = message,
            Type = type,
            Timestamp = DateTime.UtcNow
        });

        // Also send a toast to any active users
        await _notificationService.SendToTenantAsync(tenantId.ToString(), "ToastMessage", new
        {
            Title = title,
            Message = message,
            Type = type
        });

        // --- NEW: INTEGRATE WITH HUMAN-IN-THE-LOOP ESCALATION FOR CRITICAL/LOW QUOTAS ---
        if (threshold >= 90)
        {
            string severity = threshold == 100 ? "Critical" : "High";
            await _notificationService.EscalateAsync(tenantId, "Billing",
                $"{resource} usage is {threshold}%. Please top-up or upgrade to avoid service interruption.",
                severity, new { Resource = resource, Percentage = percentage }, false);
        }

        _logger.LogInformation("Sent {Threshold}% billing alert to tenant {TenantId} (Resource: {Resource})",
            threshold, tenantId, resource);
    }
}
