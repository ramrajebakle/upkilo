using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Days 78-79: Membership payment failure recovery.
/// Auto-retry + email sequence + grace period (3 days warn, 7 days pause).
/// Reactivation: SMS + email with one-click secure link.
/// </summary>
public class MembershipDunningService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<MembershipDunningService> _logger;

    public MembershipDunningService(
        AppDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<MembershipDunningService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    /// <summary>
    /// Called when Stripe fires invoice.payment_failed for a membership subscription.
    /// </summary>
    public async Task HandlePaymentFailureAsync(Guid tenantId, string stripeSubscriptionId, int attemptNumber)
    {
        var membership = await _context.ClientMemberships
            .Include(m => m.Client)
            .Include(m => m.MembershipPlan)
            .FirstOrDefaultAsync(m => m.TenantId == tenantId &&
                                      m.StripeSubscriptionId == stripeSubscriptionId);

        if (membership == null) return;

        var client = membership.Client;
        var planName = membership.MembershipPlan?.Name ?? "membership";

        switch (attemptNumber)
        {
            case 1:
                await SendDunningEmailAsync(client, planName, 0, tenantId);
                break;

            case 2:
                await SendDunningEmailAsync(client, planName, 3, tenantId);
                if (!string.IsNullOrEmpty(client.Phone) && client.SmsConsent)
                    await _smsService.SendSmsAsync(tenantId, client.Phone,
                        $"Hi {client.FirstName}, your {planName} payment failed. Update your card to avoid suspension.");
                break;

            case 3:
                membership.Status = MembershipStatus.Paused;
                membership.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await SendDunningEmailAsync(client, planName, 7, tenantId);
                _logger.LogWarning("[Dunning] Membership {Id} paused after 3 payment failures", membership.Id);
                break;
        }
    }

    /// <summary>
    /// Generate a one-click reactivation link for a paused membership and send SMS+email.
    /// </summary>
    public async Task<string> GenerateReactivationLinkAsync(Guid tenantId, Guid membershipId)
    {
        var membership = await _context.ClientMemberships
            .Include(m => m.Client)
            .Include(m => m.MembershipPlan)
            .FirstOrDefaultAsync(m => m.Id == membershipId && m.TenantId == tenantId);

        if (membership == null) throw new InvalidOperationException("Membership not found.");

        var client = membership.Client;
        var planName = membership.MembershipPlan?.Name ?? "membership";
        var tenant = await _context.Tenants.FindAsync(tenantId);
        var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{membershipId}:{DateTime.UtcNow.AddDays(7):O}"));
        var link = $"https://app.upkilo.com/book/{tenant?.Slug}/reactivate?token={Uri.EscapeDataString(token)}&membershipId={membershipId}";

        if (client.Email != null)
            await _emailService.SendEmailAsync(client.Email,
                $"Reactivate your {planName} membership",
                $"<h2>Hi {client.FirstName},</h2>" +
                $"<p>We'd love to have you back! Reactivate your <strong>{planName}</strong> with one click.</p>" +
                $"<p><a href='{link}' style='background:#4f46e5;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:bold;'>Reactivate Now →</a></p>" +
                "<p>This link expires in 7 days.</p>");

        if (!string.IsNullOrEmpty(client.Phone) && client.SmsConsent)
            await _smsService.SendSmsAsync(tenantId, client.Phone,
                $"Hi {client.FirstName}, reactivate your {planName}: {link}");

        return link;
    }

    private async Task SendDunningEmailAsync(Client client, string planName, int daysSinceFailure, Guid tenantId)
    {
        if (client.Email == null) return;
        var updateUrl = $"https://app.upkilo.com/billing/update-payment";
        var subject = daysSinceFailure >= 7 ? "Your membership has been paused"
                    : daysSinceFailure >= 3 ? "Urgent: Update your payment to avoid suspension"
                    : "Payment failed — action required";

        await _emailService.SendEmailAsync(client.Email, subject,
            $"<h2>Hi {client.FirstName},</h2>" +
            $"<p>We couldn't process your payment for <strong>{planName}</strong>" +
            (daysSinceFailure > 0 ? $" ({daysSinceFailure} days ago)" : "") + ".</p>" +
            (daysSinceFailure >= 7 ? "<p>Your membership has been <strong>paused</strong>.</p>" : "") +
            (daysSinceFailure is >= 3 and < 7 ? "<p>Your membership will be <strong>paused in 4 days</strong> if payment is not updated.</p>" : "") +
            $"<p><a href='{updateUrl}' style='background:#4f46e5;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:bold;'>Update Payment Method →</a></p>");
    }
}

/// <summary>
/// Daily at 09:00 UTC — scans paused memberships older than 30 days, sends reactivation push.
/// </summary>
public class MembershipDunningJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MembershipDunningJob> _logger;

    public MembershipDunningJob(IServiceProvider services, ILogger<MembershipDunningJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(9);
            await Task.Delay(nextRun - now, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dunning = scope.ServiceProvider.GetRequiredService<MembershipDunningService>();

            try
            {
                var paused = await context.ClientMemberships
                    .Include(m => m.Client)
                    .Where(m => m.Status == MembershipStatus.Paused &&
                                m.UpdatedAt <= DateTime.UtcNow.AddDays(-30) && !m.IsDeleted)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var m in paused)
                {
                    try { await dunning.GenerateReactivationLinkAsync(m.TenantId, m.Id); }
                    catch (Exception ex) { _logger.LogWarning(ex, "[DunningJob] Failed for {Id}", m.Id); }
                }

                _logger.LogInformation("[DunningJob] Sent reactivation to {Count} paused memberships", paused.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DunningJob] Batch failed");
            }
        }
    }
}
