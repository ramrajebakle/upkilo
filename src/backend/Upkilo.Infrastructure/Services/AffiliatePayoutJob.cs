using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Helpers;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Monthly batch job: aggregates approved affiliate commissions into a single payout per partner.
/// Runs on the 1st of each month. Uses Stripe Connect to disburse funds.
/// </summary>
public class AffiliatePayoutJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AffiliatePayoutJob> _logger;

    // Distributed lock key — prevents double payouts when multiple API replicas are running.
    private const string LockKey = "locks:affiliate-payout-job";
    private static readonly TimeSpan LockTtl = TimeSpan.FromHours(2);

    public AffiliatePayoutJob(IServiceProvider services, ILogger<AffiliatePayoutJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = new DateTime(now.Year, now.Month, 1, 2, 0, 0, DateTimeKind.Utc).AddMonths(1);
                var delay = nextRun - now;
                if (delay <= TimeSpan.Zero) delay = TimeSpan.FromHours(24);

                _logger.LogInformation("[AffiliatePayoutJob] Next run scheduled at {NextRun}", nextRun);
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                    await RunPayoutCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown — exit cleanly without logging as error
                break;
            }
            catch (Exception ex)
            {
                // Log and continue — never let one cycle kill the BackgroundService permanently
                _logger.LogError(ex, "[AffiliatePayoutJob] Unhandled exception in payout cycle. Retrying in 1 hour.");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    internal async Task RunPayoutCycleAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();

        // Atomic distributed lock via Redis SET NX — prevents double payouts across replicas.
        var redis = scope.ServiceProvider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        var lockValue = Environment.MachineName;
        var acquired = await db.StringSetAsync(LockKey, lockValue, LockTtl, StackExchange.Redis.When.NotExists);
        if (!acquired)
        {
            var holder = (string?)await db.StringGetAsync(LockKey);
            _logger.LogInformation("[AffiliatePayoutJob] Lock held by {Holder} — skipping cycle on this replica.", holder);
            return;
        }

        try
        {
            await RunPayoutCycleInternalAsync(scope, ct);
        }
        finally
        {
            await db.KeyDeleteAsync(LockKey);
        }
    }

    private async Task RunPayoutCycleInternalAsync(IServiceScope scope, CancellationToken ct)
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Find all partners with pending approved commissions
        var pendingByPartner = await context.AffiliateCommissions
            .Where(c => c.Status == AffiliateCommissionStatus.Pending && c.PayoutId == null)
            .Include(c => c.PartnerAccount)
            .GroupBy(c => c.PartnerAccountId)
            .ToListAsync(ct);

        foreach (var group in pendingByPartner)
        {
            var partner = group.First().PartnerAccount;
            if (partner == null) continue;

            var totalAmount = group.Sum(c => c.CommissionAmount);
            var currency = group.First().Currency;

            var payout = new AffiliatePayout
            {
                Id = Guid.NewGuid(),
                PartnerAccountId = partner.Id,
                Amount = totalAmount,
                Currency = currency,
                PayoutMethod = partner.PayoutMethod ?? "Stripe",
                Status = AffiliatePayoutStatus.Processing,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.AffiliatePayouts.Add(payout);
            await context.SaveChangesAsync(ct);

            // Try Stripe Connect payout if account is configured
            bool payoutSucceeded = false;
            if (!string.IsNullOrEmpty(partner.StripeConnectAccountId))
            {
                try
                {
                    var transferOptions = new Stripe.TransferCreateOptions
                    {
                        // Scaled by the currency's minor-unit exponent. A flat *100 would have
                        // transferred 100x the commission owed for a zero-decimal currency.
                        Amount = Currency.ToMinorUnits(totalAmount, currency),
                        Currency = Currency.Normalize(currency).ToLowerInvariant(),
                        Destination = partner.StripeConnectAccountId,
                        Description = $"Upkilo affiliate commission payout — {DateTime.UtcNow:MMMM yyyy}",
                        Metadata = new Dictionary<string, string>
                        {
                            ["payout_id"] = payout.Id.ToString(),
                            ["partner_id"] = partner.Id.ToString()
                        }
                    };

                    // Idempotency key ensures re-runs after a crash don't create duplicate transfers.
                    var requestOptions = new Stripe.RequestOptions { IdempotencyKey = $"payout-{payout.Id}" };
                    using var stripeCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, stripeCts.Token);
                    var transfer = await new Stripe.TransferService().CreateAsync(transferOptions, requestOptions, cancellationToken: linkedCts.Token);

                    payout.Status = AffiliatePayoutStatus.Completed;
                    payout.ProcessedAt = DateTime.UtcNow;
                    payout.TransactionReference = transfer.Id;
                    payoutSucceeded = true;
                }
                catch (Stripe.StripeException ex)
                {
                    _logger.LogError(ex, "[AffiliatePayoutJob] Stripe payout failed for partner {PartnerId}", partner.Id);
                    payout.Status = AffiliatePayoutStatus.Failed;
                    payout.FailedAt = DateTime.UtcNow;
                    payout.FailureReason = ex.Message;
                }
            }
            else
            {
                // No Stripe account — mark as scheduled for manual processing
                payout.Status = AffiliatePayoutStatus.Scheduled;
                payout.Notes = "Manual payout — partner has not connected a Stripe account";
                payoutSucceeded = true;
            }

            // Link commissions to payout
            foreach (var commission in group)
            {
                commission.PayoutId = payout.Id;
                commission.Status = payoutSucceeded ? AffiliateCommissionStatus.PaidOut : AffiliateCommissionStatus.Pending;
            }

            // Update partner totals
            if (payoutSucceeded)
            {
                partner.TotalEarnings += totalAmount;
                partner.PendingPayout = Math.Max(0, partner.PendingPayout - totalAmount);
            }

            await context.SaveChangesAsync(ct);

            // Send payout notification email
            if (payoutSucceeded && !string.IsNullOrEmpty(partner.ContactEmail))
            {
                await emailService.SendSystemEmailAsync(
                    partner.ContactEmail,
                    $"Your Upkilo commission payout: {currency} {totalAmount:F2}",
                    $"<h2>Payout Confirmed!</h2>" +
                    $"<p>Hi {partner.PartnerName},</p>" +
                    $"<p>Your commission payout of <strong>{currency} {totalAmount:F2}</strong> has been processed for {DateTime.UtcNow.AddMonths(-1):MMMM yyyy}.</p>" +
                    $"<p>View your earnings dashboard: <a href='https://app.upkilo.com/affiliates/dashboard'>Affiliate Dashboard</a></p>" +
                    $"<p>Thank you for growing Upkilo!</p>"
                );
            }

            _logger.LogInformation("[AffiliatePayoutJob] Partner {PartnerId} payout {Amount} {Currency} — {Status}",
                partner.Id, totalAmount, currency, payout.Status);
        }
    }
}
