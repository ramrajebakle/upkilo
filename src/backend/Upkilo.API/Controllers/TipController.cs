using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;


namespace Upkilo.API.Controllers;

/// <summary>
/// Tip/gratuity management for staff.
/// Supports adding tips to bookings, viewing tip reports per staff,
/// and managing tip distribution/payouts.
/// </summary>
[ApiController]
[Route("api/v1/tips")]
[Authorize]
[ApiVersion("1.0")]
public class TipController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TipController> _logger;

    public TipController(AppDbContext context, ITenantProvider tenantProvider, ILogger<TipController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// List tips for the current tenant, newest first. Optionally filtered by staff member
    /// and date range.
    /// </summary>
    /// <remarks>
    /// Backs the Tips page, which needs the individual rows (it derives totals and top-earner
    /// itself). Only aggregate (`/summary`) and per-staff (`/staff/{id}`) reads existed, so a
    /// GET on this route matched the POST-only action and returned 405.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTips(
        [FromQuery] Guid? staffId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        // Query-string dates bind with Kind=Unspecified; Npgsql rejects those for timestamptz.
        var from = Normalize(startDate) ?? DateTime.UtcNow.AddDays(-30);
        var to = Normalize(endDate) ?? DateTime.UtcNow;

        var query = _context.Set<Tip>()
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.CreatedAt >= from && t.CreatedAt <= to);

        if (staffId.HasValue)
            query = query.Where(t => t.StaffId == staffId.Value);

        var total = await query.CountAsync();

        var tips = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                id = t.Id,
                amount = t.Amount,
                type = t.Type.ToString(),
                paymentMethod = t.PaymentMethod,
                isDistributed = t.IsDistributed,
                bookingId = t.BookingId,
                staffId = t.StaffId,
                staffName = _context.StaffMembers
                    .Where(s => s.Id == t.StaffId)
                    .Select(s => s.FirstName + " " + s.LastName)
                    .FirstOrDefault(),
                createdAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = tips, page, pageSize, total });
    }

    private static DateTime? Normalize(DateTime? value) => value is null
        ? null
        : value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };

    /// <summary>
    /// Add a tip to a booking
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Owner,Admin,Staff")]
    public async Task<IActionResult> AddTip([FromBody] AddTipRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // SECURITY (M-5): Validate tip amount
        if (request.Amount <= 0)
            return BadRequest("Tip amount must be positive");
        if (request.Amount > 10_000)
            return BadRequest("Tip amount exceeds maximum allowed");

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.TenantId == tenantId);
        if (booking == null) return NotFound("Booking not found");

        var price = booking.Price ?? 0m;
        var amount = request.Type == TipType.Percentage
            ? price * (request.Amount / 100m)
            : request.Amount;

        var tip = new Tip
        {
            TenantId = tenantId.Value,
            BookingId = request.BookingId,
            StaffId = request.StaffId ?? booking.StaffId ?? Guid.Empty,
            ClientId = booking.ClientId,
            Amount = Math.Round(amount, 2),
            Type = request.Type,
            Percentage = request.Type == TipType.Percentage ? request.Amount : null,
            PaymentMethod = request.PaymentMethod ?? "card"
        };

        _context.Set<Tip>().Add(tip);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tip of {Amount} added to booking {BookingId} for staff {StaffId}",
            tip.Amount, tip.BookingId, tip.StaffId);

        return CreatedAtAction(nameof(GetTip), new { id = tip.Id }, tip);
    }

    /// <summary>
    /// Get a specific tip
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTip(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tip = await _context.Set<Tip>()
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);

        if (tip == null) return NotFound();
        return Ok(tip);
    }

    /// <summary>
    /// Get tip report for a staff member with date range
    /// </summary>
    [HttpGet("staff/{staffId}")]
    public async Task<IActionResult> GetStaffTips(
        Guid staffId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var tips = await _context.Set<Tip>()
            .Where(t => t.TenantId == tenantId && t.StaffId == staffId
                        && t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(new
        {
            staffId,
            dateRange = new { from = fromDate, to = toDate },
            totalTips = tips.Sum(t => t.Amount),
            tipCount = tips.Count,
            averageTip = tips.Count > 0 ? Math.Round(tips.Average(t => t.Amount), 2) : 0,
            undistributed = tips.Where(t => !t.IsDistributed).Sum(t => t.Amount),
            byPaymentMethod = tips.GroupBy(t => t.PaymentMethod)
                .Select(g => new { method = g.Key, total = g.Sum(t => t.Amount), count = g.Count() }),
            tips = tips.Select(t => new
            {
                t.Id,
                t.Amount,
                t.Type,
                t.PaymentMethod,
                t.IsDistributed,
                t.CreatedAt,
                t.BookingId
            })
        });
    }

    /// <summary>
    /// Mark tips as distributed (paid out to staff)
    /// SECURITY (H-5): Restricted to Owner/Admin — financial payout operation.
    /// </summary>
    [HttpPost("distribute")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> DistributeTips([FromBody] DistributeTipsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.TipIds == null || request.TipIds.Count == 0)
            return BadRequest("No tip IDs provided");

        var tips = await _context.Set<Tip>()
            .Where(t => request.TipIds.Contains(t.Id) && t.TenantId == tenantId && !t.IsDistributed)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var tip in tips)
        {
            tip.IsDistributed = true;
            tip.DistributedAt = now;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            distributedCount = tips.Count,
            totalAmount = tips.Sum(t => t.Amount),
            distributedAt = now
        });
    }

    /// <summary>
    /// Get tip summary for the entire business (date range)
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetTipSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var tips = await _context.Set<Tip>()
            .Where(t => t.TenantId == tenantId && t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
            .ToListAsync();

        return Ok(new
        {
            dateRange = new { from = fromDate, to = toDate },
            totalTips = tips.Sum(t => t.Amount),
            tipCount = tips.Count,
            averageTip = tips.Count > 0 ? Math.Round(tips.Average(t => t.Amount), 2) : 0,
            distributedTotal = tips.Where(t => t.IsDistributed).Sum(t => t.Amount),
            pendingTotal = tips.Where(t => !t.IsDistributed).Sum(t => t.Amount),
            byStaff = tips.GroupBy(t => t.StaffId)
                .Select(g => new
                {
                    staffId = g.Key,
                    total = g.Sum(t => t.Amount),
                    count = g.Count(),
                    average = Math.Round(g.Average(t => t.Amount), 2)
                })
                .OrderByDescending(x => x.total)
        });
    }
}

public class AddTipRequest
{
    public Guid BookingId { get; set; }
    public Guid? StaffId { get; set; }
    public decimal Amount { get; set; }
    public TipType Type { get; set; } = TipType.Flat;
    public string? PaymentMethod { get; set; }
}

public class DistributeTipsRequest
{
    public List<Guid> TipIds { get; set; } = new();
}
