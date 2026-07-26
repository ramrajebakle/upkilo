using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Core.ReadModels;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.ReadModels;

/// <summary>
/// EF Core implementation of the CQRS read model service for bookings.
/// All queries are read-only projections scoped to a single tenant.
/// Uses AsNoTracking() throughout to avoid write-side change tracking.
/// </summary>
public class BookingReadModelService : IBookingReadModelService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BookingReadModelService> _logger;

    // Estimated working hours per day used for occupancy calculations.
    private const double WorkingHoursPerDay = 8.0;

    public BookingReadModelService(AppDbContext db, ILogger<BookingReadModelService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Calendar
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<IReadOnlyList<BookingCalendarItem>> GetCalendarAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        Guid? staffId = null,
        Guid? serviceId = null,
        CancellationToken ct = default)
    {
        var query = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Where(b => b.TenantId == tenantId
                        && b.StartTime >= from
                        && b.StartTime < to
                        && b.Status != BookingStatus.Cancelled);

        if (staffId.HasValue)
            query = query.Where(b => b.StaffId == staffId.Value);

        if (serviceId.HasValue)
            query = query.Where(b => b.ServiceId == serviceId.Value);

        var bookings = await query
            .OrderBy(b => b.StartTime)
            .ToListAsync(ct);

        return bookings
            .Select(b => MapToCalendarItem(b))
            .ToList()
            .AsReadOnly();
    }

    // -------------------------------------------------------------------------
    // Dashboard aggregates
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<DashboardAggregates> GetDashboardAggregatesAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var periodLength = to - from;

        // ── Current period ────────────────────────────────────────────────────
        var currentBookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.StartTime >= from && b.StartTime < to)
            .Select(b => new
            {
                b.Status,
                b.Price,
                b.StartTime,
                b.ServiceId,
                b.ServiceName,
                b.StaffId,
                b.StaffName,
                b.ClientId
            })
            .ToListAsync(ct);

        var totalBookings = currentBookings.Count;
        var completedBookings = currentBookings.Count(b => b.Status == BookingStatus.Completed);

        var totalRevenue = currentBookings
            .Where(b => b.Status == BookingStatus.Completed)
            .Sum(b => b.Price ?? 0m);

        // ── Previous period (same length, immediately prior) ─────────────────
        var prevFrom = from - periodLength;
        var prevTo = from;

        var prevBookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.StartTime >= prevFrom && b.StartTime < prevTo)
            .Select(b => new { b.Status, b.Price, b.ClientId })
            .ToListAsync(ct);

        var prevRevenue = prevBookings
            .Where(b => b.Status == BookingStatus.Completed)
            .Sum(b => b.Price ?? 0m);

        var prevTotalBookings = prevBookings.Count;

        var revenueChange = prevRevenue > 0
            ? Math.Round((totalRevenue - prevRevenue) / prevRevenue * 100, 2)
            : 0m;

        var bookingsChange = prevTotalBookings > 0
            ? (int)Math.Round((double)(totalBookings - prevTotalBookings) / prevTotalBookings * 100)
            : 0;

        // ── New clients in current period ────────────────────────────────────
        var newClients = await _db.Clients
            .AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId && c.CreatedAt >= from && c.CreatedAt < to, ct);

        var prevNewClients = await _db.Clients
            .AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId && c.CreatedAt >= prevFrom && c.CreatedAt < prevTo, ct);

        var clientsChange = prevNewClients > 0
            ? (int)Math.Round((double)(newClients - prevNewClients) / prevNewClients * 100)
            : 0;

        // ── Occupancy rate ────────────────────────────────────────────────────
        // occupancy = completedBookings / (staffCount * workingDays * 8h / avgServiceDuration)
        var staffCount = await _db.StaffMembers
            .AsNoTracking()
            .CountAsync(s => s.TenantId == tenantId && s.IsActive, ct);

        var workingDays = Math.Max(1, (int)Math.Ceiling(periodLength.TotalDays));

        // Average service duration (in hours). Fall back to 1 hour if no service data.
        var avgServiceDurationHours = await _db.Services
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .Select(s => (double?)s.DurationMinutes)
            .AverageAsync(ct) ?? 60.0;

        avgServiceDurationHours /= 60.0;

        var totalAvailableSlots = staffCount > 0 && avgServiceDurationHours > 0
            ? staffCount * workingDays * WorkingHoursPerDay / avgServiceDurationHours
            : 1.0;

        var occupancyRate = totalAvailableSlots > 0
            ? Math.Round(completedBookings / totalAvailableSlots * 100, 2)
            : 0.0;

        // ── Revenue trend (group by day) ──────────────────────────────────────
        var revenueTrend = currentBookings
            .Where(b => b.Status == BookingStatus.Completed)
            .GroupBy(b => b.StartTime.Date)
            .Select(g => new RevenueByDay(
                Date: g.Key.ToString("yyyy-MM-dd"),
                Revenue: g.Sum(b => b.Price ?? 0m),
                Bookings: g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        // ── Top 5 services by booking count ───────────────────────────────────
        var topServices = currentBookings
            .Where(b => b.ServiceId.HasValue)
            .GroupBy(b => b.ServiceId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                Name = g.First().ServiceName ?? g.Key.ToString(),
                Bookings = g.Count(),
                Revenue = g.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.Price ?? 0m)
            })
            .OrderByDescending(x => x.Bookings)
            .Take(5)
            .Select(x => new TopService(x.Id, x.Name, x.Bookings, x.Revenue))
            .ToList();

        // ── Staff summaries ───────────────────────────────────────────────────
        var staffSummaries = currentBookings
            .Where(b => b.StaffId.HasValue)
            .GroupBy(b => b.StaffId!.Value)
            .Select(g =>
            {
                var staffBookings = g.Count();
                var staffCompleted = g.Count(b => b.Status == BookingStatus.Completed);
                var staffRevenue = g.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.Price ?? 0m);

                // Per-staff utilization: completedBookings / (workingDays * 8h / avgServiceDurationHours)
                var staffSlots = avgServiceDurationHours > 0
                    ? workingDays * WorkingHoursPerDay / avgServiceDurationHours
                    : 1.0;

                var utilization = staffSlots > 0
                    ? Math.Round(staffCompleted / staffSlots * 100, 2)
                    : 0.0;

                return new StaffSummary(
                    Id: g.Key,
                    Name: g.First().StaffName ?? g.Key.ToString(),
                    Bookings: staffBookings,
                    Revenue: staffRevenue,
                    UtilizationRate: utilization);
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return new DashboardAggregates(
            TotalRevenue: totalRevenue,
            TotalBookings: totalBookings,
            NewClients: newClients,
            CompletedBookings: completedBookings,
            OccupancyRate: occupancyRate,
            RevenueChange: revenueChange,
            BookingsChange: bookingsChange,
            ClientsChange: clientsChange,
            RevenueTrend: revenueTrend,
            TopServices: topServices,
            StaffSummaries: staffSummaries);
    }

    // -------------------------------------------------------------------------
    // Booking report
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<BookingReport> GetBookingReportAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        Guid? locationId = null,
        CancellationToken ct = default)
    {
        var query = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Where(b => b.TenantId == tenantId
                        && b.StartTime >= from
                        && b.StartTime < to);

        if (locationId.HasValue)
            query = query.Where(b => b.LocationId == locationId.Value);

        var bookings = await query
            .OrderBy(b => b.StartTime)
            .ToListAsync(ct);

        var total = bookings.Count;
        var completed = bookings.Count(b => b.Status == BookingStatus.Completed);
        var cancelled = bookings.Count(b => b.Status == BookingStatus.Cancelled);
        var noShows = bookings.Count(b => b.Status == BookingStatus.NoShow);

        var totalRevenue = bookings
            .Where(b => b.Status == BookingStatus.Completed)
            .Sum(b => b.Price ?? 0m);

        var cancellationRate = total > 0 ? Math.Round((double)cancelled / total * 100, 2) : 0.0;
        var noShowRate = total > 0 ? Math.Round((double)noShows / total * 100, 2) : 0.0;

        var items = bookings
            .Select(b => MapToCalendarItem(b))
            .ToList();

        return new BookingReport(
            TotalBookings: total,
            Completed: completed,
            Cancelled: cancelled,
            NoShows: noShows,
            TotalRevenue: totalRevenue,
            CancellationRate: cancellationRate,
            NoShowRate: noShowRate,
            Items: items);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static BookingCalendarItem MapToCalendarItem(Booking b)
    {
        // Resolve client name: prefer linked Client entity, then denormalized fields.
        var clientName = b.Client is not null
            ? $"{b.Client.FirstName} {b.Client.LastName}".Trim()
            : b.CustomerName ?? "Guest";

        // Resolve service name: prefer linked Service entity, then denormalized field.
        var serviceName = b.Service?.Name ?? b.ServiceName ?? "Unknown Service";

        // Resolve staff name: prefer linked Staff entity, then denormalized field.
        var staffName = b.Staff is not null
            ? $"{b.Staff.FirstName} {b.Staff.LastName}".Trim()
            : b.StaffName;

        // Calendar color comes from the Service entity; fall back to Indigo.
        var color = b.Service?.Color ?? "#6366F1";

        return new BookingCalendarItem(
            Id: b.Id,
            ClientName: clientName,
            ServiceName: serviceName,
            StaffName: staffName,
            Color: color,
            StartTime: b.StartTime,
            EndTime: b.EndTime,
            Status: b.Status.ToString(),
            Price: b.Price ?? 0m,
            IsWalkIn: b.IsWalkIn);
    }
}
