using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Manages affiliate/partner commissions, payouts, and analytics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/affiliates")]
[Authorize]
public class AffiliatesController : ControllerBase
{
    private readonly ILogger<AffiliatesController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public AffiliatesController(
        ILogger<AffiliatesController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all partner accounts for this tenant
    /// </summary>
    [HttpGet("partners")]
    public async Task<IActionResult> GetPartners([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.PartnerAccounts
            .Where(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        var total = await query.CountAsync();
        var partners = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { data = partners, total, page, pageSize });
    }

    /// <summary>
    /// Get commission history for a partner
    /// </summary>
    [HttpGet("partners/{partnerId}/commissions")]
    public async Task<IActionResult> GetCommissions(
        Guid partnerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.AffiliateCommissions
            .Where(c => c.PartnerAccountId == partnerId && c.TenantId == tenantId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AffiliateCommissionStatus>(status, true, out var parsed))
            query = query.Where(c => c.Status == parsed);

        var total = await query.CountAsync();
        var commissions = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { data = commissions, total, page, pageSize });
    }

    /// <summary>
    /// Record a new commission for a partner
    /// </summary>
    [HttpPost("partners/{partnerId}/commissions")]
    public async Task<IActionResult> RecordCommission(Guid partnerId, [FromBody] RecordCommissionRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var partner = await _context.PartnerAccounts
            .FirstOrDefaultAsync(p => p.Id == partnerId && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (partner == null) return NotFound(new { error = "Partner not found." });

        var commissionRate = partner.RevenueSharePercent / 100m;
        var commissionAmount = request.GrossAmount * commissionRate;

        var commission = new AffiliateCommission
        {
            Id = Guid.NewGuid(),
            PartnerAccountId = partnerId,
            TenantId = tenantId.Value,
            PaymentId = request.PaymentId,
            Source = request.Source,
            GrossAmount = request.GrossAmount,
            CommissionRate = commissionRate,
            CommissionAmount = commissionAmount,
            Currency = request.Currency ?? "USD",
            Status = AffiliateCommissionStatus.Pending,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.AffiliateCommissions.Add(commission);
        partner.TotalEarnings += commissionAmount;
        partner.PendingPayout += commissionAmount;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Commission recorded: ${Amount} for partner {PartnerId}", commissionAmount, partnerId);

        return CreatedAtAction(nameof(GetCommissions), new { partnerId }, commission);
    }

    /// <summary>
    /// Get payout history for a partner
    /// </summary>
    [HttpGet("partners/{partnerId}/payouts")]
    public async Task<IActionResult> GetPayouts(Guid partnerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.AffiliatePayouts
            .Where(p => p.PartnerAccountId == partnerId)
            .Include(p => p.Commissions);

        var total = await query.CountAsync();
        var payouts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Amount,
                p.Currency,
                p.PayoutMethod,
                p.TransactionReference,
                p.Status,
                p.ProcessedAt,
                p.FailedAt,
                p.FailureReason,
                p.CreatedAt,
                CommissionCount = p.Commissions.Count
            })
            .ToListAsync();

        return Ok(new { data = payouts, total, page, pageSize });
    }

    /// <summary>
    /// Request a payout — batches all pending commissions into one payout
    /// </summary>
    [HttpPost("partners/{partnerId}/payouts")]
    public async Task<IActionResult> RequestPayout(Guid partnerId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var partner = await _context.PartnerAccounts
            .FirstOrDefaultAsync(p => p.Id == partnerId && p.TenantId == tenantId.Value && !p.IsDeleted);
        if (partner == null) return NotFound();

        var pendingCommissions = await _context.AffiliateCommissions
            .Where(c => c.PartnerAccountId == partnerId && c.Status == AffiliateCommissionStatus.Approved)
            .ToListAsync();

        if (!pendingCommissions.Any())
            return BadRequest(new { error = "No approved commissions to pay out." });

        var totalAmount = pendingCommissions.Sum(c => c.CommissionAmount);

        var payout = new AffiliatePayout
        {
            Id = Guid.NewGuid(),
            PartnerAccountId = partnerId,
            Amount = totalAmount,
            Currency = pendingCommissions.First().Currency,
            PayoutMethod = partner.PayoutMethod ?? "Stripe",
            Status = AffiliatePayoutStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        _context.AffiliatePayouts.Add(payout);

        foreach (var c in pendingCommissions)
        {
            c.Status = AffiliateCommissionStatus.PaidOut;
            c.PayoutId = payout.Id;
        }

        partner.PendingPayout -= totalAmount;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payout of ${Amount} scheduled for partner {PartnerId}", totalAmount, partnerId);

        return Ok(new
        {
            payoutId = payout.Id,
            amount = totalAmount,
            commissionsIncluded = pendingCommissions.Count,
            status = payout.Status.ToString()
        });
    }

    /// <summary>
    /// Approve a pending commission
    /// </summary>
    [HttpPost("commissions/{commissionId}/approve")]
    public async Task<IActionResult> ApproveCommission(Guid commissionId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var commission = await _context.AffiliateCommissions
            .FirstOrDefaultAsync(c => c.Id == commissionId && c.TenantId == tenantId.Value);

        if (commission == null) return NotFound();
        if (commission.Status != AffiliateCommissionStatus.Pending)
            return BadRequest(new { error = "Only pending commissions can be approved." });

        commission.Status = AffiliateCommissionStatus.Approved;
        commission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, commissionId, status = "Approved" });
    }

    /// <summary>
    /// Void a commission
    /// </summary>
    [HttpPost("commissions/{commissionId}/void")]
    public async Task<IActionResult> VoidCommission(Guid commissionId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var commission = await _context.AffiliateCommissions
            .FirstOrDefaultAsync(c => c.Id == commissionId && c.TenantId == tenantId.Value);

        if (commission == null) return NotFound();
        if (commission.Status == AffiliateCommissionStatus.PaidOut)
            return BadRequest(new { error = "Cannot void a commission that has already been paid out." });

        // Reverse the partner's pending payout
        var partner = await _context.PartnerAccounts.FindAsync(commission.PartnerAccountId);
        if (partner != null)
        {
            partner.PendingPayout -= commission.CommissionAmount;
            partner.TotalEarnings -= commission.CommissionAmount;
        }

        commission.Status = AffiliateCommissionStatus.Voided;
        commission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, commissionId, status = "Voided" });
    }

    /// <summary>
    /// Get affiliate analytics dashboard
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var partners = await _context.PartnerAccounts
            .Where(p => p.TenantId == tenantId.Value && !p.IsDeleted)
            .ToListAsync();

        var commissions = await _context.AffiliateCommissions
            .Where(c => c.TenantId == tenantId.Value)
            .ToListAsync();

        var payouts = await _context.AffiliatePayouts
            .Where(p => partners.Select(pp => pp.Id).Contains(p.PartnerAccountId))
            .ToListAsync();

        return Ok(new
        {
            totalPartners = partners.Count,
            activePartners = partners.Count(p => p.Status == "Active"),
            totalCommissionsEarned = commissions.Sum(c => c.CommissionAmount),
            pendingCommissions = commissions.Where(c => c.Status == AffiliateCommissionStatus.Pending).Sum(c => c.CommissionAmount),
            approvedCommissions = commissions.Where(c => c.Status == AffiliateCommissionStatus.Approved).Sum(c => c.CommissionAmount),
            totalPaidOut = payouts.Where(p => p.Status == AffiliatePayoutStatus.Completed).Sum(p => p.Amount),
            scheduledPayouts = payouts.Where(p => p.Status == AffiliatePayoutStatus.Scheduled).Sum(p => p.Amount),
            topPartners = partners
                .OrderByDescending(p => p.TotalEarnings)
                .Take(5)
                .Select(p => new { p.Id, p.PartnerName, p.TotalEarnings, p.ManagedAccounts })
                .ToList()
        });
    }
}

public class RecordCommissionRequest
{
    public Guid? PaymentId { get; set; }
    public string Source { get; set; } = "Subscription";
    public decimal GrossAmount { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
}
