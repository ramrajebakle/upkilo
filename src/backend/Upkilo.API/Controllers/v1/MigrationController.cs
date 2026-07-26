using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;


namespace Upkilo.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ApiVersion("1.0")]
public class MigrationController : ControllerBase
{
    private readonly IMigrationService _migrationService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(
        IMigrationService migrationService,
        ITenantProvider tenantProvider,
        ILogger<MigrationController> logger)
    {
        _migrationService = migrationService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Validates credentials and returns an overview of available data to migrate
    /// </summary>
    [HttpPost("overview")]
    public async Task<ActionResult<MigrationOverview>> GetOverview([FromBody] MigrationOverviewRequest request)
    {
        try
        {
            var overview = await _migrationService.GetMigrationOverviewAsync(
                request.Provider, 
                request.ApiKey, 
                request.ExtraCredentials);
            
            return Ok(overview);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Starts the migration process
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<ImportJob>> StartMigration([FromBody] MigrationRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userId = _tenantProvider.GetUserId() ?? Guid.Empty;

        var job = await _migrationService.StartMigrationAsync(tenantId.Value, userId, request);
        return Ok(job);
    }
}

public class MigrationOverviewRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? ExtraCredentials { get; set; }
}
