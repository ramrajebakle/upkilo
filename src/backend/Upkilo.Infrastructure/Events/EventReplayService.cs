using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Events;

/// <summary>
/// Event replay service — re-publishes historical domain events from the outbox
/// for debugging, projection rebuilding, or disaster recovery.
///
/// Safety: replay is idempotent when handlers check CorrelationId / ProcessedAt.
/// </summary>
public class EventReplayService
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;
    private readonly ILogger<EventReplayService> _logger;

    public EventReplayService(AppDbContext db, IPublisher publisher, ILogger<EventReplayService> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Replay all unprocessed outbox messages for a tenant within the given window.
    /// </summary>
    public async Task<EventReplayResult> ReplayAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        string? eventTypeFilter = null,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId
                     && m.CreatedAt >= from
                     && m.CreatedAt <= to);

        if (!string.IsNullOrEmpty(eventTypeFilter))
            query = query.Where(m => m.EventType == eventTypeFilter);

        var messages = await query
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var replayed = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var msg in messages)
        {
            if (dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would replay {EventType} (id={Id}) for tenant {TenantId}",
                    msg.EventType, msg.Id, tenantId);
                replayed++;
                continue;
            }

            try
            {
                // Re-mark for processing and save
                var replayMsg = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    TenantId = msg.TenantId,
                    EventType = msg.EventType,
                    Payload = msg.Payload,
                    IsProcessed = false,
                    CorrelationId = $"replay:{msg.Id}",
                    CreatedAt = DateTime.UtcNow,
                };

                _db.OutboxMessages.Add(replayMsg);
                replayed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue replay for event {EventType} (id={Id})", msg.EventType, msg.Id);
                errors.Add($"Event {msg.Id} ({msg.EventType}): {ex.Message}");
                skipped++;
            }
        }

        if (!dryRun && replayed > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Event replay complete for tenant {TenantId}: {Replayed} queued, {Skipped} skipped, dryRun={DryRun}",
            tenantId, replayed, skipped, dryRun);

        return new EventReplayResult
        {
            TenantId = tenantId,
            From = from,
            To = to,
            EventTypeFilter = eventTypeFilter,
            TotalFound = messages.Count,
            Replayed = replayed,
            Skipped = skipped,
            Errors = errors,
            IsDryRun = dryRun,
        };
    }

    /// <summary>
    /// Get a summary of events available for replay within a time window.
    /// </summary>
    public async Task<List<EventReplaySummary>> GetReplaySummaryAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.CreatedAt >= from && m.CreatedAt <= to)
            .GroupBy(m => m.EventType)
            .Select(g => new EventReplaySummary
            {
                EventType = g.Key,
                Count = g.Count(),
                Oldest = g.Min(m => m.CreatedAt),
                Newest = g.Max(m => m.CreatedAt),
            })
            .OrderByDescending(s => s.Count)
            .ToListAsync(cancellationToken);
    }
}

public class EventReplayResult
{
    public Guid TenantId { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public string? EventTypeFilter { get; init; }
    public int TotalFound { get; init; }
    public int Replayed { get; init; }
    public int Skipped { get; init; }
    public List<string> Errors { get; init; } = new();
    public bool IsDryRun { get; init; }
}

public class EventReplaySummary
{
    public string EventType { get; init; } = string.Empty;
    public int Count { get; init; }
    public DateTime Oldest { get; init; }
    public DateTime Newest { get; init; }
}
