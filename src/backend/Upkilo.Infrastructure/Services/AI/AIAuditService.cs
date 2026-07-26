using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services.AI;

public interface IAIAuditService
{
    Task LogAsync(AIAuditEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AIAuditEntry>> GetLogsAsync(Guid tenantId, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<AIAuditEntry>> GetPendingApprovalAsync(Guid tenantId, CancellationToken ct = default);
    Task ApproveAsync(string auditId, Guid approverId, CancellationToken ct = default);
    Task RejectAsync(string auditId, Guid approverId, string reason, CancellationToken ct = default);
}

public class AIAuditEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Feature { get; set; } = string.Empty;
    public string OriginalPrompt { get; set; } = string.Empty;
    public string SanitizedPrompt { get; set; } = string.Empty;
    public string? Response { get; set; }
    public bool WasBlocked { get; set; }
    public List<string> DetectedThreats { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public bool RequiresApproval { get; set; }
    public string ApprovalStatus { get; set; } = "none"; // none, pending, approved, rejected
    public Guid? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
    public int InputTokens { get; set; }
    public decimal Cost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DB-backed AI execution audit log persisted via AIDecisionLog.
/// Survives restarts and supports the human-approval workflow.
/// </summary>
public class AIAuditService : IAIAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AIAuditService> _logger;

    public AIAuditService(AppDbContext context, ILogger<AIAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AIAuditEntry entry, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
            entry.Id = Guid.NewGuid().ToString("N")[..12];

        var log = new AIDecisionLog
        {
            Id                 = Guid.NewGuid(),
            TenantId           = entry.TenantId,
            AgentName          = entry.Feature,
            DecisionType       = entry.WasBlocked ? "Blocked" : "Allowed",
            InputData          = entry.SanitizedPrompt,
            OutputDecision     = entry.Response ?? string.Empty,
            ConfidenceScore    = (decimal)entry.ConfidenceScore,
            InputTokens        = entry.InputTokens,
            RequiresHumanReview = entry.RequiresApproval,
            IsApproved         = entry.ApprovalStatus == "approved",
            Feedback           = entry.RejectionReason,
            // Store full audit context including original prompt and threats in Feedback JSON
            Model              = JsonSerializer.Serialize(new
            {
                auditId          = entry.Id,
                originalPrompt   = entry.OriginalPrompt,
                detectedThreats  = entry.DetectedThreats,
                wasBlocked       = entry.WasBlocked,
                approvalStatus   = entry.ApprovalStatus,
                cost             = entry.Cost,
                userId           = entry.UserId
            })
        };

        _context.AIDecisionLogs.Add(log);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "AI audit log persisted. AuditId={AuditId}, TenantId={TenantId}, Feature={Feature}, Blocked={Blocked}, RequiresApproval={RequiresApproval}",
            entry.Id, entry.TenantId, entry.Feature, entry.WasBlocked, entry.RequiresApproval);
    }

    public async Task<IReadOnlyList<AIAuditEntry>> GetLogsAsync(Guid tenantId, int limit = 50, CancellationToken ct = default)
    {
        var logs = await _context.AIDecisionLogs
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        return logs.Select(ToAuditEntry).ToList();
    }

    public async Task<IReadOnlyList<AIAuditEntry>> GetPendingApprovalAsync(Guid tenantId, CancellationToken ct = default)
    {
        var logs = await _context.AIDecisionLogs
            .Where(l => l.TenantId == tenantId && l.RequiresHumanReview && !l.IsApproved && l.ReviewedAt == null)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        return logs.Select(ToAuditEntry).ToList();
    }

    public async Task ApproveAsync(string auditId, Guid approverId, CancellationToken ct = default)
    {
        var log = await FindByAuditIdAsync(auditId, ct)
            ?? throw new KeyNotFoundException($"Audit entry '{auditId}' not found.");

        log.IsApproved  = true;
        log.ReviewedBy  = approverId;
        log.ReviewedAt  = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("AI audit entry approved. AuditId={AuditId}, ApprovedBy={ApproverId}", auditId, approverId);
    }

    public async Task RejectAsync(string auditId, Guid approverId, string reason, CancellationToken ct = default)
    {
        var log = await FindByAuditIdAsync(auditId, ct)
            ?? throw new KeyNotFoundException($"Audit entry '{auditId}' not found.");

        log.IsApproved  = false;
        log.ReviewedBy  = approverId;
        log.ReviewedAt  = DateTime.UtcNow;
        log.Feedback    = reason;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("AI audit entry rejected. AuditId={AuditId}, RejectedBy={ApproverId}, Reason={Reason}",
            auditId, approverId, reason);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<AIDecisionLog?> FindByAuditIdAsync(string auditId, CancellationToken ct)
    {
        // auditId is embedded in the Model JSON field
        return await _context.AIDecisionLogs
            .Where(l => l.Model != null && l.Model.Contains($"\"auditId\":\"{auditId}\""))
            .FirstOrDefaultAsync(ct);
    }

    private static AIAuditEntry ToAuditEntry(AIDecisionLog log)
    {
        var entry = new AIAuditEntry
        {
            TenantId        = log.TenantId,
            Feature         = log.AgentName,
            SanitizedPrompt = log.InputData,
            Response        = log.OutputDecision,
            ConfidenceScore = (double)log.ConfidenceScore,
            InputTokens     = log.InputTokens,
            RequiresApproval = log.RequiresHumanReview,
            ApprovalStatus  = log.IsApproved ? "approved" : (log.ReviewedAt.HasValue ? "rejected" : "pending"),
            ApprovedBy      = log.ReviewedBy,
            RejectionReason = log.Feedback,
            CreatedAt       = log.CreatedAt
        };

        // Restore fields from the embedded JSON context
        if (!string.IsNullOrWhiteSpace(log.Model))
        {
            try
            {
                using var doc = JsonDocument.Parse(log.Model);
                var root = doc.RootElement;
                if (root.TryGetProperty("auditId", out var id))          entry.Id = id.GetString() ?? entry.Id;
                if (root.TryGetProperty("originalPrompt", out var op))   entry.OriginalPrompt = op.GetString() ?? string.Empty;
                if (root.TryGetProperty("wasBlocked", out var wb))       entry.WasBlocked = wb.GetBoolean();
                if (root.TryGetProperty("cost", out var cost))           entry.Cost = cost.GetDecimal();
                if (root.TryGetProperty("userId", out var uid) && uid.ValueKind != JsonValueKind.Null)
                    entry.UserId = uid.GetGuid();
                if (root.TryGetProperty("detectedThreats", out var dt))
                    entry.DetectedThreats = dt.EnumerateArray().Select(t => t.GetString() ?? "").ToList();
            }
            catch { /* corrupt JSON — keep defaults */ }
        }

        return entry;
    }
}
