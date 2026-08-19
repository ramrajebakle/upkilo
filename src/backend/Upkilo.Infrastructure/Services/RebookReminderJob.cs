using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Nudges clients whose last visit is now older than the service's own rebooking interval.
///
/// This is retention, not acquisition: the client already paid once, so the cost of bringing them
/// back is one message. It is vertical-neutral by design — the interval lives on
/// Service.RebookAfterDays, so a salon colour at 42 days, botox at 120, a physio review at 7 and a
/// full detail at 150 all run through this one job rather than needing a rule per industry.
///
/// Three things it deliberately refuses to do:
///
///  1. Message anyone who has not consented. These are marketing messages, not transactional
///     ones — the client is not being told about a booking they made, they are being asked to
///     make another. Client.MarketingConsent gates email and SmsConsent gates SMS, matching what
///     BroadcastController and CampaignsController already enforce. Sending regardless would be
///     unlawful under GDPR/PECR, CASL and the TCPA, and would put the tenant's sender reputation
///     at risk rather than ours alone.
///  2. Nudge a client who has already rebooked. A later booking for the same service means the
///     reminder is not just unnecessary, it is wrong — it tells someone with an appointment that
///     they have none.
///  3. Nudge twice. RebookReminderSentAt is stamped on the booking that triggered the message.
/// </summary>
public class RebookReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RebookReminderJob> _logger;

    // The overdue cutoff now lives on RebookAudienceService.MaxOverdue, with the rest of the
    // eligibility rules. A second copy here would be free to drift from the one actually applied.

    public RebookReminderJob(IServiceScopeFactory scopeFactory, ILogger<RebookReminderJob> logger)
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
                _logger.LogError(ex, "RebookReminderJob encountered an error");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    /// <summary>
    /// One pass. Public so the behaviour can be exercised directly in tests without waiting on
    /// the hosted-service timer — the consent gate in particular needs to be pinned down.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

        // Who is due, and who may be contacted, is decided by RebookAudienceService — the same
        // call that backs the tenant-facing audience preview. Sending from a second, private copy
        // of that logic is how a preview quietly stops matching what actually goes out.
        var audience = new RebookAudienceService(context);
        var candidates = await audience.GetDueAsync(tenantId: null, limit: 2000, ct);

        if (candidates.Count == 0) return;

        var now = DateTime.UtcNow;
        var sent = 0;
        var skipped = 0;

        foreach (var candidate in candidates)
        {
            if (ct.IsCancellationRequested) break;

            var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == candidate.BookingId, ct);
            if (booking == null) continue;

            // Stamped whether or not a message goes out. A client who has withheld consent or has
            // no contact details will never produce a different answer, so re-examining them every
            // night would only keep the query hot.
            booking.RebookReminderSentAt = now;

            if (candidate.Eligibility != RebookEligibility.Ready)
            {
                skipped++;
                continue;
            }

            var client = await context.Clients.FirstOrDefaultAsync(c => c.Id == candidate.ClientId, ct);
            if (client == null) { skipped++; continue; }

            var businessName = await context.Tenants
                .Where(t => t.Id == candidate.TenantId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(ct) ?? "your salon";

            var monthsSince = Math.Max(1, (int)Math.Round(candidate.DaysSinceVisit / 30.0));
            var plural = monthsSince == 1 ? "" : "s";
            var firstName = string.IsNullOrWhiteSpace(client.FirstName) ? "there" : client.FirstName;
            var delivered = false;

            try
            {
                if (candidate.Channel == "email" && !string.IsNullOrWhiteSpace(client.Email))
                {
                    var subject = $"Time to book your next {candidate.ServiceName}?";
                    var body =
                        $"Hi {firstName},\n\n" +
                        $"It has been about {monthsSince} month{plural} since your last {candidate.ServiceName} " +
                        $"at {businessName}. If you would like to keep to the same schedule, now is a good time to book.\n\n" +
                        $"Book online at any time, or reply to this message and we will find you a slot.\n\n" +
                        $"— {businessName}";

                    await emailService.SendSystemEmailAsync(client.Email!, subject, body);
                    delivered = true;
                }
                else if (candidate.Channel == "sms" && !string.IsNullOrWhiteSpace(client.Phone))
                {
                    var smsBody =
                        $"Hi {firstName}, it's been ~{monthsSince} month{plural} since your last " +
                        $"{candidate.ServiceName} at {businessName}. Ready to book again?";

                    var result = await smsService.SendSmsAsync(candidate.TenantId, client.Phone!, smsBody, client.Id);
                    delivered = result.Success;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rebook reminder failed for client {ClientId} via {Channel}",
                    candidate.ClientId, candidate.Channel);
            }

            if (delivered) sent++; else skipped++;
        }

        await context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "RebookReminderJob: {Sent} sent, {Skipped} skipped, {Examined} due",
            sent, skipped, candidates.Count);
    }
}
