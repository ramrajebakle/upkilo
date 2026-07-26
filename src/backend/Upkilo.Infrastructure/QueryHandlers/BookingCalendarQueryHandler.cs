using MediatR;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces.CQRS;
using Upkilo.Core.Queries;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.QueryHandlers;

/// <summary>
/// CQRS handler for BookingCalendarQuery.
/// Returns a flat, render-ready read model — never mutates state.
/// </summary>
public class BookingCalendarQueryHandler
    : IQueryHandler<BookingCalendarQuery, BookingCalendarReadModel>
{
    private readonly AppDbContext _db;

    public BookingCalendarQueryHandler(AppDbContext db) => _db = db;

    public async Task<BookingCalendarReadModel> Handle(
        BookingCalendarQuery query,
        CancellationToken cancellationToken)
    {
        var q = _db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == query.TenantId
                     && b.StartTime >= query.From
                     && b.StartTime < query.To);

        if (!string.IsNullOrEmpty(query.Status) &&
            Enum.TryParse<BookingStatus>(query.Status, true, out var statusEnum))
            q = q.Where(b => b.Status == statusEnum);

        if (!string.IsNullOrEmpty(query.StaffId) &&
            Guid.TryParse(query.StaffId, out var staffGuid))
            q = q.Where(b => b.StaffId == staffGuid);

        if (!string.IsNullOrEmpty(query.ServiceId) &&
            Guid.TryParse(query.ServiceId, out var serviceGuid))
            q = q.Where(b => b.ServiceId == serviceGuid);

        var bookings = await q
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                b.CustomerName,
                b.CustomerEmail,
                b.ServiceName,
                b.StaffName,
                b.StartTime,
                b.EndTime,
                b.Status,
                b.Price,
                b.Notes,
            })
            .ToListAsync(cancellationToken);

        // Assign deterministic colors per staff/service using a simple hash palette
        var palette = new[]
        {
            "#3B82F6", "#10B981", "#8B5CF6", "#F59E0B", "#EF4444",
            "#06B6D4", "#EC4899", "#14B8A6", "#F97316", "#6366F1",
        };
        var staffColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var serviceColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string GetColor(string key, Dictionary<string, string> map)
        {
            if (!map.TryGetValue(key, out var color))
            {
                color = palette[map.Count % palette.Length];
                map[key] = color;
            }
            return color;
        }

        var items = bookings.Select(b =>
        {
            var clientName = b.CustomerName ?? "Unknown";
            var initials = string.Concat(clientName.Split(' ').Take(2).Select(w => w.Length > 0 ? w[0].ToString() : ""));

            return new CalendarBookingItem
            {
                Id = b.Id.ToString(),
                ClientName = clientName,
                ClientInitials = initials,
                ClientEmail = b.CustomerEmail,
                ServiceName = b.ServiceName ?? "Unknown Service",
                ServiceColor = string.IsNullOrEmpty(b.ServiceName) ? null : GetColor(b.ServiceName, serviceColorMap),
                StaffName = b.StaffName ?? "Unassigned",
                StaffColor = string.IsNullOrEmpty(b.StaffName) ? null : GetColor(b.StaffName, staffColorMap),
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                DurationMinutes = (int)(b.EndTime - b.StartTime).TotalMinutes,
                Status = b.Status.ToString().ToLower(),
                Price = b.Price ?? 0m,
                Notes = b.Notes,
            };
        }).ToList();

        return new BookingCalendarReadModel
        {
            Bookings = items,
            Total = items.Count,
        };
    }
}
