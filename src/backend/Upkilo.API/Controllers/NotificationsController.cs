using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Notifications controller — in-app notification management.
/// Uses the Notification entity for CRUD + read/unread tracking.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    private readonly IPushNotificationService _pushService;

    public NotificationsController(
        ILogger<NotificationsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IPushNotificationService pushService)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _pushService = pushService;
    }

    private Guid? GetUserId()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdStr, out var uid) ? uid : null;
    }

    /// <summary>
    /// Get all notifications for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Notifications
            .Where(n => n.UserId == userId.Value && n.TenantId == tenantId.Value && !n.IsDeleted);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        // Single GROUP BY pass replaces 2 separate COUNT queries (saves 1 DB round-trip).
        var counts = await _context.Notifications
            .Where(n => n.UserId == userId.Value && n.TenantId == tenantId.Value && !n.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Unread = g.Count(n => !n.IsRead) })
            .FirstOrDefaultAsync();
        var total = counts?.Total ?? 0;
        var unreadCount = counts?.Unread ?? 0;

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.ActionUrl,
                n.IsRead,
                n.CreatedAt,
                n.Priority,
                n.EntityType,
                n.EntityId
            })
            .ToListAsync();

        return Ok(new { data = notifications, unreadCount, total, page, pageSize });
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId.Value && n.TenantId == tenantId.Value && !n.IsDeleted && !n.IsRead);

        return Ok(new { count });
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value && !n.IsDeleted);

        if (notification == null) return NotFound();

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification marked as read: {NotificationId}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var unread = await _context.Notifications
            .Where(n => n.UserId == userId.Value && n.TenantId == tenantId.Value && !n.IsDeleted && !n.IsRead)
            .ToListAsync();

        var count = unread.Count;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("All notifications marked as read ({Count})", count);
        return Ok(new { success = true, count });
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value && !n.IsDeleted);

        if (notification == null) return NotFound();

        notification.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification deleted: {NotificationId}", id);
        return NoContent();
    }

    // ── Push notification device registration (merged from NotificationController) ──

    /// <summary>
    /// Register a browser push subscription (Web Push API).
    /// </summary>
    [HttpPost("push/browser/register")]
    public async Task<IActionResult> RegisterBrowser([FromBody] BrowserSubscriptionRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var subscription = new WebPushSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            UserId = userId.Value,
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth,
            Tag = request.Tag,
            IsActive = true
        };

        await _pushService.RegisterBrowserSubscriptionAsync(userId.Value, subscription);
        return Ok(new { message = "Browser subscription registered." });
    }

    /// <summary>
    /// Register a mobile device token (FCM / APNs).
    /// </summary>
    [HttpPost("push/mobile/register")]
    public async Task<IActionResult> RegisterMobile([FromBody] MobileTokenRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var token = new PushNotificationToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            UserId = userId.Value,
            DeviceToken = request.DeviceToken,
            Platform = request.Platform,
            DeviceModel = request.DeviceModel,
            OsVersion = request.OsVersion,
            IsActive = true
        };

        await _pushService.RegisterMobileTokenAsync(userId.Value, token);
        return Ok(new { message = "Mobile token registered." });
    }

    /// <summary>
    /// Send a test push notification to the current user.
    /// </summary>
    [HttpPost("push/test")]
    public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await _pushService.SendPushToUserAsync(userId.Value, "Test Notification", request.Message ?? "This is a test notification from Upkilo!");
        return Ok(new { message = "Test notification sent." });
    }
}

public class BrowserSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? Tag { get; set; }
}

public class MobileTokenRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "FCM";
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
}

public class TestNotificationRequest
{
    public string? Message { get; set; }
}
