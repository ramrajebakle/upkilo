using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Events;
using Upkilo.Core.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Upkilo.Infrastructure.Events;

public class DealStageChangedEventHandler : INotificationHandler<DealStageChangedEvent>
{
    private readonly AppDbContext _context;
    private readonly ILogger<DealStageChangedEventHandler> _logger;

    public DealStageChangedEventHandler(AppDbContext context, ILogger<DealStageChangedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(DealStageChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing DealStageChangedEvent for Deal {DealId}", notification.DealId);

        // 1. Log the Activity on the Deal
        var activity = new DealActivity
        {
            TenantId = notification.TenantId,
            DealId = notification.DealId,
            ActivityType = "StageChange",
            Description = $"Deal '{notification.DealTitle}' moved to a new stage.",
            CreatedAt = DateTime.UtcNow
        };

        _context.DealActivities.Add(activity);

        // 2. Potentially trigger a Workflow (e.g. IF New Stage = 'Won' THEN Send Onboarding Email)
        var newStage = await _context.PipelineStages.FindAsync(notification.NewStageId);
        if (newStage != null && newStage.ProbabilityPercentage == 100)
        {
            // Trigger "Deal Won" automation workflow...
            _logger.LogInformation("Deal {DealId} marked as won! Triggering automations.", notification.DealId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
