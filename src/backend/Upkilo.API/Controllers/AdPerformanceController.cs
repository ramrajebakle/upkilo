using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class AdPerformanceController : ControllerBase
{
    private readonly IAdCampaignService _campaignService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AdPerformanceController> _logger;

    public AdPerformanceController(
        IAdCampaignService campaignService,
        ITenantProvider tenantProvider,
        ILogger<AdPerformanceController> logger)
    {
        _campaignService = campaignService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Sync campaigns from a specific platform
    /// </summary>
    [HttpPost("sync/{platform}")]
    public async Task<IActionResult> SyncCampaigns(string platform)
    {
        var success = await _campaignService.SyncPlatformCampaignsAsync(GetTenantId(), platform);
        if (!success) return BadRequest(new { message = "Failed to sync campaigns or account not connected." });
        return Ok(new { success = true });
    }

    /// <summary>
    /// Get recent performance metrics for a specific campaign
    /// </summary>
    [HttpGet("campaign/{id}/performance")]
    public async Task<IActionResult> GetCampaignPerformance(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;
        var metrics = await _campaignService.GetCampaignPerformanceAsync(GetTenantId(), id, startDate, endDate);
        return Ok(metrics);
    }

    /// <summary>
    /// Get total ad spend for the tenant across all platforms
    /// </summary>
    [HttpGet("total-spend")]
    public async Task<IActionResult> GetTotalSpend([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;
        var spend = await _campaignService.GetTotalAdSpendAsync(GetTenantId(), startDate, endDate);
        return Ok(new { totalSpend = spend, currency = "USD" });
    }

    /// <summary>
    /// Update campaign status
    /// </summary>
    [HttpPut("campaign/{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
    {
        var success = await _campaignService.UpdateCampaignStatusAsync(GetTenantId(), id, status);
        if (!success) return NotFound();
        return Ok(new { success = true });
    }
}

