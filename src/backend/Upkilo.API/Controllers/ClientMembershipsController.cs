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
public class ClientMembershipsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ClientMembershipsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetClientMemberships([FromQuery] Guid? clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.ClientMemberships
            .Include(m => m.Plan)
            .Include(m => m.Client)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(m => m.ClientId == clientId.Value);

        var memberships = await query.ToListAsync();
        return Ok(memberships);
    }

    [HttpPost]
    public async Task<IActionResult> AssignMembership([FromBody] AssignMembershipRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var plan = await _context.MembershipPlans.FindAsync(request.PlanId);
        if (plan == null) return NotFound("Membership plan not found.");

        var cm = new ClientMembership
        {
            TenantId = tenantId.Value,
            ClientId = request.ClientId,
            PlanId = request.PlanId,
            Status = MembershipStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = plan.BillingInterval == "Monthly" ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1),
            StripeSubscriptionId = request.StripeSubscriptionId // Normally populated via Webhook or initial checkout flow
        };

        _context.ClientMemberships.Add(cm);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetClientMemberships), new { clientId = cm.ClientId }, cm);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelMembership(Guid id)
    {
        var cm = await _context.ClientMemberships.FindAsync(id);
        if (cm == null) return NotFound();

        cm.Status = MembershipStatus.Cancelled;
        cm.EndDate = DateTime.UtcNow; // Or NextBillingDate depending on proration preferences

        await _context.SaveChangesAsync();
        // Fire DomainEvent: MembershipCancelled (triggers workflows, Stripe cancellation)
        return Ok(cm);
    }
}

public class AssignMembershipRequest
{
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public string? StripeSubscriptionId { get; set; }
}
