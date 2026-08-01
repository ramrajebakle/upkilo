using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid tenantId, Guid? userId, string entityType, string entityId,
                  string action, object? oldValues, object? newValues,
                  string? ipAddress = null, string? userAgent = null);

    Task<IEnumerable<AuditEntry>> GetLogsAsync(Guid tenantId,
        string? entityType = null, string? entityId = null,
        DateTime? from = null, DateTime? to = null, int limit = 100);

    /// <summary>
    /// Export audit logs to JSON format
    /// </summary>
    Task<byte[]> ExportToJsonAsync(Guid tenantId, DateTime? from = null, DateTime? to = null,
        string? entityType = null, int maxRecords = 10000);

    /// <summary>
    /// Export audit logs to CSV format
    /// </summary>
    Task<byte[]> ExportToCsvAsync(Guid tenantId, DateTime? from = null, DateTime? to = null,
        string? entityType = null, int maxRecords = 10000);

    /// <summary>
    /// Get audit log summary statistics
    /// </summary>
    Task<AuditSummary> GetSummaryAsync(Guid tenantId, DateTime? from = null, DateTime? to = null);
}

public class AuditSummary
{
    public int TotalLogs { get; set; }
    public int CreateActions { get; set; }
    public int UpdateActions { get; set; }
    public int DeleteActions { get; set; }
    public Dictionary<string, int> ByEntityType { get; set; } = new();
    public Dictionary<string, int> ByUser { get; set; } = new();
    public DateTime? OldestLog { get; set; }
    public DateTime? NewestLog { get; set; }
}
