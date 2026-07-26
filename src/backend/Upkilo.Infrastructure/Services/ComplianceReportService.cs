using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Generates compliance reports for audit, GDPR, and regulatory requirements
/// </summary>
public class ComplianceReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ComplianceReportService> _logger;

    public ComplianceReportService(AppDbContext context, ILogger<ComplianceReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ComplianceReport> GenerateReportAsync(Guid tenantId, DateTime from, DateTime to)
    {
        var report = new ComplianceReport
        {
            TenantId = tenantId,
            GeneratedAt = DateTime.UtcNow,
            PeriodStart = from,
            PeriodEnd = to
        };

        // Data Processing Activities
        report.DataProcessingActivities = await _context.DataProcessingLogs
            .Where(d => d.TenantId == tenantId && d.CreatedAt >= from && d.CreatedAt <= to)
            .CountAsync();

        // Consent Records
        report.ConsentRecords = await _context.GdprConsents
            .Where(c => c.TenantId == tenantId && c.CreatedAt >= from && c.CreatedAt <= to)
            .CountAsync();

        report.ActiveConsents = await _context.GdprConsents
            .Where(c => c.TenantId == tenantId && c.IsGranted)
            .CountAsync();

        report.RevokedConsents = await _context.GdprConsents
            .Where(c => c.TenantId == tenantId && !c.IsGranted && c.UpdatedAt >= from && c.UpdatedAt <= to)
            .CountAsync();

        // Data Export Requests (DSAR)
        report.DataExportRequests = await _context.DataExports
            .Where(d => d.TenantId == tenantId && d.CreatedAt >= from && d.CreatedAt <= to)
            .CountAsync();

        // Security Events
        report.SecurityEvents = await _context.SecurityEvents
            .Where(s => s.TenantId == tenantId && s.CreatedAt >= from && s.CreatedAt <= to)
            .CountAsync();

        // Audit Log Entries
        report.AuditLogEntries = await _context.AuditEntries
            .Where(a => a.TenantId == tenantId && a.Timestamp >= from && a.Timestamp <= to)
            .CountAsync();

        // Legal Agreements
        report.LegalAgreements = await _context.LegalAgreements
            .Where(l => l.TenantId == tenantId)
            .Select(l => new LegalAgreementSummary
            {
                Type = l.AgreementType,
                Version = l.Version,
                AcceptedAt = l.AcceptedAt ?? DateTime.MinValue
            })
            .ToListAsync();

        // Data Deletion Requests
        report.DeletionRequests = await _context.Clients
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId && c.IsDeleted && c.DeletedAt >= from && c.DeletedAt <= to)
            .CountAsync();

        _logger.LogInformation("Compliance report generated for tenant {TenantId} ({From} to {To})", tenantId, from, to);
        return report;
    }

    public async Task<string> GenerateDsarReportAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) throw new KeyNotFoundException("User not found");

        var bookings = await _context.Bookings
            .Where(b => b.CustomerId == userId.ToString() || b.CustomerEmail == user.Email)
            .ToListAsync();

        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var communications = await _context.CommunicationLogs
            .Where(c => c.ClientId == userId)
            .ToListAsync();

        var reportData = new
        {
            ReportGeneratedAt = DateTime.UtcNow,
            UserProfile = new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.Phone,
                user.CreatedAt,
                user.Role
            },
            Bookings = bookings.Select(b => new { b.Id, b.ServiceName, b.BookingDate, b.Status, b.Price }),
            Sessions = sessions.Select(s => new { s.IpAddress, s.Browser, s.CreatedAt, s.LastActiveAt }),
            Communications = communications.Select(c => new { c.Type, c.Subject, c.CreatedAt, c.Status })
        };

        _logger.LogInformation("GDPR DSAR report generated for user {UserId}", userId);
        
        return System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}

public class ComplianceReport
{
    public Guid TenantId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int DataProcessingActivities { get; set; }
    public int ConsentRecords { get; set; }
    public int ActiveConsents { get; set; }
    public int RevokedConsents { get; set; }
    public int DataExportRequests { get; set; }
    public int SecurityEvents { get; set; }
    public int AuditLogEntries { get; set; }
    public int DeletionRequests { get; set; }
    public List<LegalAgreementSummary> LegalAgreements { get; set; } = new();
}

public class LegalAgreementSummary
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; }
}
