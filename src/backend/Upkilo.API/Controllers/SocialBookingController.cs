using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.API.Controllers;

/// <summary>
/// Day 36-37: Social booking channels — Instagram bio link page + WhatsApp booking integration.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/social-booking")]
public class SocialBookingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public SocialBookingController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// GET /api/v1/social-booking/bio-link/{slug} — Public Instagram bio link page data.
    /// Returns tenant info + bookable services for a lightweight landing page at upkilo.com/book/{slug}.
    /// [AllowAnonymous] — no auth needed; this is a public booking page.
    /// </summary>
    [HttpGet("bio-link/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBioLinkPage(string slug)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Services)
            .FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted);

        if (tenant == null)
            return NotFound(ApiResponse.Fail("Business not found"));

        var activeServices = tenant.Services
            .Where(s => s.IsActive && !s.IsDeleted)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
            })
            .ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            tenantId = tenant.Id,
            slug = tenant.Slug,
            businessName = tenant.Name,
            tagline = tenant.Tagline,
            city = tenant.City,
            country = tenant.Country,
            averageRating = tenant.AverageRating,
            reviewCount = tenant.ReviewCount,
            bookingUrl = $"https://app.upkilo.com/book/{slug}",
            whatsAppNumber = tenant.Settings.TryGetValue("whatsapp_number", out var wa) ? wa?.ToString() : null,
            instagramHandle = tenant.Settings.TryGetValue("instagram_handle", out var ig) ? ig?.ToString() : null,
            services = activeServices
        }));
    }

    /// <summary>
    /// GET /api/v1/social-booking/whatsapp-link — Generate a WhatsApp deep link for booking inquiries.
    /// Returns wa.me link pre-filled with a booking message template.
    /// </summary>
    [HttpGet("whatsapp-link")]
    [Authorize]
    public async Task<IActionResult> GetWhatsAppBookingLink()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        if (!tenant.Settings.TryGetValue("whatsapp_number", out var waObj) || string.IsNullOrEmpty(waObj?.ToString()))
            return BadRequest(ApiResponse.Fail("WhatsApp number not configured. Add 'whatsapp_number' in business settings."));

        var waNumber = waObj.ToString()!.Replace("+", "").Replace(" ", "").Replace("-", "");
        var bookingUrl = $"https://app.upkilo.com/book/{tenant.Slug}";
        var message = Uri.EscapeDataString($"Hi {tenant.Name}! I'd like to book an appointment. Here's my booking link: {bookingUrl}");
        var deepLink = $"https://wa.me/{waNumber}?text={message}";

        return Ok(ApiResponse<object>.Ok(new
        {
            deepLink,
            waNumber,
            bookingUrl,
            qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(deepLink)}"
        }));
    }

    /// <summary>
    /// POST /api/v1/social-booking/settings — Save Instagram handle + WhatsApp number for the tenant.
    /// </summary>
    [HttpPost("settings")]
    [Authorize]
    public async Task<IActionResult> SaveSocialSettings([FromBody] SocialSettingsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        if (request.InstagramHandle != null)
            tenant.Settings["instagram_handle"] = request.InstagramHandle.TrimStart('@');

        if (request.WhatsAppNumber != null)
            tenant.Settings["whatsapp_number"] = request.WhatsAppNumber;

        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            instagramHandle = tenant.Settings.TryGetValue("instagram_handle", out var ig) ? ig?.ToString() : null,
            whatsAppNumber = tenant.Settings.TryGetValue("whatsapp_number", out var wa) ? wa?.ToString() : null,
            bioLinkUrl = $"https://upkilo.com/book/{tenant.Slug}"
        }));
    }

    /// <summary>
    /// GET /api/v1/social-booking/stats — Bio link click stats (placeholder for analytics).
    /// </summary>
    [HttpGet("stats")]
    [Authorize]
    public IActionResult GetStats()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            message = "Analytics coming soon. Link clicks will appear here.",
            bioLinkClicks = 0,
            whatsAppClicks = 0
        }));
    }
}

public class SocialSettingsRequest
{
    public string? InstagramHandle { get; set; }
    public string? WhatsAppNumber { get; set; }
}
