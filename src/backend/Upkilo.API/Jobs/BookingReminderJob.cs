using Hangfire;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

public class BookingReminderJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ITimezoneService _timezoneService;
    private readonly ILogger<BookingReminderJob> _logger;
    private readonly IConnectionMultiplexer _redis;

    public BookingReminderJob(
        AppDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        IWhatsAppService whatsAppService,
        ITimezoneService timezoneService,
        ILogger<BookingReminderJob> logger,
        IConnectionMultiplexer redis)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _whatsAppService = whatsAppService;
        _timezoneService = timezoneService;
        _logger = logger;
        _redis = redis;
    }

    private const string LockKey = "jobs:booking_reminder:lock";
    private const string LockValue = "1";

    public async Task ExecuteAsync()
    {
        // The Redis lock coordinates replicas so a booking cannot be reminded twice.
        // Redis being unreachable is therefore a reason to SKIP this run - never to run
        // unlocked, which would send duplicate reminders to real customers, and never to
        // throw. An unhandled RedisConnectionException marks the Hangfire job Failed, and
        // at 96 runs a day a brief Redis blip produces dozens of failed jobs. Hangfire
        // keeps failed jobs forever, so that burst pins the "hangfire" health check at
        // Degraded permanently, long after Redis has recovered. The reminder window is
        // 23-25 hours wide, so every booking gets roughly eight further chances and a
        // skipped run costs nothing.
        IDatabase db;
        bool acquired;
        try
        {
            db = _redis.GetDatabase();
            acquired = await db.LockTakeAsync(LockKey, LockValue, TimeSpan.FromMinutes(10));
        }
        // RedisTimeoutException derives from TimeoutException, not RedisException, so
        // both branches are needed to cover "Redis is not answering right now".
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            _logger.LogWarning(ex, "BookingReminderJob: Redis unavailable, skipping this run");
            return;
        }

        if (!acquired)
        {
            _logger.LogInformation("BookingReminderJob: lock held by another worker, skipping");
            return;
        }

        try
        {
            await RunAsync();
        }
        finally
        {
            // Releasing must not mask an exception from RunAsync, and must not fail the
            // job on its own - the lock carries a 10-minute TTL and expires regardless.
            try
            {
                await db.LockReleaseAsync(LockKey, LockValue);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException)
            {
                _logger.LogWarning(ex, "BookingReminderJob: could not release lock; it expires on its own");
            }
        }
    }

    private async Task RunAsync()
    {
        _logger.LogInformation("Starting booking reminder job");

        // Find confirmed bookings starting between 23 and 25 hours from now that haven't had a reminder sent
        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(23);
        var windowEnd = now.AddHours(25);

        var bookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Include(b => b.Tenant)
            .Where(b => b.Status == BookingStatus.Confirmed &&
                        b.StartTime >= windowStart &&
                        b.StartTime <= windowEnd &&
                        !b.ReminderSent &&
                        b.Client != null &&
                        (!string.IsNullOrEmpty(b.Client.Email) || !string.IsNullOrEmpty(b.Client.Phone)))
            .ToListAsync();

        _logger.LogInformation("Found {Count} bookings to remind", bookings.Count);

        foreach (var booking in bookings)
        {
            try
            {
                // 1. Determine Timezone and Local Time
                var timezoneId = _timezoneService.GetBookingTimezone(booking);
                var localTime = _timezoneService.ConvertToUserTimezone(booking.StartTime, timezoneId);

                var emailData = new BookingEmailData(
                    booking.Client!.Email!,
                    booking.ClientId ?? Guid.Empty,
                    booking.TenantId,
                    booking.Client.FirstName,
                    booking.Service?.Name ?? "Service",
                    booking.Staff?.FirstName ?? "Staff",
                    localTime, // Local Date
                    localTime.TimeOfDay, // Local Time
                    booking.Service?.DurationMinutes ?? 30,
                    booking.Price ?? 0,
                    booking.Id.ToString().Substring(0, 8).ToUpper(),
                    booking.Tenant!.Name,
                    "See website",
                    "",
                    null,
                    null
                );

                // 1. Email Reminder
                if (!string.IsNullOrEmpty(booking.Client!.Email))
                {
                    await _emailService.SendBookingReminderAsync(emailData);
                }

                // 2. SMS/WhatsApp (check preferences)
                if (!string.IsNullOrEmpty(booking.Client.Phone))
                {
                    // Fetch or assume default preferences
                    var prefs = await _context.Set<NotificationPreference>()
                        .FirstOrDefaultAsync(p => p.UserId == (booking.ClientId ?? Guid.Empty));

                    if (prefs == null || prefs.SmsEnabled)
                    {
                        await _smsService.SendBookingReminderAsync(booking);
                    }

                    if (prefs != null && prefs.WhatsAppEnabled)
                    {
                        var waData = new WhatsAppBookingData(
                            booking.Client.Phone,
                            booking.ClientId ?? Guid.Empty,
                            booking.TenantId,
                            booking.Client.FirstName,
                            booking.Service?.Name ?? "Service",
                            booking.Staff?.FirstName ?? "Staff",
                            localTime,
                            localTime.TimeOfDay,
                            booking.Tenant.Name,
                            booking.Id.ToString().Substring(0, 8).ToUpper()
                        );
                        await _whatsAppService.SendBookingReminderAsync(waData);
                    }
                }

                booking.ReminderSent = true;
                booking.ReminderSentAt = DateTime.UtcNow;

                // Save incrementally or in batch? Batch is better but individual failure handling is needed.
                // For simplicity saving in loop but efficiently tracked by context.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder for booking {BookingId}", booking.Id);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Booking reminder job completed");
    }
}
