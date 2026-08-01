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
public class AdvancedFeaturesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public AdvancedFeaturesController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetFeatures()
    {
        var tenantId = _tenantProvider.GetTenantId();

        var features = await _context.AdvancedFeatures
            .FirstOrDefaultAsync(f => f.TenantId == tenantId);

        if (features == null)
        {
            // Default initialization if accessed for the first time
            features = new AdvancedFeatures { TenantId = tenantId.Value };
            _context.AdvancedFeatures.Add(features);
            await _context.SaveChangesAsync();
        }

        return Ok(features);
    }

    [HttpPut]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> UpdateFeatures([FromBody] UpdateFeaturesRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var features = await _context.AdvancedFeatures
            .FirstOrDefaultAsync(f => f.TenantId == tenantId);

        if (features == null)
        {
            features = new AdvancedFeatures { TenantId = tenantId.Value };
            _context.AdvancedFeatures.Add(features);
        }

        features.EnableApiAccess = request.EnableApiAccess;
        features.EnableCustomWebhooks = request.EnableCustomWebhooks;
        features.EnableWhiteLabel = request.EnableWhiteLabel;
        features.EnablePrioritySupport = request.EnablePrioritySupport;
        features.EnableCustomSmsSenderId = request.EnableCustomSmsSenderId;
        features.EnableAdvancedReporting = request.EnableAdvancedReporting;
        features.EnableIpAllowlisting = request.EnableIpAllowlisting;

        await _context.SaveChangesAsync();
        return Ok(features);
    }
}

public class UpdateFeaturesRequest
{
    public bool EnableApiAccess { get; set; }
    public bool EnableCustomWebhooks { get; set; }
    public bool EnableWhiteLabel { get; set; }
    public bool EnablePrioritySupport { get; set; }
    public bool EnableCustomSmsSenderId { get; set; }
    public bool EnableAdvancedReporting { get; set; }
    public bool EnableIpAllowlisting { get; set; }
}
