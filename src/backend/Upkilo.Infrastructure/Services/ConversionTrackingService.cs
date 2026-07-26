using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class ConversionTrackingService : IConversionTrackingService
{
    private readonly AppDbContext _context;

    public ConversionTrackingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task TrackEventAsync(Guid tenantId, ConversionEvent evt)
    {
        evt.Id = Guid.NewGuid();
        evt.TenantId = tenantId;
        evt.CreatedAt = DateTime.UtcNow;
        evt.IsBilled = false;
        _context.ConversionEvents.Add(evt);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            when (ex.InnerException?.Message.Contains("duplicate key") == true ||
                  ex.InnerException?.Message.Contains("unique constraint") == true)
        {
            // Concurrent duplicate — idempotent, treat as success.
            _context.Entry(evt).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }
    }

    public async Task<IEnumerable<ConversionEvent>> GetEventsAsync(Guid tenantId, DateTime from, DateTime to)
    {
        return await _context.ConversionEvents
            .Where(e => e.TenantId == tenantId && e.CreatedAt >= from && e.CreatedAt <= to)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<ConversionSummaryDto> GetSummaryAsync(Guid tenantId, DateTime from, DateTime to)
    {
        // Push aggregation to the DB to avoid loading millions of rows into memory.
        var baseQuery = _context.ConversionEvents
            .Where(e => e.TenantId == tenantId && e.CreatedAt >= from && e.CreatedAt <= to);

        var totals = await baseQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalEvents     = g.Count(),
                TotalRevenue    = g.Sum(e => e.Revenue ?? 0),
                BilledRevenue   = g.Sum(e => e.IsBilled ? e.Revenue ?? 0 : 0),
                UnbilledRevenue = g.Sum(e => !e.IsBilled ? e.Revenue ?? 0 : 0),
                LeadsCaptured   = g.Count(e => e.EventCategory == "lead"),
                UnbilledLeads   = g.Count(e => e.EventCategory == "lead" && !e.IsBilled),
                BookingsFromAds = g.Count(e => e.EventCategory == "booking" && e.Platform != "Organic")
            })
            .FirstOrDefaultAsync();

        var byPlatform = await baseQuery
            .Where(e => e.Platform != null)
            .GroupBy(e => e.Platform!)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var bySource = await baseQuery
            .Where(e => e.Source != null)
            .GroupBy(e => e.Source!)
            .Select(g => new { g.Key, Revenue = g.Sum(e => e.Revenue ?? 0) })
            .ToDictionaryAsync(x => x.Key, x => x.Revenue);

        return new ConversionSummaryDto
        {
            TotalEvents      = totals?.TotalEvents ?? 0,
            TotalRevenue     = totals?.TotalRevenue ?? 0,
            BilledRevenue    = totals?.BilledRevenue ?? 0,
            UnbilledRevenue  = totals?.UnbilledRevenue ?? 0,
            LeadsCaptured    = totals?.LeadsCaptured ?? 0,
            UnbilledLeads    = totals?.UnbilledLeads ?? 0,
            BookingsFromAds  = totals?.BookingsFromAds ?? 0,
            EventsByPlatform = byPlatform,
            RevenueBySource  = bySource
        };
    }
}
