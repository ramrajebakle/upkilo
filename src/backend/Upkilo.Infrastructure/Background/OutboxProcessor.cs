// SUPERSEDED — do NOT register this as AddHostedService.
// Use Upkilo.Infrastructure.Jobs.OutboxProcessor (Hangfire) instead.
// This file is kept for reference only; it does not run in any environment.
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Background;

/// <summary>
/// Background service that processes outbox messages and dispatches them
/// to webhook subscribers and notification services.
/// Ensures at-least-once delivery with retry support.
/// </summary>
[Obsolete("Use Upkilo.Infrastructure.Jobs.OutboxProcessor scheduled via Hangfire. This class is NOT registered.")]
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int MaxRetries = 5;
    private const int BatchSize = 25;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor batch error");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();

        var messages = await context.OutboxMessages
            .Where(m => !m.IsProcessed && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        if (messages.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var normalizedEvent = NormalizeEventType(message.EventType);

                // Deserialize the payload
                object? payload = null;
                try
                {
                    payload = JsonSerializer.Deserialize<JsonElement>(message.Payload);
                }
                catch
                {
                    payload = new { raw = message.Payload };
                }

                // 1. Dispatch to Webhooks
                await webhookService.DispatchEventAsync(
                    message.TenantId,
                    normalizedEvent,
                    payload ?? new { }
                );

                // 2. Sync to Elasticsearch for Search (Specific Entities)
                // Resolve lazily — only when actually needed so ES unavailability doesn't block webhook dispatch
                if (ShouldSyncToElastic(normalizedEvent))
                {
                    var esService = scope.ServiceProvider.GetService<IElasticsearchService>();
                    if (esService != null)
                    {
                        await esService.IndexEntityAsync(message.TenantId.ToString(), payload ?? new { });
                        _logger.LogInformation("Indexed {EventType} in Elasticsearch for tenant {TenantId}", message.EventType, message.TenantId);
                    }
                }

                // Mark as successfully processed
                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;

                _logger.LogInformation(
                    "Outbox message {Id} processed: {EventType} for tenant {TenantId}",
                    message.Id, message.EventType, message.TenantId);
            }
            // ... (rest of catch block)
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogError(ex,
                    "Failed to process outbox message {Id} ({EventType}), retry {Count}/{Max}",
                    message.Id, message.EventType, message.RetryCount, MaxRetries);

                // Move to Dead Letter Queue when retries exhausted
                if (message.RetryCount >= MaxRetries)
                {
                    context.DeadLetterMessages.Add(new Upkilo.Core.Entities.DeadLetterMessage
                    {
                        Source = "OutboxProcessor",
                        EventType = message.EventType,
                        Payload = message.Payload,
                        Error = ex.Message,
                        StackTrace = ex.StackTrace,
                        OriginalRetryCount = message.RetryCount,
                        TenantId = message.TenantId,
                        CorrelationId = message.CorrelationId,
                        FailedAt = DateTime.UtcNow
                    });
                    message.IsProcessed = true; // Mark as processed to prevent re-fetch
                    message.Error = $"DEAD_LETTERED: {ex.Message}";
                    _logger.LogWarning("Outbox message {Id} moved to Dead Letter Queue after {Max} retries",
                        message.Id, MaxRetries);
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }

    private static bool ShouldSyncToElastic(string eventType)
    {
        // Define which entity events should be synced to Elasticsearch for search
        return eventType.StartsWith("booking.") || 
               eventType.StartsWith("client.") || 
               eventType.StartsWith("staff.") || 
               eventType.StartsWith("service.");
    }

    /// <summary>
    /// Normalizes PascalCase event types (e.g., "BookingCreated") to
    /// dot-separated format (e.g., "booking.created") for consistency
    /// with the WebhookEvents constants.
    /// </summary>
    private static string NormalizeEventType(string eventType)
    {
        // Already in dot format (e.g., "booking.created")
        if (eventType.Contains('.'))
            return eventType.ToLowerInvariant();

        // Convert PascalCase to dot.separated (e.g., "BookingCreated" -> "booking.created")
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < eventType.Length; i++)
        {
            if (i > 0 && char.IsUpper(eventType[i]))
            {
                result.Append('.');
            }
            result.Append(char.ToLowerInvariant(eventType[i]));
        }
        return result.ToString();
    }
}
