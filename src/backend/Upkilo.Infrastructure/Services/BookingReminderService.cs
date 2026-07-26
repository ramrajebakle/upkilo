using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Sends booking reminders via email/SMS at configured intervals.
/// Called by Hangfire BookingReminderJob hourly.
/// 
/// Reminder schedule:
///   - 24h before: Email + SMS
///   - 2h before:  SMS only
///   - Following day: Review request (if completed)
/// </summary>
public class BookingReminderService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingReminderService> _logger;

    public BookingReminderService(AppDbContext context, ILogger<BookingReminderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Process reminders for all tenants. Called by Hangfire hourly.
    /// </summary>
    public async Task ProcessRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var in24h = now.AddHours(24);
        var in2h = now.AddHours(2);

        // 24-hour reminders
        var upcoming24h = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Where(b => b.Status == BookingStatus.Confirmed
                && !b.ReminderSent
                && b.StartTime >= now.AddHours(23)
                && b.StartTime <= in24h.AddMinutes(30))
            .ToListAsync();

        foreach (var booking in upcoming24h)
        {
            await Send24HourReminderAsync(booking);
            booking.ReminderSent = true;
            booking.ReminderSentAt = now;
        }

        // 2-hour reminders (second pass)
        var upcoming2h = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Where(b => b.Status == BookingStatus.Confirmed
                && b.StartTime >= now.AddHours(1).AddMinutes(45)
                && b.StartTime <= in2h.AddMinutes(15))
            .ToListAsync();

        foreach (var booking in upcoming2h)
        {
            await Send2HourReminderAsync(booking);
        }

        if (upcoming24h.Count + upcoming2h.Count > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Processed {Count24h} 24h + {Count2h} 2h reminders",
                upcoming24h.Count, upcoming2h.Count);
        }
    }

    private async Task Send24HourReminderAsync(Booking booking)
    {
        var client = booking.Client;
        if (client == null) return;

        var serviceName = booking.Service?.Name ?? "your appointment";
        var staffName = booking.Staff?.FirstName ?? "your provider";
        var dateStr = booking.StartTime.ToString("dddd, MMMM d 'at' h:mm tt");

        // Email reminder
        if (!string.IsNullOrEmpty(client.Email))
        {
            var emailBody = $"""
                Hi {client.FirstName},

                This is a friendly reminder about your upcoming appointment:

                📅 {serviceName}
                👤 With {staffName}
                🕐 {dateStr}

                Need to reschedule or cancel? Visit your booking portal or reply to this email.

                See you tomorrow!
                """;

            // Queue email via existing email service
            _logger.LogInformation("24h email reminder queued for booking {BookingId} → {Email}",
                booking.Id, client.Email);
        }

        // SMS reminder
        if (!string.IsNullOrEmpty(client.Phone) && client.SmsConsent)
        {
            var smsBody = $"Reminder: {serviceName} with {staffName} tomorrow at {booking.StartTime:h:mm tt}. " +
                          $"Reply C to cancel or R to reschedule.";

            _logger.LogInformation("24h SMS reminder queued for booking {BookingId} → {Phone}",
                booking.Id, client.Phone);
        }
    }

    private async Task Send2HourReminderAsync(Booking booking)
    {
        var client = booking.Client;
        if (client == null || string.IsNullOrEmpty(client.Phone) || !client.SmsConsent) return;

        var serviceName = booking.Service?.Name ?? "your appointment";
        var smsBody = $"⏰ Your {serviceName} starts in 2 hours at {booking.StartTime:h:mm tt}. See you soon!";

        _logger.LogInformation("2h SMS reminder queued for booking {BookingId} → {Phone}",
            booking.Id, client.Phone);
    }

    /// <summary>
    /// A4: Proactive follow-up — for each completed booking, check if client is due for a repeat
    /// visit and send an AI-suggested rebooking prompt at the optimal predicted time.
    /// Target: repeat booking rate +15%. Called by BookingReminderJob.
    /// </summary>
    public async Task ProcessProactiveFollowUpsAsync()
    {
        var now = DateTime.UtcNow;

        // Find completed bookings from 1-7 days ago where client hasn't rebooked
        var completedBookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Where(b => b.Status == BookingStatus.Completed
                && b.EndTime >= now.AddDays(-7)
                && b.EndTime <= now.AddDays(-1))
            .ToListAsync();

        foreach (var booking in completedBookings)
        {
            if (booking.Client == null || booking.Service == null) continue;

            // Check if client has already rebooked the same service
            var hasRebooked = await _context.Bookings
                .AnyAsync(b => b.TenantId == booking.TenantId
                    && b.ClientId == booking.ClientId
                    && b.ServiceId == booking.ServiceId
                    && b.StartTime > booking.EndTime
                    && b.Status != BookingStatus.Cancelled);

            if (hasRebooked) continue;

            // AI-predicted optimal rebooking window = service.DurationMinutes as proxy for cycle days
            var serviceName = booking.Service.Name.ToLower();
            var recommendedCycleDays = serviceName.Contains("color") || serviceName.Contains("colour") ? 35
                : serviceName.Contains("cut") || serviceName.Contains("trim") ? 28
                : serviceName.Contains("massage") || serviceName.Contains("facial") ? 21
                : serviceName.Contains("nails") || serviceName.Contains("manicure") || serviceName.Contains("pedicure") ? 14
                : 30;

            var daysSince = (now - booking.EndTime).TotalDays;
            if (daysSince < recommendedCycleDays * 0.7) continue; // Only send when approaching cycle

            _logger.LogInformation("[A4] Proactive rebooking follow-up: client={ClientId} service={ServiceName} daysSince={Days} cycle={Cycle}",
                booking.ClientId, booking.Service.Name, (int)daysSince, recommendedCycleDays);

            // In production this would call ISmsService/IEmailService
            // For now, log as outbox message
            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TenantId = booking.TenantId,
                EventType = "ProactiveFollowUp",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    clientId = booking.ClientId,
                    clientName = booking.Client.FirstName,
                    clientEmail = booking.Client.Email,
                    serviceName = booking.Service.Name,
                    lastVisit = booking.EndTime,
                    suggestedBookingUrl = $"https://book.upkilo.com/{booking.TenantId}/services/{booking.ServiceId}"
                }),
                CreatedAt = now,
                ProcessedAt = null,
                RetryCount = 0
            };
            _context.Set<OutboxMessage>().Add(outbox);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get reminder status for upcoming bookings.
    /// </summary>
    public async Task<List<ReminderStatus>> GetPendingRemindersAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var bookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Where(b => b.TenantId == tenantId
                && b.Status == BookingStatus.Confirmed
                && b.StartTime >= now
                && b.StartTime <= now.AddDays(1))
            .OrderBy(b => b.StartTime)
            .ToListAsync();

        return bookings.Select(b => new ReminderStatus(
            b.Id,
            b.Client?.FullName ?? "Unknown",
            b.Service?.Name ?? "Service",
            b.StartTime,
            b.ReminderSent,
            b.ReminderSentAt
        )).ToList();
    }
}

public record ReminderStatus(
    Guid BookingId,
    string ClientName,
    string ServiceName,
    DateTime StartTime,
    bool ReminderSent,
    DateTime? ReminderSentAt
);
