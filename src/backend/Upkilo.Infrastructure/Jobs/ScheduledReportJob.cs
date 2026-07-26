using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Jobs;

public class ScheduledReportJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ScheduledReportJob> _logger;

    public ScheduledReportJob(AppDbContext context, IEmailService emailService, ILogger<ScheduledReportJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting Scheduled Report Job");

        var dueReports = await _context.ReportDefinitions
            .Where(r => r.IsScheduled && (r.LastRunAt == null || r.LastRunAt < DateTime.UtcNow.AddDays(-1)))
            .ToListAsync();

        if (!dueReports.Any())
        {
            _logger.LogInformation("No scheduled reports due for delivery");
            return;
        }

        _logger.LogInformation("Found {Count} scheduled reports to process", dueReports.Count);

        foreach (var report in dueReports)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(report.ScheduledEmailRecipients))
                {
                    _logger.LogWarning("Report {ReportName} has no email recipients configured, skipping", report.Name);
                    continue;
                }

                // Generate a basic report summary email
                var subject = $"Scheduled Report: {report.Name} — {DateTime.UtcNow:yyyy-MM-dd}";
                var body = $"<h2>{report.Name}</h2>" +
                           $"<p>This is your scheduled report generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.</p>" +
                           $"<p>Report type: {report.ReportType}</p>" +
                           $"<p><em>Full report generation with data export is under development. " +
                           $"Please use the in-app Report Builder for detailed data.</em></p>";

                foreach (var email in report.ScheduledEmailRecipients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    await _emailService.SendEmailAsync(email, subject, body);
                }

                report.LastRunAt = DateTime.UtcNow;
                _logger.LogInformation("Sent scheduled report {ReportName} to {Recipients}", report.Name, report.ScheduledEmailRecipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send scheduled report {ReportId} ({ReportName})", report.Id, report.Name);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Scheduled Report Job completed — processed {Count} reports", dueReports.Count);
    }
}
