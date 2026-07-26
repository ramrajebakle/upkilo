using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Day 47: Real-time waitlist auto-fill — called immediately when a booking slot is freed
/// (cancellation or rescheduling). Notifies the top-priority waitlisted client via SMS + email.
/// </summary>
public class WaitlistAutoFillService
{
    private readonly AppDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly ILogger<WaitlistAutoFillService> _logger;

    public WaitlistAutoFillService(
        AppDbContext context,
        ISmsService smsService,
        IEmailService emailService,
        ILogger<WaitlistAutoFillService> logger)
    {
        _context = context;
        _smsService = smsService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Called when a booking slot is freed. Finds the top-priority waitlisted client that matches
    /// the slot and notifies them via SMS + email so they can book before the slot is taken.
    /// </summary>
    public async Task NotifyNextOnWaitlistAsync(Guid tenantId, Guid serviceId, Guid? staffId, DateTime slotStart)
    {
        var nextEntry = await _context.WaitlistEntries
            .Include(w => w.Client)
            .Include(w => w.Service)
            .Where(w => w.TenantId == tenantId &&
                        w.ServiceId == serviceId &&
                        (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Pending) &&
                        (w.PreferredDate == DateTime.MinValue || w.PreferredDate.Date == slotStart.Date))
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAt)
            .FirstOrDefaultAsync();

        if (nextEntry == null)
        {
            _logger.LogDebug("[WaitlistAutoFill] No matching waitlist entry for service {ServiceId} on {Date}", serviceId, slotStart.Date);
            return;
        }

        // Atomically claim the entry — guards against double-notify when concurrent cancellations
        // free slots at the same time and both calls read the same top-priority entry.
        var claimed = await _context.WaitlistEntries
            .Where(w => w.Id == nextEntry.Id &&
                        (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Pending))
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.Status, WaitlistStatus.Notified)
                .SetProperty(w => w.UpdatedAt, DateTime.UtcNow));

        if (claimed == 0)
        {
            _logger.LogDebug("[WaitlistAutoFill] Entry {EntryId} already claimed by a concurrent call — skipping", nextEntry.Id);
            return;
        }

        var tenant = await _context.Tenants.FindAsync(tenantId);
        var clientName = nextEntry.Client?.FirstName ?? nextEntry.FirstName;
        var serviceName = nextEntry.Service?.Name ?? "your service";
        var slotDisplay = slotStart.ToString("dddd, MMMM d 'at' h:mm tt");
        var bookingUrl = $"https://app.upkilo.com/book/{tenant?.Slug}?service={serviceId}";

        // Send SMS if the client has a phone and SMS consent
        var phone = nextEntry.Client?.Phone ?? nextEntry.Phone;
        if (!string.IsNullOrEmpty(phone) && (nextEntry.Client?.SmsConsent ?? false))
        {
            var sms = $"Hi {clientName}! A {serviceName} slot just opened: {slotDisplay}. Book now: {bookingUrl}";
            if (sms.Length > 160) sms = sms[..157] + "...";

            try
            {
                await _smsService.SendSmsAsync(tenantId, phone, sms);
                _logger.LogInformation("[WaitlistAutoFill] SMS sent to {Phone} for slot {Slot}", phone, slotDisplay);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WaitlistAutoFill] SMS failed for {Phone}", phone);
            }
        }

        // Always send email if available
        var email = nextEntry.Client?.Email ?? nextEntry.Email;
        if (!string.IsNullOrEmpty(email))
        {
            try
            {
                await _emailService.SendEmailAsync(
                    email,
                    $"A slot just opened for {serviceName}!",
                    $"<h2>Hi {clientName}!</h2>" +
                    $"<p>A slot for <strong>{serviceName}</strong> just opened up: <strong>{slotDisplay}</strong>.</p>" +
                    $"<p>Book now before it's gone — these slots fill up fast!</p>" +
                    $"<p><a href='{bookingUrl}' style='background:#4f46e5;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:bold;'>Book Now →</a></p>");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WaitlistAutoFill] Email failed for {Email}", email);
            }
        }

        _logger.LogInformation("[WaitlistAutoFill] Notified entry {EntryId} ({Name}) for {Service} on {Date}",
            nextEntry.Id, clientName, serviceName, slotDisplay);
    }
}
