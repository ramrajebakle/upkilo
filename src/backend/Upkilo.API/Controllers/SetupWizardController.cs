using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SetupWizardController : ControllerBase
{
    private readonly ISetupWizardService _setupService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SetupWizardController> _logger;

    public SetupWizardController(
        ISetupWizardService setupService,
        ITenantProvider tenantProvider,
        ILogger<SetupWizardController> logger)
    {
        _setupService = setupService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Get current setup progress
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var progress = await _setupService.GetProgressAsync(GetTenantId());
        return Ok(new
        {
            progress.ProfileCompleted,
            progress.ServicesCompleted,
            progress.StaffCompleted,
            progress.AvailabilityCompleted,
            progress.IntegrationsCompleted,
            progress.CompletionPercentage,
            AllCompleted = progress.CompletionPercentage == 100
        });
    }

    /// <summary>
    /// Mark a setup step as complete
    /// </summary>
    [HttpPost("step/{stepName}")]
    public async Task<IActionResult> CompleteStep(string stepName)
    {
        var progress = await _setupService.CompleteStepAsync(GetTenantId(), stepName);
        return Ok(new
        {
            success = true,
            step = stepName,
            progress.CompletionPercentage
        });
    }

    /// <summary>
    /// Reset setup progress (for testing)
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        var progress = await _setupService.ResetProgressAsync(GetTenantId());
        return Ok(new { success = true, progress.CompletionPercentage });
    }
}

