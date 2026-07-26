using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Job to automatically award loyalty points for completed bookings
/// </summary>
public class LoyaltyPointsJob
{
    private readonly AppDbContext _context;
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILogger<LoyaltyPointsJob> _logger;

    public LoyaltyPointsJob(
        AppDbContext context,
        ILoyaltyService loyaltyService,
        ILogger<LoyaltyPointsJob> logger)
    {
        _context = context;
        _loyaltyService = loyaltyService;
        _logger = logger;
    }

    /// <summary>
    /// Award loyalty points for recently completed bookings
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting loyalty points job");

        // Find recently completed bookings that haven't been processed for loyalty points
        var now = DateTime.UtcNow;
        var windowStart = now.AddDays(-1);

        var completedBookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Where(b => b.Status == BookingStatus.Completed &&
                        b.EndTime >= windowStart &&
                        b.Price.HasValue &&
                        b.Price.Value > 0 &&
                        b.ClientId.HasValue)
            .ToListAsync();

        int processed = 0;

        foreach (var booking in completedBookings)
        {
            try
            {
                // Check if already processed via metadata
                if (booking.Metadata.ContainsKey("loyalty_points_awarded"))
                    continue;

                var points = await _loyaltyService.CalculatePointsAsync(booking.Price!.Value);
                
                if (points > 0)
                {
                    var reason = $"Booking: {booking.Service?.Name ?? "Service"} on {booking.StartTime:yyyy-MM-dd}";
                    await _loyaltyService.AwardPointsAsync(booking.ClientId!.Value, points, reason);

                    // Mark as processed
                    booking.Metadata["loyalty_points_awarded"] = points;
                    booking.Metadata["loyalty_points_awarded_at"] = DateTime.UtcNow.ToString("o");
                    
                    processed++;
                    _logger.LogInformation("Awarded {Points} points to client {ClientId} for booking {BookingId}",
                        points, booking.ClientId, booking.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to award loyalty points for booking {BookingId}", booking.Id);
            }
        }

        if (processed > 0)
        {
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Loyalty points job completed. Processed: {Count}", processed);
    }
}
