using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

// NOTE: This controller was previously duplicated — a second `ImportController` existed under
// Upkilo.API.Controllers.v1 with the same `[controller]` route (api/v1/import) and an overlapping
// `GET history` action. Two attribute-routed actions matching the same method+template throw
// AmbiguousMatchException at request time and break Swagger generation. The two controllers are
// now merged here into a single ImportController.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly IImportService _importService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        IImportService importService,
        ITenantProvider tenantProvider,
        ILogger<ImportController> logger)
    {
        _importService = importService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");
    private Guid GetUserId() => _tenantProvider.GetUserId()
        ?? throw new UnauthorizedAccessException("User context not available");

    /// <summary>
    /// Analyze a file to extract headers and preview rows
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<ImportAnalysis>> Analyze(IFormFile file, [FromQuery] string entityType = "clients")
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        using var stream = file.OpenReadStream();
        var analysis = await _importService.AnalyzeImportAsync(stream, entityType);
        return Ok(analysis);
    }

    /// <summary>
    /// Start an import job with column mapping
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<ImportJob>> Start([FromForm] FileImportRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return BadRequest("Tenant not found");

        // FIX: previously hardcoded to Guid.Empty, losing the audit trail of who ran the import.
        var userId = GetUserId();

        if (request.File == null || request.File.Length == 0)
            return BadRequest("No file uploaded");

        var mapping = !string.IsNullOrEmpty(request.MappingJson)
            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(request.MappingJson)
            : null;

        using var stream = request.File.OpenReadStream();
        var job = await _importService.StartImportAsync(
            tenantId.Value,
            userId,
            request.EntityType,
            stream,
            Path.GetFileName(request.File.FileName), // F-03: strip any client-supplied path components
            mapping);

        return Ok(job);
    }

    /// <summary>
    /// Upload CSV/Excel file to import clients (simple, no column mapping)
    /// </summary>
    [HttpPost("clients")]
    public async Task<IActionResult> ImportClients(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File is required" });

        if (!file.FileName.EndsWith(".csv") && !file.FileName.EndsWith(".xlsx"))
            return BadRequest(new { error = "Only CSV and Excel files are supported" });

        using var stream = file.OpenReadStream();
        var job = await _importService.StartImportAsync(
            GetTenantId(), GetUserId(), "clients", stream, Path.GetFileName(file.FileName));

        return Ok(new
        {
            jobId = job.Id,
            status = job.Status,
            message = "Import started. Check status using GET /api/v1/import/status/{jobId}"
        });
    }

    /// <summary>
    /// Get status of an import job
    /// </summary>
    [HttpGet("status/{jobId}")]
    public async Task<ActionResult<ImportJob>> GetStatus(Guid jobId)
    {
        var job = await _importService.GetJobStatusAsync(jobId);
        if (job == null) return NotFound();

        // VULN-A06 FIX: validate tenant ownership after fetch — otherwise any authenticated
        // user could read another tenant's import job status (IDOR). 404 (not 403) avoids
        // ID enumeration.
        if (job.TenantId != GetTenantId())
            return NotFound();

        return Ok(job);
    }

    /// <summary>
    /// Check import job status (legacy clients-scoped path)
    /// </summary>
    [HttpGet("clients/{jobId}/status")]
    public async Task<IActionResult> GetJobStatus(Guid jobId)
    {
        var job = await _importService.GetJobStatusAsync(jobId);
        if (job == null)
            return NotFound(new { error = "Job not found" });

        if (job.TenantId != GetTenantId())
            return NotFound(new { error = "Job not found" }); // 404 not 403 to avoid ID enumeration

        return Ok(new
        {
            job.Id,
            job.Status,
            job.TotalRows,
            job.ProcessedRows,
            job.SuccessfulRows,
            job.FailedRows,
            job.ErrorDetails,
            job.CreatedAt,
            job.CompletedAt
        });
    }

    /// <summary>
    /// Get import history for the tenant
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<ImportJob>>> GetHistory([FromQuery] int limit = 10)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return BadRequest("Tenant not found");

        var history = await _importService.GetJobHistoryAsync(tenantId.Value, limit);
        return Ok(history);
    }

    /// <summary>
    /// Download a CSV template for the entity type
    /// </summary>
    [HttpGet("template/{entityType}")]
    public async Task<IActionResult> GetTemplate(string entityType)
    {
        var bytes = await _importService.GetTemplateAsync(entityType);
        return File(bytes, "text/csv", $"{entityType}-template.csv");
    }

    /// <summary>
    /// Download import template (legacy clients-scoped path)
    /// </summary>
    [HttpGet("clients/template")]
    [AllowAnonymous]
    public async Task<IActionResult> GetClientsTemplate()
    {
        var template = await _importService.GetTemplateAsync("clients");
        return File(template, "text/csv", "clients_import_template.csv");
    }
}

// Named FileImportRequest to avoid colliding with the pre-existing
// Upkilo.API.Controllers.ImportRequest DTO in DataOperationsController.
public class FileImportRequest
{
    public IFormFile File { get; set; } = null!;
    public string EntityType { get; set; } = "clients";
    public string? MappingJson { get; set; }
}
