using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Jobs;

/// <summary>
/// Background job to process pending outbox messages and deliver them (Webhooks, External integrations).
/// </summary>
public class OutboxProcessor
{
    private readonly AppDbContext _context;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        AppDbContext context,
        IWebhookService webhookService,
        ILogger<OutboxProcessor> logger)
    {
        _context = context;
        _webhookService = webhookService;
        _logger = logger;
    }

    // SC5: DLQ exponential backoff schedule — 1min, 5min, 15min, then dead-letter.
    private static readonly TimeSpan[] BackoffSchedule = { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) };

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task ProcessPendingMessagesAsync()
    {
        var now = DateTime.UtcNow;

        // Only pick up messages whose next-retry time has passed
        _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));
        var messages = await _context.OutboxMessages
            .Where(m => !m.IsProcessed && !m.IsDeadLetter && m.RetryCount <= BackoffSchedule.Length &&
                        (m.NextRetryAt == null || m.NextRetryAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync();

        if (!messages.Any()) return;

        _logger.LogInformation("[SC5] Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                if (message.EventType.StartsWith("Webhook.", StringComparison.OrdinalIgnoreCase))
                {
                    using var dispatchCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await _webhookService.DispatchEventAsync(message.TenantId, message.EventType, message.Payload);
                }

                message.IsProcessed = true;
                message.ProcessedAt = now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SC5] Failed outbox message {Id} (attempt {Attempt})", message.Id, message.RetryCount + 1);
                message.Error = ex.ToString();

                if (message.RetryCount >= BackoffSchedule.Length)
                {
                    // All backoff slots exhausted — move to dead-letter queue
                    message.IsDeadLetter = true;
                    message.DeadLetteredAt = now;
                    _logger.LogCritical(
                        "[SC5] Message {Id} ({EventType}) dead-lettered after {Attempts} attempts. Manual intervention required.",
                        message.Id, message.EventType, message.RetryCount + 1);
                }
                else
                {
                    // Exponential backoff using 0-based index before incrementing: 1min → 5min → 15min
                    message.NextRetryAt = now.Add(BackoffSchedule[message.RetryCount]);
                    message.RetryCount++;
                    _logger.LogWarning("[SC5] Message {Id} scheduled for retry at {At}", message.Id, message.NextRetryAt);
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>SC5: Returns all dead-lettered messages for operator review.</summary>
    public async Task<IReadOnlyList<OutboxMessage>> GetDeadLetterQueueAsync()
    {
        return await _context.OutboxMessages
            .Where(m => m.IsDeadLetter)
            .OrderByDescending(m => m.DeadLetteredAt)
            .ToListAsync();
    }

    /// <summary>SC5: Requeue a dead-lettered message for retry.</summary>
    public async Task RequeueDeadLetterAsync(Guid messageId)
    {
        var msg = await _context.OutboxMessages.FindAsync(messageId);
        if (msg == null || !msg.IsDeadLetter) return;

        msg.IsDeadLetter = false;
        msg.DeadLetteredAt = null;
        msg.RetryCount = 0;
        msg.Error = null;
        msg.NextRetryAt = null;
        msg.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation("[SC5] Message {Id} requeued from DLQ", messageId);
    }
}
