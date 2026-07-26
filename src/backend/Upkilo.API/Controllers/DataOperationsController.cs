using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using System.Text.Json;
using System.Text;

namespace Upkilo.API.Controllers;

/// <summary>
/// Import/Export controller for bulk data operations.
/// Uses DataImportJob for tracking both import and export status.
/// Background processing via Hangfire (ClientImportJob / BookingImportJob).
/// </summary>
[ApiController]
// Was "api/data" — every web caller uses "/api/v1/data/import|export/...", so the missing version
// segment made the data-import/export page 404. Aligned with the versioned convention.
[Route("api/v1/data")]
[Authorize]
public partial class DataOperationsController : ControllerBase
{
    private readonly ILogger<DataOperationsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IWebHostEnvironment _env;

    public DataOperationsController(
        ILogger<DataOperationsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _env = env;
    }

    /// <summary>
    /// Import clients from CSV
    /// </summary>
    [HttpPost("import/clients")]
    public async Task<IActionResult> ImportClients([FromForm] ImportRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.File == null || request.File.Length == 0)
            return BadRequest("File is required");

        // 1. Save file
        var uploadDir = Path.Combine(_env.ContentRootPath, "App_Data", "Imports", tenantId.Value.ToString());
        Directory.CreateDirectory(uploadDir);
        // F-03: never trust IFormFile.FileName — strip any client-supplied path components,
        // then canonicalize and confirm the result stays inside the tenant upload directory.
        var safeLeaf = Path.GetFileName(request.File.FileName);
        var fileName = $"{Guid.NewGuid()}_{safeLeaf}";
        var filePath = Path.GetFullPath(Path.Combine(uploadDir, fileName));
        if (!filePath.StartsWith(Path.GetFullPath(uploadDir) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return BadRequest(new { message = "Invalid file name." });

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        // 2. Create Job
        var job = new DataImportJob
        {
            TenantId = tenantId.Value,
            ImportType = "Clients",
            FileName = fileName,
            Status = "Pending",
            TotalRows = 0,
            ProcessedRows = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.DataImportJobs.Add(job);
        await _context.SaveChangesAsync();

        // 3. Enqueue Hangfire background job (reliable, with retry)
        Hangfire.BackgroundJob.Enqueue<Jobs.ClientImportJob>(
            j => j.ExecuteAsync(job.Id, tenantId.Value, filePath));

        return Accepted(new
        {
            jobId = job.Id,
            status = "pending",
            message = "Import processing started."
        });
    }

    /// <summary>
    /// Import bookings from CSV
    /// </summary>
    [HttpPost("import/bookings")]
    public async Task<IActionResult> ImportBookings([FromForm] ImportRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.File == null || request.File.Length == 0)
            return BadRequest("File is required");

        // 1. Save file
        var uploadDir = Path.Combine(_env.ContentRootPath, "App_Data", "Imports", tenantId.Value.ToString());
        Directory.CreateDirectory(uploadDir);
        // F-03: never trust IFormFile.FileName — strip any client-supplied path components,
        // then canonicalize and confirm the result stays inside the tenant upload directory.
        var safeLeaf = Path.GetFileName(request.File.FileName);
        var fileName = $"{Guid.NewGuid()}_{safeLeaf}";
        var filePath = Path.GetFullPath(Path.Combine(uploadDir, fileName));
        if (!filePath.StartsWith(Path.GetFullPath(uploadDir) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return BadRequest(new { message = "Invalid file name." });

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        // 2. Create Job
        var job = new DataImportJob
        {
            TenantId = tenantId.Value,
            ImportType = "Bookings",
            FileName = fileName,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _context.DataImportJobs.Add(job);
        await _context.SaveChangesAsync();

        // 3. Enqueue Hangfire background job
        Hangfire.BackgroundJob.Enqueue<Jobs.BookingImportJob>(
            j => j.ExecuteAsync(job.Id, tenantId.Value, filePath));

        return Accepted(new
        {
            jobId = job.Id,
            status = "pending",
            message = "Booking import processing started."
        });
    }

    /// <summary>
    /// Get import job status
    /// </summary>
    [HttpGet("import/{jobId}/status")]
    public async Task<IActionResult> GetImportStatus(Guid jobId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var job = await _context.DataImportJobs.FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId.Value);
        if (job == null) return NotFound();

        return Ok(new
        {
            jobId = job.Id,
            status = job.Status.ToLower(),
            totalRows = job.TotalRows,
            processedRows = job.ProcessedRows,
            successRows = job.SuccessRows,
            errorRows = job.ErrorRows,
            duplicatesFound = job.DuplicatesFound,
            errors = string.IsNullOrEmpty(job.ValidationErrors) ? new object[0] : JsonSerializer.Deserialize<object[]>(job.ValidationErrors),
            completedAt = job.CompletedAt
        });
    }

    /// <summary>
    /// Export clients to CSV
    /// </summary>
    [HttpPost("export/clients")]
    public async Task<IActionResult> ExportClients([FromBody] ExportRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var job = new DataImportJob
        {
            TenantId = tenantId.Value,
            ImportType = "Export_Clients",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.DataImportJobs.Add(job);
        await _context.SaveChangesAsync();

        // Enqueue real export job
        Hangfire.BackgroundJob.Enqueue<Jobs.DataExportJob>(
            j => j.ExecuteAsync(job.Id, tenantId.Value, "Clients"));

        return Accepted(new
        {
            jobId = job.Id,
            status = "pending",
            message = "Client export started in the background."
        });
    }

    /// <summary>
    /// Export bookings to CSV
    /// </summary>
    [HttpPost("export/bookings")]
    public async Task<IActionResult> ExportBookings([FromBody] ExportRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var job = new DataImportJob
        {
            TenantId = tenantId.Value,
            ImportType = "Export_Bookings",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.DataImportJobs.Add(job);
        await _context.SaveChangesAsync();

        // Enqueue real export job
        Hangfire.BackgroundJob.Enqueue<Jobs.DataExportJob>(
            j => j.ExecuteAsync(job.Id, tenantId.Value, "Bookings"));

        return Accepted(new { jobId = job.Id, status = "pending", message = "Booking export started in the background." });
    }

    /// <summary>
    /// Get export job status and download link
    /// </summary>
    [HttpGet("export/{jobId}/status")]
    public async Task<IActionResult> GetExportStatus(Guid jobId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var job = await _context.DataImportJobs.FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId.Value);
        if (job == null) return NotFound();

        // No more auto-marking as completed here. Status is managed by the background job.
        return Ok(new
        {
            jobId = job.Id,
            status = job.Status.ToLower(),
            completedAt = job.CompletedAt,
            downloadUrl = job.Status == "Completed" ? $"/api/data/download/{jobId}" : null
        });
    }

    [HttpGet("download/{jobId}")]
    public async Task<IActionResult> DownloadExport(Guid jobId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var job = await _context.DataImportJobs.FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId.Value);
        if (job == null || job.Status != "Completed" || string.IsNullOrEmpty(job.FileName)) 
            return NotFound("Export file not ready or job not found.");

        var filePath = Path.Combine(_env.ContentRootPath, "App_Data", "Exports", tenantId.Value.ToString(), job.FileName);
        
        if (!System.IO.File.Exists(filePath))
            return NotFound("Export file was missing on the server storage.");

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, "text/csv", job.FileName);
    }

    /// <summary>
    /// Get import templates
    /// </summary>
    [HttpGet("import/templates")]
    public IActionResult GetImportTemplates()
    {
        return Ok(new
        {
            templates = new[]
            {
                new { type = "clients", requiredFields = new[] { "first_name", "last_name", "email" } },
                new { type = "bookings", requiredFields = new[] { "client_email", "service_name", "date", "time" } },
                new { type = "services", requiredFields = new[] { "name", "duration", "price" } }
            }
        });
    }

    /// <summary>
    /// Validate import file
    /// </summary>
    [HttpPost("import/validate")]
    public async Task<IActionResult> ValidateImport([FromForm] ImportRequest request)
    {
        if (request.File == null) return BadRequest("No file");

        // Read the file to count rows and validate structure
        using var reader = new StreamReader(request.File.OpenReadStream());
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrEmpty(headerLine))
            return BadRequest(new { valid = false, error = "File is empty" });

        var headers = headerLine.Split(',');
        int rowCount = 0;
        var previewRows = new List<object>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            rowCount++;
            if (rowCount <= 3 && !string.IsNullOrEmpty(line)) // Preview first 3 rows
            {
                previewRows.Add(new { row = rowCount, data = line.Split(',').Take(5).ToArray(), status = "ok" });
            }
        }

        return Ok(new
        {
            valid = true,
            totalRows = rowCount,
            headers = headers.Select(h => h.Trim()).ToArray(),
            preview = previewRows
        });
    }

    // ─── GDPR Compliance Endpoints ───
    // Export uses Hangfire (DataExportJob) for background processing.
    // Delete uses anonymization (soft-delete) to preserve financial records.
    [HttpPost("gdpr/export")]
    public async Task<IActionResult> GdprExport([FromBody] GdprExportRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == request.Email && c.TenantId == tenantId);
        if (client == null) return NotFound("Client not found for GDPR export");

        var job = new DataImportJob
        {
            TenantId = tenantId.Value,
            ImportType = "Export_GDPR",
            FileName = $"GDPR_Export_{client.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.json",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _context.DataImportJobs.Add(job);
        await _context.SaveChangesAsync();

        Hangfire.BackgroundJob.Enqueue<Jobs.DataExportJob>(
            j => j.ExecuteAsync(job.Id, tenantId.Value, "GDPR_" + client.Id.ToString()));

        return Accepted(new { message = "GDPR export initiated. The user will be notified when ready." });
    }

    [HttpPost("gdpr/delete")]
    public async Task<IActionResult> GdprDelete([FromBody] GdprDeleteRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrEmpty(request.ConfirmationCode) || request.ConfirmationCode != "DELETE")
            return BadRequest("Invalid confirmation code");

        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == request.Email && c.TenantId == tenantId);
        if (client == null) return NotFound("Client not found");

        var originalEmail = client.Email;
        var clientId = client.Id;

        // Anonymize instead of hard delete to keep financial/booking records intact
        client.IsDeleted = true;
        client.FirstName = "Anonymized";
        client.LastName = "Anonymized";
        client.Email = $"anonymized_{Guid.NewGuid()}@deleted.local";
        client.Phone = null;
        client.Notes = "GDPR Deletion Request Executed";
        
        // Create GDPR compliance audit trail
        _context.AuditEntries.Add(new AuditEntry
        {
            TenantId = tenantId.Value,
            UserId = Guid.TryParse((User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value), out var uid) ? uid : Guid.Empty,
            Action = "GdprDelete",
            EntityType = "Client",
            EntityId = clientId.ToString(),
            Details = $"GDPR Right to Erasure executed. Original email hash: {Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(originalEmail ?? string.Empty)))}",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _logger.LogWarning("GDPR Delete executed for client {ClientId} in tenant {TenantId}", clientId, tenantId);

        return Ok(new { success = true, message = "Client data has been anonymized according to GDPR." });
    }
}

// CSV helper
public partial class DataOperationsController
{
    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

// Request DTOs
public class ImportRequest
{
    public IFormFile? File { get; set; }
    public bool SkipDuplicates { get; set; }
    public bool UpdateExisting { get; set; }
}

public class ExportRequest
{
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public List<string>? Fields { get; set; }
    public string? TargetEntity { get; set; }
    public string? FiltersJson { get; set; }
    public string Format { get; set; } = "csv"; // csv, xlsx
}

public class GdprExportRequest
{
    public string Email { get; set; } = string.Empty;
}

public class GdprDeleteRequest
{
    public string Email { get; set; } = string.Empty;
    public string ConfirmationCode { get; set; } = string.Empty;
}
