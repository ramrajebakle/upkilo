using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public interface IAuditLogService
{
    void Log(AuditEntry entry);
}

public class BufferedAuditLogService : BackgroundService, IAuditLogService
{
    private readonly ConcurrentQueue<AuditEntry> _buffer = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BufferedAuditLogService> _logger;
    private const int FlushIntervalMs = 5000;
    private const int BatchSize = 100;
    // Cap the in-memory queue to prevent unbounded growth during DB outages.
    private const int MaxBufferSize = 10_000;

    public BufferedAuditLogService(IServiceProvider serviceProvider, ILogger<BufferedAuditLogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Log(AuditEntry entry)
    {
        if (_buffer.Count >= MaxBufferSize)
        {
            _logger.LogWarning("BufferedAuditLogService: Buffer at capacity ({Max}). Dropping audit entry to prevent OOM.", MaxBufferSize);
            return;
        }
        _buffer.Enqueue(entry);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(FlushIntervalMs, stoppingToken);

            if (_buffer.IsEmpty) continue;

            // Dequeue into a local list BEFORE attempting the save.
            // On save failure we re-enqueue so entries are not lost.
            var batch = new List<AuditEntry>();
            while (_buffer.TryDequeue(out var entry) && batch.Count < BatchSize)
            {
                batch.Add(entry);
            }

            if (batch.Count == 0) continue;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.AuditEntries.AddRangeAsync(batch, stoppingToken);
                using var saveCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await context.SaveChangesAsync(saveCts.Token);
                _logger.LogInformation("BufferedAuditLogService: Flushed {Count} audit entries.", batch.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BufferedAuditLogService: Failed to flush audit entries. Re-enqueueing {Count} entries.", batch.Count);
                // Re-enqueue only if buffer still has room; otherwise drop to prevent OOM.
                foreach (var entry in batch)
                {
                    if (_buffer.Count < MaxBufferSize)
                        _buffer.Enqueue(entry);
                    else
                        break;
                }
            }
        }
    }
}
