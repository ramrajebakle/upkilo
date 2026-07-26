using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Consumer (marketplace end-user) endpoints. A consumer may hold bookings across many
/// tenants; those bookings are linked through the per-tenant Client record that shares the
/// consumer's email. Every query here is scoped strictly to the authenticated user's own
/// email, so a caller can only ever see their own data.
///
/// This controller backs the mobile consumer app's `GET /api/v1/consumer/bookings` call,
/// which previously had no server-side route (404).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/consumer")]
[Authorize]
public class ConsumerController : ControllerBase
{
    private readonly AppDbContext _context;

    public ConsumerController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the authenticated consumer's bookings across all businesses, newest first.
    /// </summary>
    [HttpGet("bookings")]
    public async Task<IActionResult> GetMyBookings([FromQuery] int limit = 50)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return Ok(Array.Empty<object>());

        limit = Math.Clamp(limit, 1, 100);

        // Cross-tenant BY DESIGN: a consumer's bookings live under multiple tenants, joined only by
        // the email on the per-tenant Client record. IgnoreQueryFilters bypasses tenant scoping;
        // the email predicate guarantees a caller only ever sees their OWN bookings (no leak).
        var bookings = await _context.Bookings
            .IgnoreQueryFilters()
            .Where(b => !b.IsDeleted && b.ClientId != null &&
                        _context.Clients.IgnoreQueryFilters()
                            .Any(c => c.Id == b.ClientId && c.Email == email))
            .OrderByDescending(b => b.StartTime)
            .Take(limit)
            .Select(b => new
            {
                id = b.Id,
                startTime = b.StartTime,
                endTime = b.EndTime,
                status = b.Status,
                serviceName = _context.Services.IgnoreQueryFilters()
                    .Where(s => s.Id == b.ServiceId).Select(s => s.Name).FirstOrDefault(),
                businessName = _context.Tenants.IgnoreQueryFilters()
                    .Where(t => t.Id == b.TenantId).Select(t => t.Name).FirstOrDefault(),
                businessSlug = _context.Tenants.IgnoreQueryFilters()
                    .Where(t => t.Id == b.TenantId).Select(t => t.Slug).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(bookings);
    }
}
