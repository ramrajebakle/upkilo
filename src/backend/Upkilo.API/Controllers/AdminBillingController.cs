using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Platform-level billing and revenue management (platform admins only)
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/admin/billing")]
[Authorize(Roles = "SuperAdmin")]
public class AdminBillingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminBillingController> _logger;

    public AdminBillingController(AppDbContext context, ILogger<AdminBillingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all invoices across all tenants
    /// </summary>
    [HttpGet("invoices")]
    public async Task<IActionResult> GetAllInvoices([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _context.Invoices.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, true, out var statusEnum))
            query = query.Where(i => i.Status == statusEnum);

        var total = await query.CountAsync();
        var invoices = await query
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items = invoices });
    }

    /// <summary>
    /// Mark an invoice as paid manually
    /// </summary>
    [HttpPost("invoices/{id}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id)
    {
        var invoice = await _context.Invoices.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return NotFound();

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceId} marked as paid by platform admin", id);
        return Ok(new { success = true, paidAt = invoice.PaidAt });
    }

    /// <summary>
    /// Refund an invoice manually
    /// SECURITY (M-1): Explicit !IsDeleted check since IgnoreQueryFilters bypasses soft-delete too.
    /// </summary>
    [HttpPost("invoices/{id}/refund")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] decimal? amount = null)
    {
        var invoice = await _context.Invoices
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        if (invoice == null) return NotFound();

        // SECURITY (M-5): Validate refund amount
        if (amount.HasValue && (amount.Value <= 0 || amount.Value > invoice.TotalAmount))
            return BadRequest("Refund amount must be between 0 and the invoice total");

        invoice.Status = InvoiceStatus.Refunded;
        invoice.RefundedAmount = amount ?? invoice.TotalAmount;
        invoice.RefundedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceId} refunded by platform admin. Amount: {Amount}", id, invoice.RefundedAmount);
        return Ok(new { success = true, refundedAt = invoice.RefundedAt, amount = invoice.RefundedAmount });
    }

    /// <summary>
    /// Get revenue summary across all tenants
    /// </summary>
    [HttpGet("revenue-summary")]
    public async Task<IActionResult> GetRevenueSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var startDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = to ?? DateTime.UtcNow;

        // Aggregated in SQL rather than by materializing every invoice. This query spans all
        // tenants and an unbounded caller-supplied date range; pulling full rows (including the
        // jsonb Metadata column) back just to sum them scaled with invoice volume. Grouped in the
        // database it returns one row per currency instead.
        var byCurrency = await _context.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= startDate && i.PaidAt <= endDate)
            .GroupBy(i => i.Currency)
            .Select(g => new
            {
                currency = g.Key,
                totalRevenue = g.Sum(i => i.TotalAmount),
                invoiceCount = g.Count()
            })
            .ToListAsync();

        var count = byCurrency.Sum(x => x.invoiceCount);

        // Reported per currency rather than as one figure.
        //
        // This query spans every tenant, and tenants settle in the currency of their own connected
        // Stripe account. A flat Sum() therefore added rupees to yen to dollars and printed the
        // result as revenue — a number that is not wrong so much as meaningless. It was invisible
        // only because every tenant was USD; it breaks the moment a non-US tenant onboards.
        //
        // No conversion is applied: that needs a rate source and a policy about which day's rate
        // applies, and inventing one here would replace an obviously-wrong number with a
        // plausibly-wrong one.
        //
        // Codes are folded case-insensitively here rather than in the GROUP BY, so that legacy
        // rows written before currency was normalized on save ("usd") merge with current ones.
        var currencies = byCurrency
            .GroupBy(x => Upkilo.Core.Helpers.Currency.Normalize(x.currency))
            .Select(g => new
            {
                currency = g.Key,
                totalRevenue = g.Sum(x => x.totalRevenue),
                invoiceCount = g.Sum(x => x.invoiceCount),
                averageInvoiceValue = g.Sum(x => x.invoiceCount) > 0
                    ? g.Sum(x => x.totalRevenue) / g.Sum(x => x.invoiceCount)
                    : 0
            })
            .OrderByDescending(x => x.invoiceCount)
            .ToList();

        return Ok(new
        {
            startDate,
            endDate,
            invoiceCount = count,
            currencies,
            // Retained for existing callers, and only populated when a single currency is present.
            // Null across mixed currencies is deliberate: a caller that renders it blank is
            // preferable to one that renders an added-up total of different units.
            totalRevenue = currencies.Count == 1 ? currencies[0].totalRevenue : (decimal?)null,
            currency = currencies.Count == 1 ? currencies[0].currency : null,
            isMixedCurrency = currencies.Count > 1
        });
    }

}
