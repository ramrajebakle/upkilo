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
public class MembershipPlansController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public MembershipPlansController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlans()
    {
        var plansList = await _context.MembershipPlans
            .Where(p => p.IsActive)
            .ToListAsync();
        var plans = plansList.OrderBy(p => p.Price);
            
        return Ok(plans);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreateMembershipPlanRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var plan = new MembershipPlan
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            BillingInterval = request.BillingInterval, // e.g., "Monthly", "Yearly"
            IsActive = true,
            StripePriceId = request.StripePriceId // Assuming product created in Stripe via webhook or UI flow
        };

        _context.MembershipPlans.Add(plan);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPlans), new { id = plan.Id }, plan);
    }
}
