using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using System.Text;

namespace Upkilo.Infrastructure.Jobs;

public class DataExportJob
{
    private readonly AppDbContext _context;
    private readonly ICloudStorageProvider _storage;
    private readonly ILogger<DataExportJob> _logger;

    private const string ExportContainer = "upkilo-exports";

    public DataExportJob(AppDbContext context, ICloudStorageProvider storage, ILogger<DataExportJob> logger)
    {
        _context = context;
        _storage = storage;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid exportId, string filtersJson, CancellationToken cancellationToken)
    {
        var export = await _context.DataExports.FindAsync(new object[] { exportId }, cancellationToken);
        if (export == null)
        {
            _logger.LogError("DataExport {ExportId} not found.", exportId);
            return;
        }

        export.Status = "Processing";
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Building CSV for {TargetEntity} - Export {ExportId}", export.TargetEntity, exportId);

            // Stream records into a MemoryStream to avoid building a huge in-memory string.
            using var ms = new MemoryStream();
            await using var writer = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true);

            if (export.TargetEntity == "Clients")
            {
                await writer.WriteLineAsync("Id,FirstName,LastName,Email,Phone,CreatedAt");
                await foreach (var c in _context.Clients
                    .Where(c => c.TenantId == export.TenantId)
                    .AsAsyncEnumerable().WithCancellation(cancellationToken))
                {
                    await writer.WriteLineAsync($"{c.Id},{Escape(c.FirstName)},{Escape(c.LastName)},{Escape(c.Email)},{Escape(c.Phone)},{c.CreatedAt:O}");
                }
            }
            else if (export.TargetEntity == "Bookings")
            {
                await writer.WriteLineAsync("Id,Service,Client,StartTime,Status");
                await foreach (var b in _context.Bookings
                    .Where(b => b.TenantId == export.TenantId)
                    .Include(b => b.Client)
                    .Include(b => b.Service)
                    .AsAsyncEnumerable().WithCancellation(cancellationToken))
                {
                    await writer.WriteLineAsync($"{b.Id},{Escape(b.Service?.Name)},{Escape(b.Client?.Email)},{b.StartTime:O},{b.Status}");
                }
            }

            await writer.FlushAsync();
            ms.Position = 0;

            var blobName = $"{export.TenantId}/{export.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            var url = await _storage.UploadAsync(ExportContainer, blobName, ms, "text/csv", cancellationToken);

            // Store a signed URL valid for 24 hours — not a public static path.
            var signedUrl = await _storage.GetSignedUrlAsync(ExportContainer, blobName, TimeSpan.FromHours(24), cancellationToken);
            export.FileUrl = signedUrl;
            export.CompletedAt = DateTime.UtcNow;
            export.Status = "Completed";

            _logger.LogInformation("Export {ExportId} finished. Blob: {Blob}", exportId, blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export {ExportId} failed.", exportId);
            export.Status = "Failed";
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string Escape(string? value)
    {
        if (value == null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
