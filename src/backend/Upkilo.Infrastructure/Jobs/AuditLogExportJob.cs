using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Jobs
{
    public class AuditLogExportJob
    {
        private readonly AppDbContext _context;
        private readonly ICsvExportService _csvService;
        private readonly IFileService _fileService;
        private readonly ILogger<AuditLogExportJob> _logger;

        public AuditLogExportJob(
            AppDbContext context,
            ICsvExportService csvService,
            IFileService fileService,
            ILogger<AuditLogExportJob> logger)
        {
            _context = context;
            _csvService = csvService;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task RunAsync(Guid tenantId, DateTime start, DateTime end, string format = "csv")
        {
            _logger.LogInformation("Starting audit log export for tenant {TenantId} from {Start} to {End}", tenantId, start, end);

            var logs = await _context.AuditEntries
                .Where(l => l.TenantId == tenantId && l.Timestamp >= start && l.Timestamp <= end)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            byte[] content;
            string fileName = $"audit-log-{tenantId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            if (format.ToLower() == "json")
            {
                var options = new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                content = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(logs, options));
                fileName += ".json";
            }
            else
            {
                // We use the specialized CSV export service which handles AuditEntry specifically if implemented, 
                // or a generic one. Let's assume ICsvExportService is generic or we map it.
                content = await _csvService.ExportToCsvAsync(logs);
                fileName += ".csv";
            }

            // Save to storage (S3/Azure Blob/Local)
            var url = await _fileService.SaveFileAsync(content, fileName, "application/octet-stream", tenantId);
            
            _logger.LogInformation("Audit log export completed. URL: {Url}", url);
            
            // In a real system, we'd notify the user via email/notification that the export is ready
        }
    }
}
