using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;


namespace Upkilo.API.Controllers;

/// <summary>
/// Notification preferences: lets users control which channels
/// and notification types they receive.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/notification-preferences")]
[Authorize]
public class NotificationPreferencesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public NotificationPreferencesController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get current user's notification preferences (creates defaults if not set)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var prefs = await _context.Set<NotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        if (prefs == null)
        {
            prefs = new NotificationPreference
            {
                UserId = userId.Value,
                TenantId = _tenantProvider.GetTenantId() ?? Guid.Empty
            };
            _context.Set<NotificationPreference>().Add(prefs);
            await _context.SaveChangesAsync();
        }

        return Ok(prefs);
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var prefs = await _context.Set<NotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        if (prefs == null) return NotFound("Preferences not found. Call GET first to initialize.");

        // Update channel toggles
        if (request.EmailEnabled.HasValue) prefs.EmailEnabled = request.EmailEnabled.Value;
        if (request.SmsEnabled.HasValue) prefs.SmsEnabled = request.SmsEnabled.Value;
        if (request.PushEnabled.HasValue) prefs.PushEnabled = request.PushEnabled.Value;
        if (request.InAppEnabled.HasValue) prefs.InAppEnabled = request.InAppEnabled.Value;
        if (request.WhatsAppEnabled.HasValue) prefs.WhatsAppEnabled = request.WhatsAppEnabled.Value;

        // Update notification type toggles
        if (request.BookingConfirmations.HasValue) prefs.BookingConfirmations = request.BookingConfirmations.Value;
        if (request.BookingReminders.HasValue) prefs.BookingReminders = request.BookingReminders.Value;
        if (request.BookingCancellations.HasValue) prefs.BookingCancellations = request.BookingCancellations.Value;
        if (request.PaymentReceipts.HasValue) prefs.PaymentReceipts = request.PaymentReceipts.Value;
        if (request.MarketingEmails.HasValue) prefs.MarketingEmails = request.MarketingEmails.Value;
        if (request.PromotionalOffers.HasValue) prefs.PromotionalOffers = request.PromotionalOffers.Value;
        if (request.LoyaltyUpdates.HasValue) prefs.LoyaltyUpdates = request.LoyaltyUpdates.Value;
        if (request.ReviewRequests.HasValue) prefs.ReviewRequests = request.ReviewRequests.Value;
        if (request.SystemUpdates.HasValue) prefs.SystemUpdates = request.SystemUpdates.Value;

        // Security alerts cannot be disabled
        prefs.SecurityAlerts = true;

        // Timing
        if (request.QuietHoursStart != null) prefs.QuietHoursStart = request.QuietHoursStart;
        if (request.QuietHoursEnd != null) prefs.QuietHoursEnd = request.QuietHoursEnd;
        if (request.PreferredTimezone != null) prefs.PreferredTimezone = request.PreferredTimezone;
        if (request.ChannelPriority != null) prefs.ChannelPriority = request.ChannelPriority;

        // Sound and Badge
        if (request.PlaySound.HasValue) prefs.PlaySound = request.PlaySound.Value;
        if (request.SoundFileName != null) prefs.SoundFileName = request.SoundFileName;
        if (request.ShowBadge.HasValue) prefs.ShowBadge = request.ShowBadge.Value;

        prefs.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(prefs);
    }

    /// <summary>
    /// Reset all preferences to defaults
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetPreferences()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var prefs = await _context.Set<NotificationPreference>()
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        if (prefs == null) return NotFound();

        prefs.EmailEnabled = true;
        prefs.SmsEnabled = true;
        prefs.PushEnabled = true;
        prefs.InAppEnabled = true;
        prefs.WhatsAppEnabled = false;
        prefs.BookingConfirmations = true;
        prefs.BookingReminders = true;
        prefs.BookingCancellations = true;
        prefs.PaymentReceipts = true;
        prefs.MarketingEmails = true;
        prefs.PromotionalOffers = true;
        prefs.LoyaltyUpdates = true;
        prefs.ReviewRequests = true;
        prefs.SecurityAlerts = true;
        prefs.SystemUpdates = true;
        prefs.QuietHoursStart = null;
        prefs.QuietHoursEnd = null;
        prefs.ChannelPriority = "email,sms,push";
        prefs.PlaySound = true;
        prefs.SoundFileName = "default.mp3";
        prefs.ShowBadge = true;
        prefs.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Preferences reset to defaults", prefs });
    }

    private Guid? GetUserId()
    {
        var sub = (User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        return sub != null ? Guid.Parse(sub) : null;
    }
}

public class UpdatePreferencesRequest
{
    // Channel toggles
    public bool? EmailEnabled { get; set; }
    public bool? SmsEnabled { get; set; }
    public bool? PushEnabled { get; set; }
    public bool? InAppEnabled { get; set; }
    public bool? WhatsAppEnabled { get; set; }

    // Type toggles
    public bool? BookingConfirmations { get; set; }
    public bool? BookingReminders { get; set; }
    public bool? BookingCancellations { get; set; }
    public bool? PaymentReceipts { get; set; }
    public bool? MarketingEmails { get; set; }
    public bool? PromotionalOffers { get; set; }
    public bool? LoyaltyUpdates { get; set; }
    public bool? ReviewRequests { get; set; }
    public bool? SystemUpdates { get; set; }

    // Timing
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
    public string? PreferredTimezone { get; set; }
    public string? ChannelPriority { get; set; }

    // Sound and Badge
    public bool? PlaySound { get; set; }
    public string? SoundFileName { get; set; }
    public bool? ShowBadge { get; set; }
}
