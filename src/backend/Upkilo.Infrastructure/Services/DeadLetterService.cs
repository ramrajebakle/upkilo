using Microsoft.Extensions.Logging;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for handling failed background tasks and outbox messages.
/// Persists dead-lettered messages as AuditEntry records for observability and retry.
/// </summary>
public class DeadLetterService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DeadLetterService> _logger;

    public DeadLetterService(AppDbContext context, ILogger<DeadLetterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task MoveToDeadLetterAsync(OutboxMessage message, string reason, string? exception = null)
    {
        _logger.LogWarning("Message {MessageId} moved to Dead Letter Queue. Reason: {Reason}", message.Id, reason);

        // Mark the outbox message as permanently failed
        message.IsProcessed = true; // Mark processed so it won't be retried
        message.UpdatedAt = DateTime.UtcNow;

        // Persist dead-letter record as an audit entry for tracking and manual intervention
        var deadLetterDetails = JsonSerializer.Serialize(new
        {
            OriginalMessageId = message.Id,
            message.EventType,
            message.Payload,
            Reason = reason,
            Exception = exception,
            OriginalCreatedAt = message.CreatedAt,
            DeadLetteredAt = DateTime.UtcNow
        });

        _context.AuditEntries.Add(new AuditEntry
        {
            TenantId = message.TenantId,
            Action = "DeadLetter",
            EntityType = "OutboxMessage",
            EntityId = message.Id.ToString(),
            Details = deadLetterDetails,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
