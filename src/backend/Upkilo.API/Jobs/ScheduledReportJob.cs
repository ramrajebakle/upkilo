using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Background job to process and send scheduled reports
/// </summary>
public class ScheduledReportJob
{
    private readonly AppDbContext _context;
    private readonly ICsvExportService _csvExportService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ScheduledReportJob> _logger;

    public ScheduledReportJob(
        AppDbContext context,
        ICsvExportService csvExportService,
        IEmailService emailService,
        ILogger<ScheduledReportJob> logger)
    {
        _context = context;
        _csvExportService = csvExportService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the job for a specific report definition
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(Guid reportDefinitionId)
    {
        var report = await _context.ReportDefinitions.FindAsync(reportDefinitionId);
        if (report == null || !report.IsScheduled || string.IsNullOrEmpty(report.Recipients))
        {
            _logger.LogWarning("Scheduled report {ReportId} not found or not active", reportDefinitionId);
            return;
        }

        _logger.LogInformation("Processing scheduled report: {ReportName} for tenant {TenantId}", report.Name, report.TenantId);

        try
        {
            // Simulate data fetching based on report type
            byte[] csvData;
            if (report.ReportType.Equals("revenue", StringComparison.OrdinalIgnoreCase))
            {
                var data = await _context.Payments
                    .Where(p => p.TenantId == report.TenantId && !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(500)
                    .Select(p => new { p.Id, p.Amount, p.CreatedAt, p.PaymentMethod, p.Status })
                    .ToListAsync();
                csvData = _csvExportService.ExportToCsv(data);
            }
            else if (report.ReportType.Equals("bookings", StringComparison.OrdinalIgnoreCase))
            {
                var data = await _context.Bookings
                    .Where(b => b.TenantId == report.TenantId && !b.IsDeleted)
                    .Include(b => b.Client)
                    .Include(b => b.Service)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(500)
                    .Select(b => new { 
                        b.Id, 
                        b.StartTime, 
                        Client = b.Client != null ? $"{b.Client.FirstName} {b.Client.LastName}" : "Unknown",
                        Service = b.Service != null ? b.Service.Name : "Unknown",
                        b.Status,
                        b.Price
                    })
                    .ToListAsync();
                csvData = _csvExportService.ExportToCsv(data);
            }
            else if (report.ReportType.Equals("clients", StringComparison.OrdinalIgnoreCase))
            {
                var data = await _context.Clients
                    .Where(c => c.TenantId == report.TenantId && !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(500)
                    .Select(c => new { c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.CreatedAt })
                    .ToListAsync();
                csvData = _csvExportService.ExportToCsv(data);
            }
            else
            {
                _logger.LogWarning("Unknown report type: {ReportType}", report.ReportType);
                return;
            }

            if (csvData == null || csvData.Length == 0)
            {
                _logger.LogWarning("No data found for report {ReportName}", report.Name);
                return;
            }

            var recipients = report.Recipients.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var email in recipients)
            {
                await _emailService.SendEmailAsync(
                    email.Trim(),
                    $"Scheduled Report: {report.Name}",
                    $@"<h2>{report.Name}</h2>
                       <p>Please find attached the latest scheduled report for your business. Generated on {DateTime.UtcNow:f} UTC.</p>
                       <p>This is an automated email from your Upkilo dashboard.</p>",
                    true, // IsBodyHtml
                    new List<(string, byte[])> { ($"{report.Name.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.csv", csvData) }
                );
            }

            _logger.LogInformation("Scheduled report {ReportName} sent successfully to {RecipientCount} recipients", report.Name, recipients.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scheduled report {ReportName}", report.Name);
            throw; // Hangfire will retry
        }
    }
}
