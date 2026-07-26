using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class TourController : ControllerBase
{
    private readonly ITourService _tourService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TourController> _logger;

    public TourController(
        ITourService tourService,
        ITenantProvider tenantProvider,
        ILogger<TourController> logger)
    {
        _tourService = tourService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetUserId() => _tenantProvider.GetUserId()
        ?? throw new UnauthorizedAccessException("User context not available");

    /// <summary>
    /// Get progress for a specific tour
    /// </summary>
    [HttpGet("{tourKey}")]
    public async Task<IActionResult> GetProgress(string tourKey)
    {
        var progress = await _tourService.GetProgressAsync(GetUserId(), tourKey);
        return Ok(progress);
    }

    /// <summary>
    /// Update tour progress
    /// </summary>
    [HttpPost("{tourKey}/step")]
    public async Task<IActionResult> UpdateProgress(string tourKey, [FromBody] TourProgressRequest request)
    {
        await _tourService.UpdateProgressAsync(GetUserId(), tourKey, request.Step, request.IsCompleted);
        return Ok(new { message = "Progress updated" });
    }

    /// <summary>
    /// Reset a tour
    /// </summary>
    [HttpPost("{tourKey}/reset")]
    public async Task<IActionResult> ResetTour(string tourKey)
    {
        await _tourService.ResetTourAsync(GetUserId(), tourKey);
        return Ok(new { message = "Tour reset" });
    }

    /// <summary>
    /// Get all tours progress for user
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllProgress()
    {
        var result = await _tourService.GetAllToursProgressAsync(GetUserId());
        return Ok(result);
    }
}

public record TourProgressRequest(int Step, bool IsCompleted);

