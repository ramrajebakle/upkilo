using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Jobs;

public class GoogleCalendarSyncJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<GoogleCalendarSyncJob> _logger;
    private readonly IGoogleCalendarService _googleCalendarService;

    public GoogleCalendarSyncJob(AppDbContext context, ILogger<GoogleCalendarSyncJob> logger, IGoogleCalendarService googleCalendarService)
    {
        _context = context;
        _logger = logger;
        _googleCalendarService = googleCalendarService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Google Calendar Sync background job.");

        // Grab all active tokens
        var tokens = await _context.CalendarSyncTokens
            .Where(t => t.IsActive && t.Provider == "Google")
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            try
            {
                // Refresh token sequence if expired or expiring soon
                if (token.ExpiresAt <= DateTime.UtcNow.AddMinutes(5) && !string.IsNullOrEmpty(token.RefreshToken))
                {
                    _logger.LogInformation("Refreshing OAuth token for Staff {StaffId}", token.StaffId);
                    var refreshed = await _googleCalendarService.RefreshAccessTokenAsync(token.RefreshToken);
                    token.AccessToken = refreshed.AccessToken;
                    token.ExpiresAt = refreshed.ExpiresAt;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                if (string.IsNullOrEmpty(token.AccessToken)) continue;

                if (token.SyncDirection == "TwoWay" || token.SyncDirection == "OneWayUp")
                {
                    var lastSync = token.LastSyncAt ?? DateTime.UtcNow.AddDays(-7);
                    
                    // Find recently updated Bookings for this Staff member and Push to Google
                    var modifiedBookings = await _context.Set<Booking>()
                        .Include(b => b.Client)
                        .Include(b => b.Service)
                        .Where(b => b.StaffId == token.StaffId && b.UpdatedAt >= lastSync)
                        .ToListAsync(cancellationToken);

                    foreach (var booking in modifiedBookings)
                    {
                        // Check if booking is cancelled
                        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.NoShow)
                        {
                            if (!string.IsNullOrEmpty(booking.ExternalId))
                            {
                                await _googleCalendarService.DeleteBookingAsync(token.AccessToken, booking.ExternalId);
                                booking.ExternalId = null;
                            }
                        }
                        else
                        {
                            var newEventId = await _googleCalendarService.PushBookingAsync(token.AccessToken, booking, booking.ExternalId);
                            booking.ExternalId = newEventId;
                        }
                    }
                    _logger.LogInformation("Pushed {Count} events to Google for Staff {StaffId}", modifiedBookings.Count, token.StaffId);
                }

                if (token.SyncDirection == "TwoWay" || token.SyncDirection == "OneWayDown")
                {
                    var from = DateTime.UtcNow;
                    var to   = DateTime.UtcNow.AddDays(30);

                    var pulledEvents = (await _googleCalendarService.PullEventsAsync(token.AccessToken, from, to)).ToList();

                    foreach (var evt in pulledEvents)
                    {
                        // Remove block for cancelled Google events
                        if (evt.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                        {
                            var existing = await _context.ScheduleBlocks.FirstOrDefaultAsync(
                                b => b.StaffId == token.StaffId &&
                                     b.Title == evt.Title &&
                                     b.StartDate == evt.StartTime.Date, cancellationToken);
                            if (existing != null)
                                _context.ScheduleBlocks.Remove(existing);
                            continue;
                        }

                        // Skip if an identical block already exists for this staff + time slot
                        var alreadyExists = await _context.ScheduleBlocks.AnyAsync(
                            b => b.StaffId   == token.StaffId &&
                                 b.StartDate  == evt.StartTime.Date &&
                                 b.StartTime  == evt.StartTime.TimeOfDay, cancellationToken);

                        if (!alreadyExists)
                        {
                            _context.ScheduleBlocks.Add(new ScheduleBlock
                            {
                                Id        = Guid.NewGuid(),
                                TenantId  = token.TenantId,
                                StaffId   = token.StaffId,
                                Type      = "external",
                                Title     = evt.Title,
                                StartDate = evt.StartTime.Date,
                                EndDate   = evt.EndTime.Date,
                                AllDay    = evt.StartTime.TimeOfDay == TimeSpan.Zero && evt.EndTime.TimeOfDay == TimeSpan.Zero,
                                StartTime = evt.StartTime.TimeOfDay,
                                EndTime   = evt.EndTime.TimeOfDay,
                                Status    = "approved"
                            });
                        }
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Synced {Count} external events from Google for Staff {StaffId}", pulledEvents.Count, token.StaffId);
                }

                token.LastSyncAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync calendar for Staff {StaffId}", token.StaffId);
                // Optionally disable token if revoked (401 invalid_grant)
                if (ex.Message.Contains("invalid_grant"))
                {
                    token.IsActive = false;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Google Calendar Sync job finished.");
    }
}
