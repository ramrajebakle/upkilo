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
/// Warns tenants before their trial runs out, escalating as the date approaches.
///
/// Grown out of the never-registered Upkilo.Infrastructure.Jobs.OnboardingDripJob, which already
/// contained a "Your Trial Ends in N Days" email. That job was dead three times over: it was never
/// added to Hangfire, it filtered on TrialEndsAt which nothing ever set, and its de-duplication
/// mutated Tenant.Metadata in place — a dictionary EF does not see as changed unless it is
/// reassigned — so had it ever run it would have re-sent the same email on every pass.
///
/// The setup-nudge emails from that job are not reproduced here: OnboardingDripJob already owns
/// "you have not finished setting up", and two jobs mailing about the same thing is how people
/// unsubscribe.
/// </summary>
public class TrialReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrialReminderJob> _logger;

    /// <summary>
    /// Days-remaining thresholds, each sent at most once. Seven gives time to evaluate, three is
    /// the decision point (and matches UpsellTriggerService's existing Critical trigger), one is
    /// the last call.
    /// </summary>
    private static readonly int[] Milestones = { 7, 3, 1 };

    public TrialReminderJob(IServiceScopeFactory scopeFactory, ILogger<TrialReminderJob> logger)
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
                _logger.LogError(ex, "TrialReminderJob encountered an error");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    /// <summary>One pass. Public so it can be driven directly in tests.</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var appUrl = (configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
        var now = DateTime.UtcNow;

        // Tenant.TrialEndsAt, not Subscription.EndDate — see TrialExpiryJob for why that column
        // cannot carry this meaning.
        var trialingTenants = await context.Tenants
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted && t.TrialEndsAt != null && t.TrialEndsAt > now)
            .ToListAsync(ct);

        if (trialingTenants.Count == 0) return;

        var candidateIds = trialingTenants.Select(t => t.Id).ToList();

        var active = await context.Set<Subscription>()
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                        && s.Status == SubscriptionStatus.Trialing
                        && candidateIds.Contains(s.TenantId))
            .ToListAsync(ct);

        if (active.Count == 0) return;

        var tenantIds = active.Select(s => s.TenantId).ToList();
        var tenants = trialingTenants.Where(t => tenantIds.Contains(t.Id)).ToList();

        var owners = (await context.Users
            .IgnoreQueryFilters()
            .Where(u => tenantIds.Contains(u.TenantId) && !u.IsDeleted
                        && (u.Role == UserRole.Owner || u.Role == UserRole.Admin))
            .OrderBy(u => u.Role).ThenBy(u => u.CreatedAt)
            .Select(u => new { u.TenantId, u.Email, u.FirstName })
            .ToListAsync(ct))
            .GroupBy(u => u.TenantId)
            .ToDictionary(g => g.Key, g => g.First());

        var sent = 0;

        foreach (var subscription in active)
        {
            var tenant = tenants.FirstOrDefault(t => t.Id == subscription.TenantId);
            if (tenant == null) continue;

            var daysLeft = (int)Math.Ceiling((tenant.TrialEndsAt!.Value - now).TotalDays);

            // The SMALLEST milestone still at or above days-remaining. Milestones is descending, so
            // LastOrDefault gives that; FirstOrDefault would give the largest and mail "7 days
            // left" to somebody with 2. Choosing the tightest bucket also means a job that misses a
            // window — a deploy, an outage — sends the correct, more urgent message on its next
            // pass rather than a stale one.
            var milestone = Milestones.LastOrDefault(m => daysLeft <= m);
            if (milestone == 0) continue;

            var metadataKey = $"trial_reminder_{milestone}d_sent";
            if (tenant.Metadata != null && tenant.Metadata.ContainsKey(metadataKey)) continue;

            owners.TryGetValue(subscription.TenantId, out var owner);
            var recipient = !string.IsNullOrWhiteSpace(tenant.Email) ? tenant.Email : owner?.Email;
            if (string.IsNullOrWhiteSpace(recipient)) continue;

            var greetingName = !string.IsNullOrWhiteSpace(owner?.FirstName) ? owner!.FirstName : "there";
            var planName = tenant.SubscriptionTier.ToString();

            try
            {
                await emailService.SendSystemEmailAsync(
                    recipient,
                    SubjectFor(milestone, daysLeft),
                    BodyFor(milestone, daysLeft, greetingName, planName, appUrl));

                // Reassign the dictionary rather than mutating it. Tenant.Metadata is a jsonb
                // property; EF compares it by reference, so an in-place index assignment is not
                // detected as a change and never persists — which is what made the original job's
                // de-duplication a no-op. SettingsController hit and documented the same trap.
                tenant.Metadata = new Dictionary<string, object>(tenant.Metadata ?? new())
                {
                    [metadataKey] = now.ToString("O")
                };
                tenant.UpdatedAt = now;
                sent++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {Milestone}-day trial reminder to tenant {TenantId}",
                    milestone, subscription.TenantId);
            }
        }

        if (sent > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("TrialReminderJob: {Count} reminders sent", sent);
        }
    }

    private static string SubjectFor(int milestone, int daysLeft) => milestone switch
    {
        7 => $"{daysLeft} days left in your Upkilo trial",
        3 => $"Your Upkilo trial ends in {daysLeft} days",
        _ => "Your Upkilo trial ends tomorrow",
    };

    private static string BodyFor(int milestone, int daysLeft, string name, string planName, string appUrl)
    {
        var cta = $@"<p><a href='{appUrl}/settings/billing?upgrade=true' style='background:#6366f1;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;'>Choose your plan →</a></p>";
        var footer = $@"<p style='color:#6b7280;font-size:12px;margin-top:24px;'>You're receiving this because you signed up for Upkilo. <a href='{appUrl}/settings/notifications'>Manage preferences</a></p>";

        // Deliberately states what happens at expiry rather than implying deletion. The account is
        // not lost, it moves to Free — saying otherwise would be a lie the product then has to
        // live down the first time somebody checks.
        return milestone switch
        {
            7 => $@"<h2>You've got {daysLeft} days left, {name}</h2>
                    <p>You're currently on the <strong>{planName}</strong> trial, with every feature switched on.</p>
                    <p>If you'd like to keep them past the trial, you can pick a plan any time — no interruption to your bookings.</p>
                    {cta}{footer}",

            3 => $@"<h2>{daysLeft} days left, {name}</h2>
                    <p>Your <strong>{planName}</strong> trial ends soon. After that your account moves to the Free plan: your data and booking page stay exactly as they are, but you'll drop to 1 staff member and 100 clients, and the premium features switch off.</p>
                    <p>Upgrade now and nothing changes.</p>
                    {cta}{footer}",

            _ => $@"<h2>Last day, {name}</h2>
                    <p>Your <strong>{planName}</strong> trial ends tomorrow. Your account and bookings are safe either way — it will move to the Free plan, with 1 staff member and 100 clients.</p>
                    <p>To keep everything you've been using, choose a plan today.</p>
                    {cta}{footer}",
        };
    }
}
