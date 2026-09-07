using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Lands expired trials on the Free plan.
///
/// Signup grants the top plan for PricingPlan.TrialDays (a "reverse trial"). Without this job that
/// grant would never end — which is the state the product was actually in: Tenant.TrialEndsAt and
/// SubscriptionStatus.Trialing both existed, and nothing anywhere read them to expire anything.
///
/// The landing is deliberately soft. The tenant keeps their data and their public booking page
/// stays live, so their own customers are never caught in our billing decisions; what they lose is
/// the paid feature set and the Free plan's limits start applying. That is real pressure to
/// upgrade without breaking somebody's business to apply it.
/// </summary>
public class TrialExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrialExpiryJob> _logger;

    public TrialExpiryJob(IServiceScopeFactory scopeFactory, ILogger<TrialExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TrialExpiryJob encountered an error");
            }

            // Hourly rather than daily: a trial that ended at 09:00 should not keep serving paid
            // features until midnight, and an hourly pass keeps the "your trial ended" email close
            // enough to the event to still feel like a consequence.
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    /// <summary>
    /// One pass. Public for the same reason as OnboardingDripJob.RunAsync — the only other entry
    /// point is an infinite loop with an hour-long delay in it.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var entitlements = scope.ServiceProvider.GetRequiredService<IEntitlementService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var appUrl = (configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
        var now = DateTime.UtcNow;

        var freePlan = await context.PricingPlans
            .FirstOrDefaultAsync(p => p.Name == "Free", ct);

        if (freePlan == null)
        {
            // Downgrading to a plan that does not exist would strand these tenants on a null plan,
            // which is worse than leaving the trial running for another hour.
            _logger.LogError("Free pricing plan missing; cannot expire trials this pass");
            return;
        }

        // Tenant.TrialEndsAt is the source of truth for when a trial runs out — it is the purpose-
        // built nullable field. Subscription.EndDate is NOT usable for this: it is non-nullable and
        // effectively vestigial (CurrentPeriodEnd is the real billing period), so there is no way
        // to express "does not expire" in it.
        var expiredTenants = await context.Tenants
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted && t.TrialEndsAt != null && t.TrialEndsAt <= now)
            .ToListAsync(ct);

        if (expiredTenants.Count == 0) return;

        var candidateIds = expiredTenants.Select(t => t.Id).ToList();

        // Status is the second half of the condition, and the one that makes this idempotent: a
        // tenant who upgraded mid-trial is Active, not Trialing, and must never be caught here
        // however their TrialEndsAt reads. Once downgraded they are Active too, so a second pass
        // finds nothing.
        var expired = await context.Set<Subscription>()
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                        && s.Status == SubscriptionStatus.Trialing
                        && candidateIds.Contains(s.TenantId))
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        _logger.LogInformation("TrialExpiryJob: {Count} trials to expire", expired.Count);

        var tenantIds = expired.Select(s => s.TenantId).ToList();
        var tenants = expiredTenants.Where(t => tenantIds.Contains(t.Id)).ToList();

        var owners = (await context.Users
            .IgnoreQueryFilters()
            .Where(u => tenantIds.Contains(u.TenantId) && !u.IsDeleted
                        && (u.Role == UserRole.Owner || u.Role == UserRole.Admin))
            .OrderBy(u => u.Role).ThenBy(u => u.CreatedAt)
            .Select(u => new { u.TenantId, u.Email, u.FirstName })
            .ToListAsync(ct))
            .GroupBy(u => u.TenantId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var subscription in expired)
        {
            var tenant = tenants.FirstOrDefault(t => t.Id == subscription.TenantId);
            if (tenant == null) continue;

            var previousPlanName = tenant.SubscriptionTier.ToString();

            // Free is a real plan, not a suspension, so the subscription goes back to Active on it.
            // Status is what ends the trial; EndDate keeps the date the trial ran out, which is now
            // an accurate historical record rather than a pending expiry.
            subscription.PricingPlanId = freePlan.Id;
            subscription.Status = SubscriptionStatus.Active;
            subscription.UpdatedAt = now;

            tenant.PricingPlanId = freePlan.Id;
            tenant.SubscriptionTier = SubscriptionTierMap.FromPlanName(freePlan.Name);
            tenant.UpdatedAt = now;
            // TrialEndsAt is deliberately left in place. It is the record that this tenant HAD a
            // trial and when it ran out, which the upgrade prompts and any win-back campaign both
            // need. Status is what says the trial is over.

            // The entitlement snapshot is cached; without this the tenant keeps paid features until
            // the cache happens to expire, which is precisely the window a downgrade must not have.
            await entitlements.InvalidateAsync(tenant.Id);

            _logger.LogInformation(
                "Trial expired for tenant {TenantId}: {PreviousPlan} -> Free", tenant.Id, previousPlanName);

            owners.TryGetValue(subscription.TenantId, out var owner);
            var recipient = !string.IsNullOrWhiteSpace(tenant.Email) ? tenant.Email : owner?.Email;
            if (string.IsNullOrWhiteSpace(recipient)) continue;

            var greetingName = !string.IsNullOrWhiteSpace(owner?.FirstName) ? owner!.FirstName : "there";

            try
            {
                await emailService.SendSystemEmailAsync(
                    recipient,
                    "Your Upkilo trial has ended — your account is still here",
                    $@"<h2>Your trial has ended, {greetingName}</h2>
                       <p>Your {previousPlanName} trial is over and your account has moved to the <strong>Free plan</strong>.</p>
                       <p><strong>Nothing has been deleted.</strong> Your clients, bookings and settings are all exactly where you left them, and your booking page is still live and taking bookings.</p>
                       <p>What changes on Free: 1 staff member, up to 100 clients, and the premium features from your trial are switched off.</p>
                       <p>Upgrade any time and everything switches straight back on.</p>
                       <p><a href='{appUrl}/settings/billing?upgrade=true' style='background:#6366f1;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;'>See plans →</a></p>
                       <p style='color:#6b7280;font-size:12px;margin-top:24px;'>You're receiving this because you signed up for Upkilo. <a href='{appUrl}/settings/notifications'>Manage preferences</a></p>");
            }
            catch (Exception ex)
            {
                // The downgrade itself still stands — an email failure must not leave a tenant on a
                // paid plan they are not paying for.
                _logger.LogError(ex, "Failed to send trial-ended email to tenant {TenantId}", tenant.Id);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
