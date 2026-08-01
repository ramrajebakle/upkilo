using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Jobs;

public class WaitlistNotifierJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<WaitlistNotifierJob> _logger;

    public WaitlistNotifierJob(AppDbContext context, IEmailService emailService, ILogger<WaitlistNotifierJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Running Waitlist Notifier Job...");

        var now = DateTime.UtcNow;
        var windowEnd = now.AddDays(7);

        // Limit batch size to prevent unbounded memory growth on large deployments.
        var pendingEntries = await _context.WaitlistEntries
            .Where(w => w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Pending)
            .Include(w => w.Client)
            .Include(w => w.Service)
            .OrderBy(w => w.Priority)
            .ThenBy(w => w.CreatedAt)
            .Take(500)
            .ToListAsync();

        if (!pendingEntries.Any())
        {
            _logger.LogInformation("No pending waitlist entries found.");
            return;
        }

        // Find bookings cancelled in the last 24h whose slots fall within the next 7 days
        var freedSlots = await _context.Set<Booking>()
            .Where(b =>
                b.Status == BookingStatus.Cancelled &&
                b.CancelledAt >= now.AddHours(-24) &&
                b.StartTime >= now &&
                b.StartTime <= windowEnd)
            .Select(b => new
            {
                b.TenantId,
                b.ServiceId,
                b.StaffId,
                b.StartTime
            })
            .ToListAsync();

        if (!freedSlots.Any())
        {
            _logger.LogInformation("No freed slots in the next 7 days.");
            return;
        }

        int notified = 0;

        foreach (var entry in pendingEntries)
        {
            var match = freedSlots.FirstOrDefault(s =>
                s.TenantId == entry.TenantId &&
                s.ServiceId == entry.ServiceId &&
                (entry.StaffId == null || s.StaffId == entry.StaffId) &&
                s.StartTime.Date == entry.PreferredDate.Date &&
                MatchesTimePreference(s.StartTime, entry.PreferredTimeRange));

            if (match == null) continue;

            var email = entry.Client?.Email ?? entry.Email;
            var firstName = entry.Client?.FirstName ?? entry.FirstName;
            var service = entry.Service?.Name ?? "your requested service";

            if (string.IsNullOrWhiteSpace(email)) continue;

            try
            {
                await _emailService.SendEmailAsync(
                    email,
                    "A spot just opened up!",
                    $"Hi {firstName}, an opening for <strong>{service}</strong> on " +
                    $"<strong>{match.StartTime:dddd, MMMM d 'at' h:mm tt}</strong> is now available. " +
                    $"Book now before it's taken!");

                // Only mark Notified after confirmed send — prevents silent re-send suppression.
                entry.Status = WaitlistStatus.Notified;
                entry.UpdatedAt = DateTime.UtcNow;
                notified++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send waitlist notification email to {Email} for entry {EntryId}", email, entry.Id);
            }

            _logger.LogInformation(
                "Notified waitlist entry {EntryId} for service {ServiceId} on {Date}",
                entry.Id, entry.ServiceId, match.StartTime);
        }

        if (notified > 0)
            await _context.SaveChangesAsync();

        _logger.LogInformation("Waitlist Notifier Job completed — {Count} entries notified.", notified);
    }

    private static bool MatchesTimePreference(DateTime slotTime, string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference) || preference.Equals("Anytime", StringComparison.OrdinalIgnoreCase))
            return true;

        return preference.ToLowerInvariant() switch
        {
            "morning" => slotTime.Hour >= 6 && slotTime.Hour < 12,
            "afternoon" => slotTime.Hour >= 12 && slotTime.Hour < 17,
            "evening" => slotTime.Hour >= 17 && slotTime.Hour < 21,
            _ => true
        };
    }
}
