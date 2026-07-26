using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class ClientMatchingService
{
    private readonly AppDbContext _context;

    public ClientMatchingService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// For each open slot, find top N lapsed clients who previously booked a similar service.
    /// Ranked by: recency of last booking + service match + booking frequency.
    /// </summary>
    public async Task<List<SlotClientMatch>> FindMatchesAsync(
        Guid tenantId,
        List<OpenSlot> openSlots,
        int topN = 5)
    {
        if (!openSlots.Any()) return new List<SlotClientMatch>();

        // Get all client booking history
        var clientBookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.ClientId.HasValue &&
                        b.Status == BookingStatus.Completed)
            .Include(b => b.Client)
            .Include(b => b.Service)
            .OrderByDescending(b => b.StartTime)
            .AsNoTracking()
            .ToListAsync();

        if (!clientBookings.Any()) return new List<SlotClientMatch>();

        // Build client profile: last booking, total bookings, preferred service, LTV
        var clientProfiles = clientBookings
            .GroupBy(b => b.ClientId!.Value)
            .Select(g => new ClientProfile
            {
                ClientId = g.Key,
                Client = g.First().Client!,
                LastBookingAt = g.Max(b => b.StartTime),
                TotalBookings = g.Count(),
                PreferredServiceId = g.GroupBy(b => b.ServiceId)
                                      .OrderByDescending(sg => sg.Count())
                                      .First().Key,
                PreferredServiceName = g.GroupBy(b => b.ServiceName)
                                        .OrderByDescending(sg => sg.Count())
                                        .First().Key ?? "Unknown",
                LifetimeValue = g.Sum(b => b.Price ?? 0),
                DaysSinceLastBooking = (int)(DateTime.UtcNow - g.Max(b => b.StartTime)).TotalDays
            })
            .Where(p => p.Client != null &&
                        p.DaysSinceLastBooking >= 21 && // Only lapsed clients
                        p.TotalBookings >= 1)
            .OrderBy(p => p.DaysSinceLastBooking) // Most recent lapsed first
            .ToList();

        var results = new List<SlotClientMatch>();

        // Group slots by duration to match services
        var slotGroups = openSlots
            .GroupBy(s => s.DurationMinutes / 30) // Group by 30-min buckets
            .ToList();

        foreach (var slot in openSlots.Take(20)) // Cap at 20 slots to avoid over-generation
        {
            var matchedClients = clientProfiles
                .Where(p => p.Client.SmsConsent || !string.IsNullOrEmpty(p.Client.Email))
                .OrderByDescending(p => Score(p, slot))
                .Take(topN)
                .Select(p => new MatchedClient
                {
                    ClientId = p.ClientId,
                    Name = $"{p.Client.FirstName} {p.Client.LastName}".Trim(),
                    Phone = p.Client.Phone,
                    Email = p.Client.Email,
                    LastServiceName = p.PreferredServiceName,
                    DaysSinceLastVisit = p.DaysSinceLastBooking,
                    TotalBookings = p.TotalBookings,
                    LifetimeValue = p.LifetimeValue,
                    HasSmsConsent = p.Client.SmsConsent,
                    Score = Score(p, slot)
                })
                .ToList();

            if (matchedClients.Any())
            {
                results.Add(new SlotClientMatch
                {
                    Slot = slot,
                    MatchedClients = matchedClients
                });
            }
        }

        return results;
    }

    private static int Score(ClientProfile p, OpenSlot slot)
    {
        var score = 0;
        // Recency bonus (closer to 30-90 days is prime re-engagement window)
        if (p.DaysSinceLastBooking is >= 21 and <= 90) score += 50;
        else if (p.DaysSinceLastBooking <= 120) score += 30;

        // Frequency bonus
        score += Math.Min(p.TotalBookings * 5, 30);

        // LTV bonus
        score += (int)Math.Min(p.LifetimeValue / 10, 20);

        return score;
    }

    private class ClientProfile
    {
        public Guid ClientId { get; set; }
        public Client Client { get; set; } = null!;
        public DateTime LastBookingAt { get; set; }
        public int TotalBookings { get; set; }
        public Guid? PreferredServiceId { get; set; }
        public string PreferredServiceName { get; set; } = string.Empty;
        public decimal LifetimeValue { get; set; }
        public int DaysSinceLastBooking { get; set; }
    }
}

public class SlotClientMatch
{
    public OpenSlot Slot { get; set; } = null!;
    public List<MatchedClient> MatchedClients { get; set; } = new();
}

public class MatchedClient
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string LastServiceName { get; set; } = string.Empty;
    public int DaysSinceLastVisit { get; set; }
    public int TotalBookings { get; set; }
    public decimal LifetimeValue { get; set; }
    public bool HasSmsConsent { get; set; }
    public int Score { get; set; }
}
