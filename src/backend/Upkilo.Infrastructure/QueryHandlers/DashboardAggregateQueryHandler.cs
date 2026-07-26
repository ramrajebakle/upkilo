using MediatR;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces.CQRS;
using Upkilo.Core.Queries;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.QueryHandlers;

/// <summary>
/// CQRS handler for DashboardAggregateQuery.
/// Builds pre-aggregated KPIs for the dashboard without touching OLTP hot-paths.
/// </summary>
public class DashboardAggregateQueryHandler
    : IQueryHandler<DashboardAggregateQuery, DashboardAggregateReadModel>
{
    private readonly AppDbContext _db;

    public DashboardAggregateQueryHandler(AppDbContext db) => _db = db;

    public async Task<DashboardAggregateReadModel> Handle(
        DashboardAggregateQuery query,
        CancellationToken cancellationToken)
    {
        var (from, priorFrom, priorTo) = ResolvePeriod(query.Period);
        var now = DateTime.UtcNow;

        // ── Current period bookings ────────────────────────────────────────
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == query.TenantId && b.StartTime >= from && b.StartTime <= now)
            .Select(b => new { b.Status, b.Price, b.StartTime })
            .ToListAsync(cancellationToken);

        // ── Prior period bookings (for change %) ──────────────────────────
        var priorBookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == query.TenantId && b.StartTime >= priorFrom && b.StartTime < priorTo)
            .Select(b => new { b.Status, b.Price })
            .ToListAsync(cancellationToken);

        var completed = bookings.Where(b => b.Status == BookingStatus.Completed).ToList();
        var cancelled = bookings.Where(b => b.Status == BookingStatus.Cancelled).ToList();
        var noShow = bookings.Where(b => b.Status == BookingStatus.NoShow).ToList();

        var totalRevenue = completed.Sum(b => b.Price ?? 0m);
        var priorRevenue = priorBookings
            .Where(b => b.Status == BookingStatus.Completed)
            .Sum(b => b.Price ?? 0m);
        var revenueChange = priorRevenue > 0
            ? Math.Round((double)((totalRevenue - priorRevenue) / priorRevenue * 100), 1)
            : 0;

        // ── Clients ───────────────────────────────────────────────────────
        var allClients = await _db.Clients
            .AsNoTracking()
            .Where(c => c.TenantId == query.TenantId && !c.IsDeleted)
            .Select(c => new { c.CreatedAt, c.Id })
            .ToListAsync(cancellationToken);

        var newClients = allClients.Count(c => c.CreatedAt >= from);
        var returningClientIds = bookings
            .Select(b => b.StartTime) // proxy: clients with >1 booking window
            .Distinct()
            .Count();

        // ── Staff utilization ───────────────────────────────────────��─────
        var activeStaff = await _db.Staff
            .AsNoTracking()
            .CountAsync(s => s.TenantId == query.TenantId && s.IsActive, cancellationToken);

        // ── Daily time series ────────────────────────────────────────���────
        var revenueByDay = bookings
            .Where(b => b.Status == BookingStatus.Completed)
            .GroupBy(b => b.StartTime.Date.ToString("yyyy-MM-dd"))
            .Select(g => new DailyRevenueSample(g.Key, g.Sum(b => b.Price ?? 0m), g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        var bookingsByDay = bookings
            .GroupBy(b => b.StartTime.Date.ToString("yyyy-MM-dd"))
            .Select(g => new DailyBookingSample(
                g.Key,
                g.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed),
                g.Count(b => b.Status == BookingStatus.Cancelled),
                g.Count(b => b.Status == BookingStatus.NoShow)))
            .OrderBy(d => d.Date)
            .ToList();

        // ── Top services (from DB with actual service names) ──────────

        // Get actual service names from database
        var serviceStats = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == query.TenantId && b.StartTime >= from && b.Status == BookingStatus.Completed)
            .GroupBy(b => new { b.ServiceId, b.ServiceName })
            .Select(g => new TopServiceItem(
                g.Key.ServiceId.ToString() ?? "",
                g.Key.ServiceName ?? "Unknown",
                g.Count(),
                g.Sum(b => b.Price ?? 0m)))
            .OrderByDescending(s => s.Revenue)
            .Take(5)
            .ToListAsync(cancellationToken);

        // ── Top staff ─────────────────────────────────────────────────────
        var staffStats = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == query.TenantId && b.StartTime >= from && b.Status == BookingStatus.Completed)
            .GroupBy(b => new { b.StaffId, b.StaffName })
            .Select(g => new TopStaffItem(
                g.Key.StaffId.ToString() ?? "",
                g.Key.StaffName ?? "Unassigned",
                g.Count(),
                0)) // utilization computed separately if needed
            .OrderByDescending(s => s.BookingCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        // Staff utilization: booked hours / total working hours (assume 8h/day per active staff)
        var completedBookingsForMinutes = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == query.TenantId && b.StartTime >= from && b.Status == BookingStatus.Completed)
            .Select(b => new { b.StartTime, b.EndTime })
            .ToListAsync(cancellationToken);
            
        var totalBookedMinutes = completedBookingsForMinutes.Sum(b => (b.EndTime - b.StartTime).TotalMinutes);
        var daysInPeriod = Math.Max(1, (now - from).Days);
        var totalAvailableMinutes = activeStaff * daysInPeriod * 8 * 60; // 8-hour workday
        var avgUtilization = totalAvailableMinutes > 0
            ? Math.Round((double)totalBookedMinutes / totalAvailableMinutes * 100, 1)
            : 0;

        return new DashboardAggregateReadModel
        {
            TotalRevenue = totalRevenue,
            RevenueChange = (decimal)revenueChange,
            PendingRevenue = bookings
                .Where(b => b.Status == BookingStatus.Confirmed)
                .Sum(b => b.Price ?? 0m),
            TotalBookings = bookings.Count,
            BookingsChange = bookings.Count - priorBookings.Count,
            CompletedBookings = completed.Count,
            CancelledBookings = cancelled.Count,
            NoShowBookings = noShow.Count,
            CancellationRate = bookings.Count > 0
                ? Math.Round((double)cancelled.Count / bookings.Count * 100, 1)
                : 0,
            TotalClients = allClients.Count,
            NewClients = newClients,
            ReturningClients = returningClientIds,
            RetentionRate = allClients.Count > 0
                ? Math.Round((double)(allClients.Count - newClients) / allClients.Count * 100, 1)
                : 0,
            ActiveStaff = activeStaff,
            AvgUtilizationRate = avgUtilization,
            RevenueByDay = revenueByDay,
            BookingsByDay = bookingsByDay,
            TopServices = serviceStats,
            TopStaff = staffStats,
            Period = query.Period,
        };
    }

    private static (DateTime From, DateTime PriorFrom, DateTime PriorTo) ResolvePeriod(string period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            "7d"  => (now.AddDays(-7), now.AddDays(-14), now.AddDays(-7)),
            "90d" => (now.AddDays(-90), now.AddDays(-180), now.AddDays(-90)),
            "ytd" => (new DateTime(now.Year, 1, 1), new DateTime(now.Year - 1, 1, 1), new DateTime(now.Year, 1, 1)),
            _     => (now.AddDays(-30), now.AddDays(-60), now.AddDays(-30)), // 30d default
        };
    }
}
