using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Text.Json;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service to dispatch triggers and start workflows based on domain events.
/// </summary>
public class TriggerDispatcher : ITriggerDispatcher
{
    private readonly AppDbContext _context;
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<TriggerDispatcher> _logger;

    public TriggerDispatcher(
        AppDbContext context,
        IWorkflowService workflowService,
        ILogger<TriggerDispatcher> logger)
    {
        _context = context;
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task DispatchAsync(string eventName, object data, Guid tenantId)
    {
        _logger.LogInformation("Dispatching triggers for event {EventName} in tenant {TenantId}", eventName, tenantId);

        // Find active workflows for this tenant and event type
        var workflows = await _context.Workflows
            .Where(w => w.TenantId == tenantId && w.IsActive && w.TriggerType == eventName)
            .ToListAsync();

        if (!workflows.Any())
        {
            _logger.LogDebug("No active workflows found for event {EventName}", eventName);
            return;
        }

        foreach (var workflow in workflows)
        {
            try
            {
                var triggerEvent = new WorkflowEvent
                {
                    EventName = eventName,
                    Data = data,
                    TenantId = tenantId,
                    OccurredAt = DateTime.UtcNow
                };

                // Use the production workflow service for durable execution
                await _workflowService.ExecuteWorkflowAsync(workflow, triggerEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate workflow {WorkflowName} for event {EventName}", workflow.Name, eventName);
            }
        }
    }
}
