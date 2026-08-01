using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Background;

public class DeadLetterRetryJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<DeadLetterRetryJob> _logger;

    public DeadLetterRetryJob(AppDbContext context, ILogger<DeadLetterRetryJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var unresolvedMessages = await _context.DeadLetterMessages
            .Where(m => !m.IsResolved && m.Source == "OutboxProcessor")
            .Take(100)
            .ToListAsync();

        if (!unresolvedMessages.Any()) return;

        foreach (var dlq in unresolvedMessages)
        {
            // Transiently retry by re-inserting into the Outbox
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TenantId = dlq.TenantId ?? Guid.Empty,
                EventType = dlq.EventType,
                Payload = dlq.Payload,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false,
                ProcessedAt = null,
                RetryCount = 0 // Resetting retry count for the fresh outbox item
            };

            _context.OutboxMessages.Add(outboxMessage);

            dlq.IsResolved = true;
            dlq.ResolvedAt = DateTime.UtcNow;
            dlq.ResolutionNotes = $"Automatically re-queued as OutboxMessage {outboxMessage.Id}";

            _logger.LogInformation("Re-queued DLQ message {DlqId} as Outbox message {OutboxId}", dlq.Id, outboxMessage.Id);
        }

        await _context.SaveChangesAsync();
    }
}
