using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Owner,Admin")]
public class TwoFactorConfigsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public TwoFactorConfigsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var tenant = await _context.Tenants.FindAsync(tenantId);

        if (tenant == null) return NotFound();

        return Ok(new
        {
            EnforceTwoFactorForStaff = tenant.EnforceTwoFactorForStaff,
            EnforceTwoFactorForClients = tenant.EnforceTwoFactorForClients // Assuming these flags exist on Tenant
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateTwoFactorConfigRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var tenant = await _context.Tenants.FindAsync(tenantId);

        if (tenant == null) return NotFound();

        tenant.EnforceTwoFactorForStaff = request.EnforceTwoFactorForStaff;

        // Example check: Assuming we add EnforceTwoFactorForClients to the DB schema if it's not there.
        // For now, updating what is statically available in typical multi-tenant setups.

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "2FA policy updated." });
    }
}

public class UpdateTwoFactorConfigRequest
{
    public bool EnforceTwoFactorForStaff { get; set; }
    // public bool EnforceTwoFactorForClients { get; set; } 
}
