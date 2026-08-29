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

    // OutboxMessage.Error is "character varying(500)" in PostgreSQL. A longer value is
    // rejected by the server with 22001 "value too long for type character varying(500)",
    // which throws out of SaveChangesAsync. See the catch block below.
    private const int ErrorColumnLength = 500;

    // Lock wait must exceed a normal run, not the 30s schedule gap. This job is scheduled
    // every 30 seconds while a single run is allowed 30s for its query alone, so on a
    // slow database (Burstable tiers throttle hard once CPU credits are spent) the next
    // run would queue behind the current one, fail to take the lock inside 60s, and throw
    // DistributedLockTimeoutException - a "failure" that means nothing except "the previous
    // run is still going". 120s absorbs that without letting a genuinely stuck run pile up.
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
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
                    await _webhookService.DispatchEventAsync(message.TenantId, message.EventType, message.Payload);
                }

                message.IsProcessed = true;
                message.ProcessedAt = now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SC5] Failed outbox message {Id} (attempt {Attempt})", message.Id, message.RetryCount + 1);
                // MUST be truncated, and must not be ex.ToString(). Error is varying(500),
                // while a full stack trace runs to thousands of characters, so PostgreSQL
                // rejected the write with 22001 and the SaveChangesAsync at the end of this
                // method threw - failing the entire Hangfire job. The message then stayed
                // unprocessed, because the very write that would have recorded its retry
                // state is the one that failed, so the next run 30s later picked up the same
                // message and failed again. That is the loop that filled the Failed set and
                // held the "hangfire" health check at Degraded, blocking every deployment.
                // SQLite ignores varchar limits, which is why the test suite never caught it.
                message.Error = Truncate(ex.Message, ErrorColumnLength);

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

        // Deliberately guarded. Failing here accomplishes nothing - the messages stay
        // pending and the next run 30 seconds later retries them anyway - while an
        // un-persistable row would otherwise fail this job on every run forever, which is
        // precisely the outage this method just spent a comment block explaining. A real
        // database outage still surfaces: the "postgresql" health check reports Unhealthy
        // and this logs at Error for Application Insights / Sentry.
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[SC5] Could not persist outbox bookkeeping for {Count} message(s); they remain pending for the next run.",
                messages.Count);
        }
    }

    /// <summary>Clamps a value to a column's length so a write cannot fail with 22001.</summary>
    private static string Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];

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
