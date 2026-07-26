using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Daily Hangfire job that reconciles local subscription states with Stripe.
/// Detects and corrects drift between the local Subscription records
/// and the actual Stripe subscription statuses — protecting against
/// missed webhooks, network failures, or manual Stripe dashboard edits.
/// </summary>
public class BillingReconciliationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BillingReconciliationJob> _logger;

    public BillingReconciliationJob(IServiceScopeFactory scopeFactory, ILogger<BillingReconciliationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("BillingReconciliationJob started");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get all active subscriptions that have a Stripe ID
        var subscriptions = await context.Subscriptions
            .Where(s => !string.IsNullOrEmpty(s.StripeSubscriptionId) &&
                        s.Status != SubscriptionStatus.Cancelled)
            .ToListAsync();

        _logger.LogInformation("Reconciling {Count} active subscriptions with Stripe", subscriptions.Count);

        int synced = 0, drifted = 0, errors = 0;

        foreach (var sub in subscriptions)
        {
            try
            {
                var stripeService = new Stripe.SubscriptionService();
                var stripeSub = await stripeService.GetAsync(sub.StripeSubscriptionId);

                if (stripeSub == null)
                {
                    _logger.LogWarning(
                        "Stripe subscription {StripeId} not found for tenant {TenantId}",
                        sub.StripeSubscriptionId, sub.TenantId);
                    errors++;
                    continue;
                }

                var stripeStatus = MapStripeStatus(stripeSub.Status);
                var hasDrift = false;

                // Check status drift
                if (sub.Status != stripeStatus)
                {
                    _logger.LogWarning(
                        "Status drift for tenant {TenantId}: local={LocalStatus} stripe={StripeStatus}",
                        sub.TenantId, sub.Status, stripeStatus);
                    sub.Status = stripeStatus;
                    hasDrift = true;
                }

                // Check period dates drift
                /*
                if (stripeSub.CurrentPeriodStart != sub.CurrentPeriodStart ||
                    stripeSub.CurrentPeriodEnd != sub.CurrentPeriodEnd)
                {
                    sub.CurrentPeriodStart = stripeSub.CurrentPeriodStart;
                    sub.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd;
                    hasDrift = true;
                }
                */

                // Check cancellation drift
                if (stripeSub.CanceledAt.HasValue && !sub.CancelledAt.HasValue)
                {
                    sub.CancelledAt = stripeSub.CanceledAt;
                    hasDrift = true;
                }

                if (hasDrift)
                {
                    sub.UpdatedAt = DateTime.UtcNow;
                    drifted++;
                }

                synced++;
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe API error while reconciling subscription {StripeId} for tenant {TenantId}",
                    sub.StripeSubscriptionId, sub.TenantId);
                errors++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error reconciling subscription for tenant {TenantId}",
                    sub.TenantId);
                errors++;
            }
        }

        if (drifted > 0)
        {
            await context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "BillingReconciliationJob complete: {Synced} synced, {Drifted} corrected, {Errors} errors",
            synced, drifted, errors);
    }

    private static SubscriptionStatus MapStripeStatus(string stripeStatus) => stripeStatus switch
    {
        "active" => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Cancelled,
        "unpaid" => SubscriptionStatus.Suspended,
        "trialing" => SubscriptionStatus.Trial,
        "paused" => SubscriptionStatus.Paused,
        "incomplete" => SubscriptionStatus.PastDue,
        "incomplete_expired" => SubscriptionStatus.Cancelled,
        _ => SubscriptionStatus.Active
    };
}
