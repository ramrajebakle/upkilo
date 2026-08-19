using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers;

/// <summary>
/// The tenant's view of automated rebooking — who is due, who will actually be contacted, and the
/// switch to stop it.
///
/// These messages go out under the tenant's name to the tenant's customers, so the tenant needs to
/// be able to see the audience rather than take it on trust. The audience shown here is produced
/// by the same RebookAudienceService the sending job uses, so what is previewed is what is sent.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/marketing/rebook")]
[Authorize(Roles = "Owner,Admin")]
public class RebookMarketingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<RebookMarketingController> _logger;

    public RebookMarketingController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<RebookMarketingController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// GET /marketing/rebook/audience — everyone currently past their rebooking interval, with the
    /// reason anyone who will not be contacted is excluded.
    ///
    /// Read-only. Viewing the audience never sends anything.
    /// </summary>
    [HttpGet("audience")]
    public async Task<IActionResult> GetAudience([FromQuery] int limit = 500, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        limit = Math.Clamp(limit, 1, 2000);

        var enabled = await _context.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.RebookRemindersEnabled)
            .FirstOrDefaultAsync(ct);

        // Paused tenants are filtered out inside the service, which would make the audience look
        // empty rather than paused. Reported explicitly so the screen can say why.
        var audience = enabled
            ? await new RebookAudienceService(_context).GetDueAsync(tenantId, limit, ct)
            : new List<RebookCandidate>();

        var byService = audience
            .GroupBy(c => new { c.ServiceId, c.ServiceName })
            .Select(g => new
            {
                serviceId = g.Key.ServiceId,
                service = g.Key.ServiceName,
                due = g.Count(),
                willContact = g.Count(c => c.Eligibility == RebookEligibility.Ready),
            })
            .OrderByDescending(g => g.due);

        return Ok(new
        {
            data = new
            {
                enabled,
                // Counts first: the actionable number is willContact, and it is routinely far
                // below totalDue once consent is applied. Showing only totalDue would overstate
                // the reach of a campaign the tenant is deciding whether to rely on.
                totalDue = audience.Count,
                willContact = audience.Count(c => c.Eligibility == RebookEligibility.Ready),
                excluded = new
                {
                    alreadyRebooked = audience.Count(c => c.Eligibility == RebookEligibility.AlreadyRebooked),
                    noConsent = audience.Count(c => c.Eligibility == RebookEligibility.NoConsent),
                    noContactDetails = audience.Count(c => c.Eligibility == RebookEligibility.NoContactDetails),
                },
                byService,
                clients = audience.Select(c => new
                {
                    c.ClientId,
                    client = c.ClientName,
                    service = c.ServiceName,
                    lastVisit = c.LastVisit,
                    c.DaysSinceVisit,
                    dueAfterDays = c.RebookAfterDays,
                    overdueByDays = c.DaysSinceVisit - c.RebookAfterDays,
                    status = c.Eligibility.ToString(),
                    channel = c.Channel,
                }),
            }
        });
    }

    /// <summary>
    /// PUT /marketing/rebook/settings — pause or resume automated rebooking reminders.
    ///
    /// Pausing does not clear any service's interval, so resuming restores the previous behaviour
    /// without the tenant having to remember what each service was set to.
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] RebookSettingsRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant == null) return NotFound();

        tenant.RebookRemindersEnabled = request.Enabled;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Rebooking reminders {State} for tenant {TenantId}",
            request.Enabled ? "enabled" : "paused", tenantId);

        return Ok(new { success = true, enabled = tenant.RebookRemindersEnabled });
    }
}

public record RebookSettingsRequest(bool Enabled);
