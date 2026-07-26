using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ITenantProvider tenantProvider, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() ?? Guid.Empty;

    /// <summary>
    /// Get audit logs for tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100)
    {
        var logs = await _auditService.GetLogsAsync(GetTenantId(), entityType, entityId, from, to, limit);
        return Ok(logs);
    }

    /// <summary>
    /// Get audit log summary statistics
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var summary = await _auditService.GetSummaryAsync(GetTenantId(), from, to);
        return Ok(summary);
    }

    /// <summary>
    /// Export audit logs as JSON file
    /// </summary>
    [HttpGet("export/json")]
    [Produces("application/json")]
    public async Task<IActionResult> ExportJson(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? entityType = null,
        [FromQuery] int maxRecords = 10000)
    {
        var tenantId = GetTenantId();
        var bytes = await _auditService.ExportToJsonAsync(tenantId, from, to, entityType, maxRecords);
        
        var fileName = $"audit-logs-{tenantId:N}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return File(bytes, "application/json", fileName);
    }

    /// <summary>
    /// Export audit logs as CSV file
    /// </summary>
    [HttpGet("export/csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? entityType = null,
        [FromQuery] int maxRecords = 10000)
    {
        var tenantId = GetTenantId();
        var bytes = await _auditService.ExportToCsvAsync(tenantId, from, to, entityType, maxRecords);
        
        var fileName = $"audit-logs-{tenantId:N}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// [Enterprise Only] Enqueue a background export of audit logs
    /// </summary>
    [HttpPost("export/enqueue")]
    [Authorize(Roles = "Owner,EnterpriseAdmin")]
    public IActionResult EnqueueExport([FromBody] AuditExportRequest request)
    {
        var tenantId = GetTenantId();
        var jobId = Hangfire.BackgroundJob.Enqueue<Upkilo.Infrastructure.Jobs.AuditLogExportJob>(
            job => job.RunAsync(tenantId, request.From ?? DateTime.UtcNow.AddDays(-30), request.To ?? DateTime.UtcNow, request.Format ?? "csv")
        );

        return Accepted(new { jobId, message = "Export job enqueued. You will be notified when it is ready." });
    }
}

public class AuditExportRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Format { get; set; } // "csv" or "json"
}

