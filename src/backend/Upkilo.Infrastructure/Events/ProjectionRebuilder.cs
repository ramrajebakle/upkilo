using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using System.Collections.Concurrent;

namespace Upkilo.Infrastructure.Events;

/// <summary>
/// Projection Rebuilder — replays stored OutboxMessages through registered
/// projection handlers to reconstruct read models from scratch.
///
/// Usage:
///   services.AddSingleton&lt;ProjectionRebuilder&gt;();
///
///   // Register a projection:
///   rebuilder.Register("DashboardStats", async (events, ct) => { ... });
///
///   // Trigger a full rebuild:
///   await rebuilder.RebuildAsync(tenantId, projectionName, dryRun: false, ct);
/// </summary>
public class ProjectionRebuilder
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ProjectionRebuilder> _logger;

    // Registered projection handlers: projectionName → async handler
    private readonly ConcurrentDictionary<string, Func<IReadOnlyList<OutboxMessage>, CancellationToken, Task<ProjectionRebuildResult>>>
        _handlers = new(StringComparer.OrdinalIgnoreCase);

    // In-memory consistency snapshots
    private readonly ConcurrentDictionary<string, ProjectionCheckpoint> _checkpoints = new();

    public ProjectionRebuilder(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<ProjectionRebuilder> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    // ── Registration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Register a named projection handler. The handler receives all relevant
    /// OutboxMessages in order and is responsible for updating its read model.
    /// </summary>
    public void Register(
        string projectionName,
        Func<IReadOnlyList<OutboxMessage>, CancellationToken, Task<ProjectionRebuildResult>> handler)
    {
        _handlers[projectionName] = handler;
        _logger.LogDebug("Projection handler registered: {ProjectionName}", projectionName);
    }

    /// <summary>Returns the list of registered projection names.</summary>
    public IReadOnlyList<string> RegisteredProjections => _handlers.Keys.ToList();

    // ── Rebuild ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Replay all OutboxMessages for a tenant through the named projection.
    /// </summary>
    /// <param name="tenantId">Tenant scope (null = all tenants)</param>
    /// <param name="projectionName">Which projection to rebuild (null = all)</param>
    /// <param name="eventTypeFilter">Optional event type filter (e.g. "BookingCreated")</param>
    /// <param name="from">Replay from this UTC timestamp</param>
    /// <param name="dryRun">If true, simulate but do not persist changes</param>
    public async Task<IReadOnlyList<ProjectionRebuildResult>> RebuildAsync(
        Guid? tenantId = null,
        string? projectionName = null,
        string? eventTypeFilter = null,
        DateTime? from = null,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProjectionRebuildResult>();
        var started = DateTime.UtcNow;

        // Determine which projections to run
        var targets = string.IsNullOrWhiteSpace(projectionName)
            ? _handlers.ToList()
            : _handlers.Where(h => h.Key.Equals(projectionName, StringComparison.OrdinalIgnoreCase)).ToList();

        if (targets.Count == 0)
        {
            var msg = projectionName != null
                ? $"No handler registered for projection '{projectionName}'"
                : "No projection handlers registered";

            results.Add(new ProjectionRebuildResult
            {
                ProjectionName = projectionName ?? "*",
                Status = "skipped",
                Message = msg,
                StartedAt = started,
                CompletedAt = DateTime.UtcNow
            });
            return results;
        }

        // Load OutboxMessages from DB
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var query = db.OutboxMessages.AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(m => m.TenantId == tenantId.Value);

        if (!string.IsNullOrWhiteSpace(eventTypeFilter))
            query = query.Where(m => m.EventType == eventTypeFilter);

        if (from.HasValue)
            query = query.Where(m => m.CreatedAt >= from.Value);

        var messages = await query
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "ProjectionRebuilder: loaded {Count} events for rebuild (dryRun={DryRun})",
            messages.Count, dryRun);

        // Run each handler
        foreach (var (name, handler) in targets)
        {
            var projStart = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Rebuilding projection '{Name}'...", name);

                ProjectionRebuildResult result;
                if (dryRun)
                {
                    result = new ProjectionRebuildResult
                    {
                        ProjectionName = name,
                        Status = "dry_run",
                        EventsProcessed = messages.Count,
                        Message = $"Dry run: {messages.Count} events would be processed",
                        StartedAt = projStart,
                        CompletedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    result = await handler(messages.AsReadOnly(), cancellationToken);
                    result.ProjectionName = name;
                    result.StartedAt = projStart;
                    result.CompletedAt = DateTime.UtcNow;
                }

                // Update checkpoint
                _checkpoints[name] = new ProjectionCheckpoint
                {
                    ProjectionName = name,
                    LastRebuiltAt = DateTime.UtcNow,
                    LastEventCount = messages.Count,
                    Status = result.Status,
                    IsDryRun = dryRun
                };

                results.Add(result);
                _logger.LogInformation(
                    "Projection '{Name}' rebuilt: {Status} ({Events} events, {Ms}ms)",
                    name, result.Status, result.EventsProcessed,
                    (result.CompletedAt - result.StartedAt).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Projection '{Name}' rebuild failed", name);
                results.Add(new ProjectionRebuildResult
                {
                    ProjectionName = name,
                    Status = "error",
                    Message = ex.Message,
                    StartedAt = projStart,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        return results;
    }

    // ── Consistency monitoring ────────────────────────────────────────────────

    /// <summary>
    /// Returns a consistency report for all registered projections.
    /// Compares the latest checkpoint against the current OutboxMessage count.
    /// </summary>
    public async Task<IReadOnlyList<ProjectionConsistencyReport>> GetConsistencyReportAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var query = db.OutboxMessages.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(m => m.TenantId == tenantId.Value);

        var totalEvents = await query.CountAsync(cancellationToken);
        var latestEvent = await query.MaxAsync(m => (DateTime?)m.CreatedAt, cancellationToken);

        var reports = new List<ProjectionConsistencyReport>();

        foreach (var name in _handlers.Keys)
        {
            var cp = _checkpoints.TryGetValue(name, out var checkpoint) ? checkpoint : null;
            var lag = cp == null ? totalEvents : Math.Max(0, totalEvents - cp.LastEventCount);
            var isStale = cp == null || (latestEvent.HasValue && cp.LastRebuiltAt < latestEvent.Value);

            reports.Add(new ProjectionConsistencyReport
            {
                ProjectionName = name,
                LastRebuiltAt = cp?.LastRebuiltAt,
                LastEventCount = cp?.LastEventCount ?? 0,
                CurrentEventCount = totalEvents,
                EventLag = lag,
                LatestEventAt = latestEvent,
                IsStale = isStale,
                Status = cp?.Status ?? "never_built",
                RecommendRebuild = isStale || lag > 0
            });
        }

        return reports;
    }

    /// <summary>Returns the checkpoint for a named projection.</summary>
    public ProjectionCheckpoint? GetCheckpoint(string projectionName)
        => _checkpoints.TryGetValue(projectionName, out var cp) ? cp : null;
}

// ─── Supporting types ─────────────────────────────────────────────────────────

public class ProjectionRebuildResult
{
    public string ProjectionName { get; set; } = "";
    public string Status { get; set; } = "ok"; // ok | error | dry_run | skipped
    public int EventsProcessed { get; set; }
    public string? Message { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public double DurationMs => (CompletedAt - StartedAt).TotalMilliseconds;
}

public class ProjectionCheckpoint
{
    public string ProjectionName { get; set; } = "";
    public DateTime LastRebuiltAt { get; set; }
    public int LastEventCount { get; set; }
    public string Status { get; set; } = "";
    public bool IsDryRun { get; set; }
}

public class ProjectionConsistencyReport
{
    public string ProjectionName { get; set; } = "";
    public DateTime? LastRebuiltAt { get; set; }
    public int LastEventCount { get; set; }
    public int CurrentEventCount { get; set; }
    public int EventLag { get; set; }
    public DateTime? LatestEventAt { get; set; }
    public bool IsStale { get; set; }
    public bool RecommendRebuild { get; set; }
    public string Status { get; set; } = "";
}
