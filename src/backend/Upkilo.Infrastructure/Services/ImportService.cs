using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Hangfire;

namespace Upkilo.Infrastructure.Services;

public class ImportService : IImportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ImportService> _logger;
    private readonly IBackgroundJobClient _jobClient;

    public ImportService(
        AppDbContext context,
        ILogger<ImportService> logger,
        IBackgroundJobClient jobClient)
    {
        _context = context;
        _logger = logger;
        _jobClient = jobClient;
    }

    public async Task<ImportAnalysis> AnalyzeImportAsync(Stream fileStream, string entityType)
    {
        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync();
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0) return new ImportAnalysis();

        var headers = ParseCsvLine(lines[0]);
        var previewRows = new List<Dictionary<string, string>>();

        for (int i = 1; i < Math.Min(lines.Length, 6); i++)
        {
            var values = ParseCsvLine(lines[i]);
            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Count && j < values.Count; j++)
            {
                row[headers[j]] = values[j];
            }
            previewRows.Add(row);
        }

        return new ImportAnalysis
        {
            Headers = headers,
            PreviewRows = previewRows,
            EstimatedRows = lines.Length - 1
        };
    }

    public async Task<ImportJob> StartImportAsync(
        Guid tenantId,
        Guid userId,
        string entityType,
        Stream fileStream,
        string fileName,
        Dictionary<string, string>? columnMapping = null)
    {
        // For background processing, we might need to save the file content or stream to a persistent store
        // For now, we'll read it into memory. In production, save to Azure Blob/Disk.
        using var reader = new StreamReader(fileStream);
        var csvContent = await reader.ReadToEndAsync();

        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EntityType = entityType,
            FileName = fileName,
            Status = "pending",
            ColumnMapping = columnMapping != null ? JsonSerializer.Serialize(columnMapping) : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ImportJob>().Add(job);
        await _context.SaveChangesAsync();

        // Enqueue background job
        _jobClient.Enqueue(() => ProcessImportBackgroundAsync(job.Id, csvContent));

        _logger.LogInformation("Import job {JobId} enqueued for {EntityType}", job.Id, entityType);
        return job;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessImportBackgroundAsync(Guid jobId, string csvContent)
    {
        var startTime = DateTime.UtcNow;
        var job = await _context.Set<ImportJob>().FindAsync(jobId);
        if (job == null) return;

        job.Status = "processing";
        await _context.SaveChangesAsync();

        var errors = new List<object>();
        var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return;
        }

        var headers = ParseCsvLine(lines[0]);
        var mapping = !string.IsNullOrEmpty(job.ColumnMapping)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(job.ColumnMapping)
            : null;

        job.TotalRows = lines.Length - 1;

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var values = ParseCsvLine(lines[i]);
                var rowData = new Dictionary<string, string>();
                for (int j = 0; j < headers.Count && j < values.Count; j++)
                {
                    rowData[headers[j]] = values[j];
                }

                await ProcessRowAsync(job.TenantId, job.EntityType, rowData, mapping);
                job.SuccessfulRows++;
            }
            catch (Exception ex)
            {
                job.FailedRows++;
                errors.Add(new { row = i + 1, error = ex.Message });
            }

            job.ProcessedRows++;

            // Periodically save progress
            if (i % 20 == 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        job.Status = job.FailedRows > 0 ? "completed_with_errors" : "completed";
        job.ErrorDetails = errors.Count > 0 ? JsonSerializer.Serialize(errors) : null;
        job.CompletedAt = DateTime.UtcNow;
        job.ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Import job {JobId} finished with {Success} successes and {Fail} failures",
            jobId, job.SuccessfulRows, job.FailedRows);
    }

    private async Task ProcessRowAsync(Guid tenantId, string entityType, Dictionary<string, string> row, Dictionary<string, string>? mapping)
    {
        string GetValue(string field)
        {
            if (mapping != null && mapping.TryGetValue(field, out var csvHeader))
            {
                return row.TryGetValue(csvHeader, out var val) ? val : string.Empty;
            }
            return row.TryGetValue(field, out var direct) ? direct : string.Empty;
        }

        if (entityType == "clients")
        {
            var email = GetValue("Email");
            if (string.IsNullOrEmpty(email)) throw new Exception("Email is required");

            var existing = await _context.Set<Client>()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Email == email);

            if (existing == null)
            {
                var client = new Client
                {
                    TenantId = tenantId,
                    Email = email,
                    FirstName = GetValue("FirstName"),
                    LastName = GetValue("LastName"),
                    Phone = GetValue("Phone"),
                    Notes = GetValue("Notes"),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Set<Client>().Add(client);
            }
            else
            {
                // Update basic info
                existing.FirstName = GetValue("FirstName") ?? existing.FirstName;
                existing.LastName = GetValue("LastName") ?? existing.LastName;
                existing.Phone = GetValue("Phone") ?? existing.Phone;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (entityType == "bookings")
        {
            // Simplified bookings import
            var clientEmail = GetValue("ClientEmail");
            var serviceName = GetValue("ServiceName");
            var dateStr = GetValue("Date");

            if (string.IsNullOrEmpty(clientEmail) || string.IsNullOrEmpty(serviceName) || string.IsNullOrEmpty(dateStr))
                throw new Exception("ClientEmail, ServiceName, and Date are required");

            // Logic to find/create client, find service, and create booking
            // ... (omitted for brevity in this step, but would involve DB lookups)
        }
    }

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    current.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());
        return result;
    }

    public async Task<ImportJob?> GetJobStatusAsync(Guid jobId)
    {
        return await _context.Set<ImportJob>().FindAsync(jobId);
    }

    public async Task<IEnumerable<ImportJob>> GetJobHistoryAsync(Guid tenantId, int limit = 10)
    {
        return await _context.Set<ImportJob>()
            .Where(j => j.TenantId == tenantId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public Task<byte[]> GetTemplateAsync(string entityType)
    {
        var template = entityType switch
        {
            "clients" => "FirstName,LastName,Email,Phone,Notes\nJohn,Doe,john@example.com,+1234567890,VIP Client",
            "bookings" => "ClientEmail,ServiceName,Date,Time,Duration\njohn@example.com,Haircut,2024-03-15,10:00,60",
            _ => "Field1,Field2,Field3"
        };

        return Task.FromResult(Encoding.UTF8.GetBytes(template));
    }
}
