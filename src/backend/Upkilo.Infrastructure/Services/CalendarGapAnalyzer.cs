using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class CalendarGapAnalyzer
{
    private readonly AppDbContext _context;

    public CalendarGapAnalyzer(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns open time slots (gaps between bookings) for next N days per staff member.
    /// A "gap" is a window of at least minGapMinutes that has no booking.
    /// </summary>
    public async Task<List<OpenSlot>> GetOpenSlotsAsync(Guid tenantId, int daysAhead = 7, int minGapMinutes = 30)
    {
        var now = DateTime.UtcNow;
        var windowEnd = now.AddDays(daysAhead);

        // Get all confirmed/pending bookings in window
        var bookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.StartTime >= now &&
                        b.StartTime <= windowEnd &&
                        b.Status != BookingStatus.Cancelled &&
                        b.StaffId.HasValue)
            .OrderBy(b => b.StaffId)
            .ThenBy(b => b.StartTime)
            .ToListAsync();

        // Get staff working hours (assume 9-18 if no schedule set)
        var staffIds = bookings.Select(b => b.StaffId!.Value).Distinct().ToList();

        var staff = await _context.Staff
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync();

        var openSlots = new List<OpenSlot>();
        var defaultStart = TimeSpan.FromHours(9);
        var defaultEnd = TimeSpan.FromHours(18);

        foreach (var staffMember in staff)
        {
            var staffBookings = bookings
                .Where(b => b.StaffId == staffMember.Id)
                .OrderBy(b => b.StartTime)
                .ToList();

            for (var date = now.Date; date <= windowEnd.Date; date = date.AddDays(1))
            {
                var dayStart = date + defaultStart;
                var dayEnd = date + defaultEnd;

                if (dayStart < now) dayStart = now;
                if (dayStart >= dayEnd) continue;

                var dayBookings = staffBookings
                    .Where(b => b.StartTime.Date == date)
                    .OrderBy(b => b.StartTime)
                    .ToList();

                // Find gaps
                var cursor = dayStart;
                foreach (var booking in dayBookings)
                {
                    if (booking.StartTime > cursor.AddMinutes(minGapMinutes))
                    {
                        openSlots.Add(new OpenSlot
                        {
                            StaffId = staffMember.Id,
                            StaffName = $"{staffMember.FirstName} {staffMember.LastName}".Trim(),
                            Start = cursor,
                            End = booking.StartTime,
                            DurationMinutes = (int)(booking.StartTime - cursor).TotalMinutes
                        });
                    }
                    if (booking.EndTime > cursor) cursor = booking.EndTime;
                }

                // Gap after last booking
                if (cursor < dayEnd.AddMinutes(-minGapMinutes))
                {
                    openSlots.Add(new OpenSlot
                    {
                        StaffId = staffMember.Id,
                        StaffName = $"{staffMember.FirstName} {staffMember.LastName}".Trim(),
                        Start = cursor,
                        End = dayEnd,
                        DurationMinutes = (int)(dayEnd - cursor).TotalMinutes
                    });
                }
            }
        }

        return openSlots.OrderBy(s => s.Start).ToList();
    }
}

public class OpenSlot
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int DurationMinutes { get; set; }
}
