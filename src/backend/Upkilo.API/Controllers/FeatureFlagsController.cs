using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers;

/// <summary>
/// Per-tenant feature flag management — gradual rollouts, kill switches, A/B
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class FeatureFlagsController : ControllerBase
{
    private readonly ILogger<FeatureFlagsController> _logger;
    private readonly FeatureFlagService _featureFlagService;
    private readonly ITenantProvider _tenantProvider;

    public FeatureFlagsController(ILogger<FeatureFlagsController> logger, FeatureFlagService featureFlagService, ITenantProvider tenantProvider)
    {
        _logger = logger;
        _featureFlagService = featureFlagService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>GET /api/v1/featureflags — list all flags with tenant context</summary>
    [HttpGet]
    public IActionResult GetFlags()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var flags = _featureFlagService.GetAllFlags();
        var flagsWithContext = flags.Select(f =>
        {
            var flagName = ((dynamic)f).Name as string ?? "";
            return new
            {
                name = flagName,
                description = ((dynamic)f).Description,
                isEnabled = ((dynamic)f).IsEnabled,
                rolloutPercentage = ((dynamic)f).RolloutPercentage,
                enabledForTenant = _featureFlagService.IsEnabled(flagName, tenantId),
            };
        }).ToList();

        return Ok(ApiResponse<object>.Ok(new { flags = flagsWithContext, total = flagsWithContext.Count }));
    }

    /// <summary>GET /api/v1/featureflags/{name}/check — check if flag enabled for current tenant</summary>
    [HttpGet("{name}/check")]
    public IActionResult CheckFlag(string name)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var enabled = _featureFlagService.IsEnabled(name, tenantId);
        return Ok(ApiResponse<object>.Ok(new { name, enabled }));
    }

    /// <summary>POST /api/v1/featureflags/{name}/override — set tenant-level override</summary>
    [HttpPost("{name}/override")]
    public IActionResult SetTenantOverride(string name, [FromBody] FeatureFlagOverrideRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        _featureFlagService.SetTenantOverride(name, tenantId.Value, request.Enabled);
        _logger.LogInformation("Tenant {TenantId} set override for flag {Flag}: {Enabled}", tenantId, name, request.Enabled);

        return Ok(ApiResponse<object>.Ok(new { name, enabled = request.Enabled, tenantId }));
    }

    /// <summary>POST /api/v1/featureflags — register a new flag (admin)</summary>
    [HttpPost]
    public IActionResult CreateFlag([FromBody] CreateFeatureFlagRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse.Fail("Flag name is required"));

        // Normalize flag name: lowercase, underscores
        var flagName = request.Name.ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
        _featureFlagService.RegisterFlag(flagName, request.DefaultEnabled, request.Description);

        if (request.RolloutPercentage.HasValue)
            _featureFlagService.SetRolloutPercentage(flagName, request.RolloutPercentage.Value);

        return Ok(ApiResponse<object>.Ok(new { name = flagName, created = true }));
    }

    /// <summary>PUT /api/v1/featureflags/{name}/rollout — update rollout percentage</summary>
    [HttpPut("{name}/rollout")]
    public IActionResult SetRollout(string name, [FromBody] SetRolloutRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var percentage = Math.Clamp(request.Percentage, 0, 100);
        _featureFlagService.SetRolloutPercentage(name, percentage);

        return Ok(ApiResponse<object>.Ok(new { name, rolloutPercentage = percentage }));
    }
}

public class FeatureFlagOverrideRequest
{
    public bool Enabled { get; set; }
}

public class CreateFeatureFlagRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool DefaultEnabled { get; set; } = false;
    public int? RolloutPercentage { get; set; }
}

public class SetRolloutRequest
{
    public int Percentage { get; set; }
}
