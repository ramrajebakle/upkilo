// SUPERSEDED — do NOT register this as AddHostedService.
// Use Upkilo.Infrastructure.Jobs.OutboxProcessor (Hangfire) instead.
// This file is kept for reference only; it does not run in any environment.
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Services;

[Obsolete("Use Upkilo.Infrastructure.Jobs.OutboxProcessor scheduled via Hangfire. This class is NOT registered.")]
public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher> _logger;
    private const int IntervalMs = 10000; // Check every 10 seconds

    public OutboxDispatcher(IServiceProvider serviceProvider, ILogger<OutboxDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var messages = await context.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        // Simulate event dispatching (e.g., to RabbitMQ or MediatR)
                        _logger.LogInformation("OutboxDispatcher: Processing message {Id} of type {Type}.", message.Id, message.EventType);
                        
                        message.ProcessedAt = DateTime.UtcNow;
                        await context.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        message.Error = ex.Message;
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogError(ex, "OutboxDispatcher: Failed to process message {Id}.", message.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxDispatcher: Background worker error.");
            }

            await Task.Delay(IntervalMs, stoppingToken);
        }
    }
}
