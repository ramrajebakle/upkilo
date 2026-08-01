using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Attributes;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[AllowGracefulDegradation]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DashboardController> _logger;
    private readonly ICacheService _cache;

    public DashboardController(AppDbContext context, ITenantProvider tenantProvider, ILogger<DashboardController> logger, ICacheService cache)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _cache = cache;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() ?? Guid.Empty;

    /// <summary>
    /// Get revenue metrics
    /// </summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        // Use Read Model for time-series data
        var dailyMetrics = await _context.TenantDailyMetrics
            .Where(m => m.Date >= startDate.Date && m.Date <= endDate.Date)
            .OrderBy(m => m.Date)
            .Select(m => new { date = m.Date, revenue = m.Revenue })
            .ToListAsync();

        // Stats summary from Read Model
        var stats = await _context.TenantDashboardStats.FirstOrDefaultAsync();

        return Ok(new
        {
            period = new { from = startDate, to = endDate },
            totalRevenue = stats?.TotalRevenue ?? 0,
            revenueThisMonth = stats?.RevenueThisMonth ?? 0,
            dailyRevenue = dailyMetrics
        });
    }

    /// <summary>
    /// Get booking analytics
    /// </summary>
    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;
        var periodDuration = endDate - startDate;
        var prevStartDate = startDate - periodDuration;
        var prevEndDate = startDate;

        // Aggregate server-side — avoid loading all booking rows into memory
        var totalBookings = await _context.Bookings
            .CountAsync(b => b.StartTime >= startDate && b.StartTime <= endDate);

        var prevBookingsCount = await _context.Bookings
            .CountAsync(b => b.StartTime >= prevStartDate && b.StartTime <= prevEndDate);

        decimal trendPercentage = prevBookingsCount == 0
            ? (totalBookings > 0 ? 100M : 0M)
            : Math.Round(((totalBookings - (decimal)prevBookingsCount) / prevBookingsCount) * 100M, 2);

        var byStatus = await _context.Bookings
            .Where(b => b.StartTime >= startDate && b.StartTime <= endDate)
            .GroupBy(b => b.Status)
            .Select(g => new { status = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        var peakDays = await _context.Bookings
            .Where(b => b.StartTime >= startDate && b.StartTime <= endDate)
            .GroupBy(b => b.StartTime.DayOfWeek)
            .Select(g => new { day = g.Key.ToString(), bookings = g.Count() })
            .OrderByDescending(x => x.bookings)
            .ToListAsync();

        return Ok(new
        {
            period = new { from = startDate, to = endDate },
            totalBookings,
            trendPercentage,
            byStatus = byStatus.ToDictionary(x => x.status.ToLower(), x => x.count),
            peakDays
        });
    }

    /// <summary>
    /// Get client analytics
    /// </summary>
    [HttpGet("clients")]
    public async Task<IActionResult> GetClients([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;
        var periodDuration = endDate - startDate;
        var prevStartDate = startDate - periodDuration;
        var prevEndDate = startDate;

        var totalClients = await _context.Clients.CountAsync();
        var newClientsCount = await _context.Clients
            .CountAsync(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate);

        var prevNewClientsCount = await _context.Clients
            .CountAsync(c => c.CreatedAt >= prevStartDate && c.CreatedAt <= prevEndDate);

        decimal trendPercentage = prevNewClientsCount == 0
            ? (newClientsCount > 0 ? 100M : 0M)
            : Math.Round(((newClientsCount - (decimal)prevNewClientsCount) / prevNewClientsCount) * 100M, 2);

        var topClients = await _context.Clients
            .OrderByDescending(c => c.LifetimeValue)
            .Take(5)
            .Select(c => new { name = c.FirstName + " " + c.LastName, c.LifetimeValue })
            .ToListAsync();

        return Ok(new
        {
            period = new { from = startDate, to = endDate },
            totalClients,
            newClientsCount,
            trendPercentage,
            topClients
        });
    }

    /// <summary>
    /// Get staff performance metrics
    /// </summary>
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaffPerformance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        // GroupJoin produces a single SQL query instead of 2 correlated subqueries per staff member.
        var bookingsByStaff = await _context.Bookings
            .Where(b => b.StartTime >= startDate && b.StartTime <= endDate && b.StaffId != null)
            .GroupBy(b => b.StaffId!)
            .Select(g => new
            {
                StaffId = g.Key,
                BookingCount = g.Count(),
                Revenue = g.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => (decimal?)b.Price) ?? 0
            })
            .ToListAsync();

        var staffLookup = bookingsByStaff.ToDictionary(x => x.StaffId ?? Guid.Empty);

        var staffStats = await _context.StaffMembers
            .Select(s => new { s.Id, s.FirstName, s.LastName })
            .ToListAsync();

        var staffResult = staffStats.Select(s =>
        {
            staffLookup.TryGetValue(s.Id, out var b);
            return new
            {
                s.Id,
                name = s.FirstName + " " + s.LastName,
                bookingsCount = b?.BookingCount ?? 0,
                revenue = b?.Revenue ?? 0
            };
        }).ToList();

        return Ok(new
        {
            period = new { from = startDate, to = endDate },
            staff = staffResult
        });
    }

    /// <summary>
    /// Get recent bookings
    /// </summary>
    [HttpGet("recent-bookings")]
    public async Task<IActionResult> GetRecentBookings([FromQuery] int limit = 5)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .OrderByDescending(b => b.StartTime)
            .Take(limit)
            .Select(b => new
            {
                b.Id,
                clientName = b.Client != null ? b.Client.FirstName + " " + b.Client.LastName : "Unknown",
                clientInitials = b.Client != null
                    ? ((b.Client.FirstName != null && b.Client.FirstName.Length > 0 ? b.Client.FirstName.Substring(0, 1) : "") +
                       (b.Client.LastName != null && b.Client.LastName.Length > 0 ? b.Client.LastName.Substring(0, 1) : ""))
                    : "U",
                serviceName = b.Service != null ? b.Service.Name : "Unknown",
                b.StartTime,
                status = b.Status.ToString().ToLower(),
                amount = b.Price ?? 0
            })
            .ToListAsync();

        return Ok(bookings);
    }

    /// <summary>
    /// Get summary widget data
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var tenantId = GetTenantId();
        var cacheKey = "dashboard_summary";

        var summary = await _cache.GetOrSetAsync(tenantId, cacheKey, async () =>
        {
            var stats = await _context.TenantDashboardStats.FirstOrDefaultAsync();
            var today = DateTime.UtcNow.Date;
            var todayMetric = await _context.TenantDailyMetrics
                .FirstOrDefaultAsync(m => m.Date == today);

            return new
            {
                todayBookings = todayMetric?.BookingCount ?? 0,
                todayRevenue = todayMetric?.Revenue ?? 0,
                totalBookings = stats?.TotalBookings ?? 0,
                pendingBookings = stats?.PendingBookings ?? 0,
                completedBookings = stats?.CompletedBookings ?? 0,
                bookingsThisMonth = stats?.BookingsThisMonth ?? 0,
                revenueThisMonth = stats?.RevenueThisMonth ?? 0,
                totalRevenue = stats?.TotalRevenue ?? 0,
                totalClients = stats?.TotalClients ?? 0,
                lastUpdated = stats?.UpdatedAt ?? DateTime.UtcNow
            };
        }, TimeSpan.FromMinutes(5));

        return Ok(summary);
    }
}
