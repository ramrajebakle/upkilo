using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(AppDbContext context, ILogger<AuditService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(Guid tenantId, Guid? userId, string entityType, string entityId,
                               string action, object? oldValues, object? newValues,
                               string? ipAddress = null, string? userAgent = null)
    {
        var contextIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var contextAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = ipAddress ?? contextIp,
            UserAgent = userAgent ?? contextAgent,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditEntries.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Audit: {Action} on {EntityType}/{EntityId} by User {UserId}",
            action, entityType, entityId, userId);
    }

    public async Task<IEnumerable<AuditEntry>> GetLogsAsync(Guid tenantId,
        string? entityType = null, string? entityId = null,
        DateTime? from = null, DateTime? to = null, int limit = 100)
    {
        var query = BuildQuery(tenantId, entityType, from, to);

        if (!string.IsNullOrEmpty(entityId))
            query = query.Where(a => a.EntityId == entityId);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<byte[]> ExportToJsonAsync(Guid tenantId, DateTime? from = null, DateTime? to = null,
        string? entityType = null, int maxRecords = 10000)
    {
        var logs = await BuildQuery(tenantId, entityType, from, to)
            .OrderByDescending(a => a.Timestamp)
            .Take(maxRecords)
            .Select(a => new
            {
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Action,
                a.UserId,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.UserAgent,
                Timestamp = a.Timestamp.ToString("o")
            })
            .ToListAsync();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(new
        {
            exportedAt = DateTime.UtcNow.ToString("o"),
            tenantId = tenantId.ToString(),
            recordCount = logs.Count,
            logs
        }, options);

        _logger.LogInformation("Exported {Count} audit logs to JSON for tenant {TenantId}", logs.Count, tenantId);
        return Encoding.UTF8.GetBytes(json);
    }

    public async Task<byte[]> ExportToCsvAsync(Guid tenantId, DateTime? from = null, DateTime? to = null,
        string? entityType = null, int maxRecords = 10000)
    {
        var logs = await BuildQuery(tenantId, entityType, from, to)
            .OrderByDescending(a => a.Timestamp)
            .Take(maxRecords)
            .ToListAsync();

        var sb = new StringBuilder();

        // CSV Header
        sb.AppendLine("Id,Timestamp,EntityType,EntityId,Action,UserId,IpAddress,UserAgent,OldValues,NewValues");

        foreach (var log in logs)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(log.Id.ToString()),
                EscapeCsv(log.Timestamp.ToString("o")),
                EscapeCsv(log.EntityType),
                EscapeCsv(log.EntityId),
                EscapeCsv(log.Action),
                EscapeCsv(log.UserId?.ToString() ?? ""),
                EscapeCsv(log.IpAddress ?? ""),
                EscapeCsv(log.UserAgent ?? ""),
                EscapeCsv(log.OldValues ?? ""),
                EscapeCsv(log.NewValues ?? "")
            ));
        }

        _logger.LogInformation("Exported {Count} audit logs to CSV for tenant {TenantId}", logs.Count, tenantId);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<AuditSummary> GetSummaryAsync(Guid tenantId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditEntries.Where(a => a.TenantId == tenantId);

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        var totalLogs = await query.CountAsync();

        var actions = await query
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToListAsync();

        var entities = await query
            .GroupBy(a => a.EntityType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var users = await query
            .Where(a => a.UserId != null)
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var dates = await query.Select(a => a.Timestamp).ToListAsync();

        return new AuditSummary
        {
            TotalLogs = totalLogs,
            CreateActions = actions.FirstOrDefault(a => a.Action == "Create")?.Count ?? 0,
            UpdateActions = actions.FirstOrDefault(a => a.Action == "Update")?.Count ?? 0,
            DeleteActions = actions.FirstOrDefault(a => a.Action == "Delete")?.Count ?? 0,
            ByEntityType = entities.ToDictionary(e => e.Type, e => e.Count),
            ByUser = users.ToDictionary(u => u.UserId!, u => u.Count),
            OldestLog = dates.Any() ? dates.Min() : null,
            NewestLog = dates.Any() ? dates.Max() : null
        };
    }

    private IQueryable<AuditEntry> BuildQuery(Guid tenantId, string? entityType, DateTime? from, DateTime? to)
    {
        var query = _context.AuditEntries.Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        return query;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        // Escape quotes and wrap in quotes if contains special characters
        var escaped = value.Replace("\"", "\"\"");
        if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
        {
            return $"\"{escaped}\"";
        }
        return escaped;
    }
}
