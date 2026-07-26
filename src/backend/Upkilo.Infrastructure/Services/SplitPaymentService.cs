using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Manages split payments and deposit workflows for bookings.
/// Supports deposit-first booking with configurable deposit percentages,
/// installment schedules, and automatic balance reminders.
/// </summary>
public class SplitPaymentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SplitPaymentService> _logger;

    public SplitPaymentService(AppDbContext context, ILogger<SplitPaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a split payment record for a booking with a configurable deposit.
    /// </summary>
    public async Task<SplitPayment> CreateDepositAsync(
        Guid tenantId, Guid bookingId, decimal totalAmount, decimal depositPercentage = 50,
        string currency = "usd")
    {
        var depositAmount = Math.Round(totalAmount * depositPercentage / 100, 2);

        var split = new SplitPayment
        {
            TenantId = tenantId,
            BookingId = bookingId,
            TotalAmount = totalAmount,
            Currency = currency,
            Status = "Pending",
            SplitType = "Deposit",
            DepositAmount = depositAmount,
            DepositPercentage = depositPercentage
        };

        _context.Set<SplitPayment>().Add(split);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created split payment for booking {BookingId}: {Deposit}/{Total} ({Pct}% deposit)",
            bookingId, depositAmount, totalAmount, depositPercentage);

        return split;
    }

    /// <summary>
    /// Records deposit payment completion.
    /// </summary>
    public async Task<bool> RecordDepositPaymentAsync(Guid splitPaymentId, string stripePaymentIntentId)
    {
        var split = await _context.Set<SplitPayment>().FindAsync(splitPaymentId);
        if (split == null) return false;

        split.Status = "DepositPaid";
        split.DepositPaidAt = DateTime.UtcNow;
        split.StripePaymentIntentId = stripePaymentIntentId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deposit paid for split payment {Id}", splitPaymentId);
        return true;
    }

    /// <summary>
    /// Records full payment completion (remaining balance paid).
    /// </summary>
    public async Task<bool> RecordFullPaymentAsync(Guid splitPaymentId)
    {
        var split = await _context.Set<SplitPayment>().FindAsync(splitPaymentId);
        if (split == null) return false;

        split.Status = "FullyPaid";
        split.FullyPaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Full payment completed for split payment {Id}", splitPaymentId);
        return true;
    }

    /// <summary>
    /// Gets the remaining balance for a split payment.
    /// </summary>
    public async Task<decimal> GetRemainingBalanceAsync(Guid splitPaymentId)
    {
        var split = await _context.Set<SplitPayment>().FindAsync(splitPaymentId);
        if (split == null) return 0;

        return split.Status switch
        {
            "FullyPaid" => 0,
            "DepositPaid" => split.TotalAmount - split.DepositAmount,
            _ => split.TotalAmount
        };
    }

    /// <summary>
    /// Gets all pending split payments for a tenant (for reminders/follow-up).
    /// </summary>
    public async Task<List<SplitPayment>> GetPendingPaymentsAsync(Guid tenantId)
    {
        return await _context.Set<SplitPayment>()
            .Where(s => s.TenantId == tenantId && s.Status == "DepositPaid")
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }
}
