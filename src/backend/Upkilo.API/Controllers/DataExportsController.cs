using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Hangfire;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class DataExportsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<DataExportsController> _logger;

    public DataExportsController(AppDbContext context, ITenantProvider tenantProvider, IBackgroundJobClient backgroundJobs, ILogger<DataExportsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetExports()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var exports = await _context.DataExports
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.RequestedAt)
            .ToListAsync();

        return Ok(exports);
    }

    /// <summary>
    /// Triggers an asynchronous export job
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> RequestExport([FromBody] ExportRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var export = new DataExport
        {
            TenantId = tenantId.Value,
            RequestedById = Guid.Parse(userId ?? Guid.Empty.ToString()),
            TargetEntity = request.TargetEntity,
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        };

        _context.DataExports.Add(export);
        await _context.SaveChangesAsync();

        // Enqueue background processing job
        _backgroundJobs.Enqueue<Upkilo.Infrastructure.Jobs.DataExportJob>(
            x => x.ExecuteAsync(export.Id, request.FiltersJson, CancellationToken.None));

        return Accepted(new { message = "Export job queued", exportId = export.Id });
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetExportStatus(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var export = await _context.DataExports.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (export == null) return NotFound();

        return Ok(new
        {
            export.Id,
            export.Status,
            export.TargetEntity,
            export.RequestedAt,
            export.CompletedAt,
            export.FileUrl,
            export.ErrorMessage
        });
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadExport(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var export = await _context.DataExports.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (export == null) return NotFound();
        if (export.Status != "Completed" || string.IsNullOrEmpty(export.FileUrl))
            return BadRequest(new { message = "Export is not ready for download." });

        // F-06: canonicalize and confirm the resolved path stays within wwwroot — defense in
        // depth against any traversal sequence that could reach a stored FileUrl value.
        var exportRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
        var filePath = Path.GetFullPath(Path.Combine(exportRoot, export.FileUrl.TrimStart('/')));
        if (!filePath.StartsWith(exportRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            _logger.LogWarning("Blocked export download outside wwwroot: {FilePath}", filePath);
            return BadRequest(new { message = "Invalid export path." });
        }

        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("Export file not found: {FilePath}", filePath);
            return NotFound(new { message = "Physical export file no longer exists." });
        }

        var fileName = Path.GetFileName(filePath);
        return PhysicalFile(filePath, "text/csv", fileName);
    }
}
