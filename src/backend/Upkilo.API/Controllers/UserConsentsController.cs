using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UserConsentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public UserConsentsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetConsents()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var consents = await _context.UserConsents
            .Where(c => c.TenantId == tenantId && c.UserId.ToString() == userId)
            .ToListAsync();

        return Ok(consents);
    }

    [HttpPost]
    public async Task<IActionResult> RecordConsent([FromBody] RecordConsentRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var existingConsent = await _context.UserConsents
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ConsentType == request.ConsentType);

        if (existingConsent != null)
        {
            existingConsent.IsGranted = request.IsGranted;
            existingConsent.UpdatedAt = DateTime.UtcNow;
            existingConsent.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        else
        {
            var consent = new UserConsent
            {
                TenantId = tenantId.Value,
                UserId = userId,
                ConsentType = request.ConsentType, // e.g., "MarketingEmails", "DataProcessing"
                IsGranted = request.IsGranted,
                GrantedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.UserConsents.Add(consent);
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> WithdrawConsent([FromBody] WithdrawConsentRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var existingConsent = await _context.UserConsents
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ConsentType == request.ConsentType);

        if (existingConsent != null && existingConsent.IsGranted)
        {
            existingConsent.IsGranted = false;
            existingConsent.UpdatedAt = DateTime.UtcNow;
            existingConsent.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Log security event for consent withdrawal
            var securityEvent = new Upkilo.Core.Entities.SecurityEvent
            {
                TenantId = tenantId.Value,
                UserId = userId,
                EventType = Upkilo.Core.Entities.SecurityEventTypes.PrivacyConsentWithdrawn,
                IpAddress = existingConsent.IpAddress ?? string.Empty,
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                Timestamp = DateTime.UtcNow,
                Details = $"{request.ConsentType} consent withdrawn."
            };

            _context.Set<Upkilo.Core.Entities.SecurityEvent>().Add(securityEvent);
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetConsentHistory()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        // Fetch security events related to consent for this user
        var history = await _context.Set<Upkilo.Core.Entities.SecurityEvent>()
            .Where(e => e.TenantId == tenantId && e.UserId == userId &&
                       (e.EventType == SecurityEventTypes.PrivacyConsentGranted ||
                        e.EventType == SecurityEventTypes.PrivacyConsentWithdrawn))
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();

        return Ok(history);
    }
}

public class WithdrawConsentRequest
{
    public string ConsentType { get; set; } = string.Empty;
}

public class RecordConsentRequest
{
    public string ConsentType { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
