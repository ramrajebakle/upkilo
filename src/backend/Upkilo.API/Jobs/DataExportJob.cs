using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Text;

namespace Upkilo.API.Jobs;

/// <summary>
/// Hangfire background job for large data exports.
/// Bridges the gap between the mock controller logic and real platform services.
/// </summary>
public class DataExportJob
{
    private readonly AppDbContext _context;
    private readonly IExportService _exportService;
    private readonly ILogger<DataExportJob> _logger;
    private readonly IWebHostEnvironment _env;

    public DataExportJob(
        AppDbContext context,
        IExportService exportService,
        IWebHostEnvironment env,
        ILogger<DataExportJob> logger)
    {
        _context = context;
        _exportService = exportService;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Execute export job in background.
    /// </summary>
    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(Guid jobId, Guid tenantId, string exportType)
    {
        _logger.LogInformation("Starting {ExportType} export for job {JobId} and tenant {TenantId}", exportType, jobId, tenantId);

        var job = await _context.DataImportJobs.FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == tenantId);
        if (job == null) return;

        try
        {
            job.Status = "Processing";
            await _context.SaveChangesAsync();

            byte[]? data = null;
            string extension = "csv";

            if (exportType.Contains("Clients", StringComparison.OrdinalIgnoreCase))
            {
                data = await _exportService.ExportClientsToCsvAsync(tenantId);
            }
            else if (exportType.Contains("Bookings", StringComparison.OrdinalIgnoreCase))
            {
                data = await _exportService.ExportBookingsToCsvAsync(tenantId);
            }

            if (data == null || data.Length == 0)
            {
                job.Status = "Failed";
                job.ValidationErrors = "No data found to export.";
                job.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return;
            }

            // Save to disk for download
            var exportDir = Path.Combine(_env.ContentRootPath, "App_Data", "Exports", tenantId.ToString());
            Directory.CreateDirectory(exportDir);

            var fileName = $"export_{exportType}_{Guid.NewGuid():N}.{extension}";
            var filePath = Path.Combine(exportDir, fileName);

            await File.WriteAllBytesAsync(filePath, data);

            job.Status = "Completed";
            job.FileName = fileName;
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Export job {JobId} completed. File saved to {FilePath}", jobId, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export job {JobId} failed", jobId);
            job.Status = "Failed";
            job.ValidationErrors = $"System error: {ex.Message}";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw; // Let Hangfire retry if configured
        }
    }
}
