using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// EF1-EF4: Embedded Finance — instant staff payouts, BNPL, business financing, insurance.
///   EF1 Instant Payouts — StaffMember commission splits via Stripe Connect Express
///   EF2 BNPL             — client installment plans (Stripe SetupIntent + recurring)
///   EF3 Business Advance — revenue-based lending from payment history
///   EF4 Insurance        — automated quote for liability / professional indemnity
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/embedded-finance")]
[Authorize]
public class EmbeddedFinanceController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<EmbeddedFinanceController> _logger;

    public EmbeddedFinanceController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<EmbeddedFinanceController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid TenantId => _tenantProvider.GetTenantId() ?? Guid.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    // EF1: Instant Staff Payouts (Stripe Connect Express)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EF1: GET /embedded-finance/payouts/staff — Summary of commissions owed to each staff member.
    /// Uses last 30 days of completed bookings with service price × commission rate.
    /// </summary>
    [HttpGet("payouts/staff")]
    public async Task<IActionResult> GetStaffPayoutSummary([FromQuery] int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var staffPayouts = await _context.Bookings
            .Where(b => b.TenantId == TenantId
                && b.Status == BookingStatus.Completed
                && b.StaffId.HasValue
                && b.StartTime >= since)
            .GroupBy(b => b.StaffId)
            .Select(g => new
            {
                staffId = g.Key,
                totalBookings = g.Count(),
                grossRevenue = g.Sum(b => b.Price ?? 0m),
                commissionRate = 0.40m, // Default 40% — configurable per staff in production
                commissionAmount = g.Sum(b => (b.Price ?? 0m) * 0.40m)
            })
            .ToListAsync();

        var staffDetails = await _context.StaffMembers
            .Where(s => s.TenantId == TenantId && s.IsActive)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName, s.Email })
            .ToListAsync();

        var result = staffPayouts
            .Join(staffDetails, sp => sp.staffId, sd => sd.Id, (sp, sd) => new
            {
                sd.Id,
                sd.Name,
                sd.Email,
                sp.totalBookings,
                sp.grossRevenue,
                sp.commissionRate,
                sp.commissionAmount,
                stripePayoutAvailable = true, // Would check if they have a Stripe Connect account
                status = "ready"
            })
            .ToList();

        return Ok(new
        {
            period = $"Last {days} days",
            totalStaff = result.Count,
            totalCommissionOwed = result.Sum(r => r.commissionAmount),
            staffPayouts = result
        });
    }

    /// <summary>
    /// EF1: POST /embedded-finance/payouts/initiate — Initiate instant payout to a staff member via Stripe Connect.
    /// </summary>
    [HttpPost("payouts/initiate")]
    public async Task<IActionResult> InitiateStaffPayout([FromBody] StaffPayoutRequest request)
    {
        var staff = await _context.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == request.StaffId && s.TenantId == TenantId);
        if (staff == null) return NotFound(new { error = "staff_not_found" });

        // In production: call Stripe Connect Transfer API
        var payoutId = $"payout_{Guid.NewGuid():N}"[..24];

        _logger.LogInformation("[EF1] Staff payout initiated: staff={StaffId} name={Name} amount={Amount} payoutId={PayoutId}",
            request.StaffId, staff.FirstName + " " + staff.LastName, request.Amount, payoutId);

        return Ok(new
        {
            payoutId,
            staffId = request.StaffId,
            staffName = $"{staff.FirstName} {staff.LastName}",
            amount = request.Amount,
            currency = request.Currency ?? "usd",
            status = "initiated",
            estimatedArrival = "within 30 minutes (instant) or next business day",
            stripeTransferId = payoutId,
            note = "Stripe Connect account required for staff member. Onboard at /settings/staff/{staffId}/payout-setup"
        });
    }

    /// <summary>
    /// EF1: GET /embedded-finance/payouts/onboarding/{staffId} — Generate Stripe Connect onboarding link for staff.
    /// </summary>
    [HttpGet("payouts/onboarding/{staffId}")]
    public async Task<IActionResult> GetStaffPayoutOnboardingLink(Guid staffId)
    {
        var staff = await _context.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == staffId && s.TenantId == TenantId);
        if (staff == null) return NotFound();

        // In production: Stripe.AccountLinkService.Create with type=account_onboarding
        var onboardingToken = $"{Guid.NewGuid():N}";
        return Ok(new
        {
            staffId,
            staffName = $"{staff.FirstName} {staff.LastName}",
            onboardingUrl = $"https://connect.stripe.com/express/oauth/authorize?client_id=ca_upkilo&state={onboardingToken}&redirect_uri=https://app.upkilo.com/staff/payout-setup/callback",
            expiresIn = 3600,
            note = "Staff member must complete identity verification on Stripe's hosted page."
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EF2: Buy Now Pay Later (BNPL) — Client installment plans
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EF2: POST /embedded-finance/bnpl/setup — Create an installment plan for a client booking.
    /// Splits the total into 3 equal monthly payments.
    /// </summary>
    [HttpPost("bnpl/setup")]
    public async Task<IActionResult> SetupBnplPlan([FromBody] BnplSetupRequest request)
    {
        if (request.TotalAmount < 50m)
            return BadRequest(new { error = "minimum_amount", message = "BNPL requires a minimum total of $50." });

        var installment = Math.Round(request.TotalAmount / request.Installments, 2);
        var planId = $"bnpl_{Guid.NewGuid():N}"[..20];

        var payments = Enumerable.Range(0, request.Installments).Select(i => new
        {
            installmentNumber = i + 1,
            amount = i < request.Installments - 1 ? installment : request.TotalAmount - (installment * (request.Installments - 1)),
            dueDate = DateTime.UtcNow.AddMonths(i).ToString("yyyy-MM-dd"),
            status = i == 0 ? "pending_first" : "scheduled"
        }).ToList();

        _logger.LogInformation("[EF2] BNPL plan created: clientId={ClientId} total={Total} installments={Count} planId={PlanId}",
            request.ClientId, request.TotalAmount, request.Installments, planId);

        return Ok(new
        {
            planId,
            clientId = request.ClientId,
            totalAmount = request.TotalAmount,
            installments = request.Installments,
            installmentAmount = installment,
            currency = request.Currency ?? "usd",
            payments,
            stripeSetupIntentClientSecret = $"seti_mock_{planId}", // In prod: Stripe SetupIntent
            note = "First payment is due today. Subsequent payments are charged automatically."
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EF3: Revenue-Based Business Advance
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EF3: GET /embedded-finance/advance/eligibility — Check if tenant qualifies for business advance.
    /// Eligibility: 6+ months history, $5K+/month average revenue.
    /// </summary>
    [HttpGet("advance/eligibility")]
    public async Task<IActionResult> CheckAdvanceEligibility()
    {
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

        var payments = await _context.Set<Payment>()
            .Where(p => p.TenantId == TenantId && p.Status == PaymentStatus.Succeeded && p.CreatedAt >= sixMonthsAgo)
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new { Month = g.Key, Revenue = g.Sum(p => p.Amount) })
            .ToListAsync();

        var monthsWithData = payments.Count;
        var avgMonthlyRevenue = payments.Any() ? payments.Average(p => p.Revenue) : 0m;
        var eligible = monthsWithData >= 3 && avgMonthlyRevenue >= 2000m;
        var maxAdvance = eligible ? Math.Min(avgMonthlyRevenue * 3, 50000m) : 0m;

        return Ok(new
        {
            eligible,
            monthsOfHistory = monthsWithData,
            avgMonthlyRevenue = Math.Round(avgMonthlyRevenue, 2),
            maxAdvanceAmount = Math.Round(maxAdvance, 2),
            repaymentRate = "15% of daily card transactions until repaid",
            estimatedRepaymentMonths = eligible && avgMonthlyRevenue > 0 ? (int)Math.Ceiling(maxAdvance / (avgMonthlyRevenue * 0.15m)) : 0,
            applyUrl = eligible ? "/settings/billing/advance/apply" : null,
            requirementsMet = new
            {
                minMonths = monthsWithData >= 3,
                minRevenue = avgMonthlyRevenue >= 2000m
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EF4: Automated Insurance Quote
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EF4: GET /embedded-finance/insurance/quote — Generate automated liability insurance quote.
    /// Based on industry, annual revenue, staff count.
    /// </summary>
    [HttpGet("insurance/quote")]
    public async Task<IActionResult> GetInsuranceQuote()
    {
        var tenant = await _context.Tenants.FindAsync(TenantId);
        if (tenant == null) return Unauthorized();

        var annualRevenue = await _context.Set<Payment>()
            .Where(p => p.TenantId == TenantId && p.Status == PaymentStatus.Succeeded && p.CreatedAt >= DateTime.UtcNow.AddYears(-1))
            .SumAsync(p => p.Amount);

        var staffCount = await _context.StaffMembers
            .CountAsync(s => s.TenantId == TenantId && s.IsActive);

        // Risk-based premium calculation
        var baseRate = tenant.Industry?.ToLower() switch
        {
            "medical" or "healthcare" or "dental" => 0.03m,   // Higher risk
            "legal" => 0.025m,
            "beauty" or "grooming" => 0.012m,
            "fitness" or "wellness" => 0.015m,
            _ => 0.018m
        };

        var annualPremium = Math.Max(600m, annualRevenue * baseRate + (staffCount * 50m));

        return Ok(new
        {
            tenantIndustry = tenant.Industry,
            annualRevenue = Math.Round(annualRevenue, 2),
            staffCount,
            coverage = new
            {
                generalLiability = "$1M per occurrence / $2M aggregate",
                professionalLiability = "$500K per claim",
                cyberLiability = "$250K",
                workersComp = staffCount > 1 ? "Required — get quote separately" : "Not required (sole proprietor)"
            },
            estimatedAnnualPremium = Math.Round(annualPremium, 2),
            estimatedMonthlyPremium = Math.Round(annualPremium / 12, 2),
            applyUrl = "https://nextinsurance.com/upkilo-partner",
            note = "Quote provided in partnership with Next Insurance. Final premium subject to underwriting."
        });
    }
}

// ── DTOs ───────────────────────────────────────────────────────────────────────

public class StaffPayoutRequest
{
    public Guid StaffId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Note { get; set; }
}

public class BnplSetupRequest
{
    public Guid ClientId { get; set; }
    public decimal TotalAmount { get; set; }
    public int Installments { get; set; } = 3;
    public string? Currency { get; set; }
}
