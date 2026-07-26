using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Hangfire job that releases expired slot holds.
/// When a client starts the booking flow, a slot is temporarily held
/// for 15 minutes. If they don't complete the booking, this job
/// releases the hold so other clients can book that time.
/// Runs every 2 minutes to prevent inventory starvation.
/// </summary>
public class SlotExpiryJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlotExpiryJob> _logger;

    public SlotExpiryJob(IServiceScopeFactory scopeFactory, ILogger<SlotExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        // 1. Release expired slot holds (15-minute TTL)
        var expiredHolds = await context.SlotHolds
            .Where(h => h.ExpiresAt < now && !h.IsConverted)
            .ToListAsync();

        if (expiredHolds.Count > 0)
        {
            context.SlotHolds.RemoveRange(expiredHolds);
            _logger.LogInformation("Released {Count} expired slot holds", expiredHolds.Count);
        }

        // 2. Cancel bookings stuck in "Pending" status for > 30 minutes
        var pendingThreshold = now.AddMinutes(-30);
        var staleBookings = await context.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.CreatedAt < pendingThreshold)
            .ToListAsync();

        foreach (var booking in staleBookings)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.Notes = (booking.Notes ?? "") + " [Auto-cancelled: confirmation timeout exceeded]";
            booking.UpdatedAt = now;
        }

        if (staleBookings.Count > 0)
        {
            _logger.LogInformation("Auto-cancelled {Count} stale pending bookings (>30min)", staleBookings.Count);
        }

        if (expiredHolds.Count > 0 || staleBookings.Count > 0)
        {
            await context.SaveChangesAsync();
        }
    }
}
