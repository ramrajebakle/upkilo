using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Models.Workflows;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;

namespace Upkilo.Infrastructure.Background
{
    public class WorkflowWorker : BackgroundService
    {
        private readonly EventService _eventService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WorkflowWorker> _logger;

        public WorkflowWorker(
            EventService eventService, 
            IServiceScopeFactory scopeFactory, 
            ILogger<WorkflowWorker> logger)
        {
            _eventService = eventService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WorkflowWorker started. Listening for events...");

            await foreach (var evt in _eventService.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessEventAsync(evt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {EventName} for tenant {TenantId}", 
                        evt.EventName, evt.TenantId);
                }
            }
        }

        private async Task ProcessEventAsync(WorkflowEvent evt)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();
            var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();

            // 1. Dispatch to Webhooks
            await webhookService.DispatchEventAsync(evt.TenantId, evt.EventName, evt.Data);

            // 2. Find workflows triggered by this event
            var workflows = await context.Workflows
                .Where(w => w.TenantId == evt.TenantId && 
                            w.IsActive && 
                            w.TriggerType == evt.EventName)
                .ToListAsync();

            if (!workflows.Any())
            {
                _logger.LogDebug("No matching workflows for event {EventName} and tenant {TenantId}", 
                    evt.EventName, evt.TenantId);
                return;
            }

            // Optional Event Data parser
            JsonElement? evtDataElement = null;
            if (evt.Data != null)
            {
                evtDataElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(evt.Data));
            }

            foreach (var workflow in workflows)
            {
                try
                {
                    // 3. Evaluate TriggerConfig Conditions
                    bool shouldTrigger = EvaluateTriggerConfig(workflow.TriggerConfig, evtDataElement);
                    if (!shouldTrigger) 
                    {
                        _logger.LogInformation("Workflow {WorkflowId} trigger conditions not met for event {EventName}", workflow.Id, evt.EventName);
                        continue;
                    }

                    _logger.LogInformation("Executing Workflow: {WorkflowName} ({WorkflowId}) for event {EventName}", 
                        workflow.Name, workflow.Id, evt.EventName);
                    
                    await workflowService.ExecuteWorkflowAsync(workflow, evt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute workflow {WorkflowId}", workflow.Id);
                }
            }
        }

        private bool EvaluateTriggerConfig(string triggerConfigJson, JsonElement? evtDataElement)
        {
            if (string.IsNullOrWhiteSpace(triggerConfigJson) || triggerConfigJson == "{}")
                return true;

            try 
            {
                var config = JsonSerializer.Deserialize<TriggerConfig>(triggerConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config == null || config.Filters == null || !config.Filters.Any())
                    return true;

                if (!evtDataElement.HasValue)
                {
                    // Filters exist but no data to evaluate against
                    return false;
                }

                foreach (var filter in config.Filters)
                {
                    // Basic evaluation
                    if (!EvaluateFilter(filter, evtDataElement.Value))
                        return false; // All filters must pass (AND)
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing TriggerConfig JSON");
                return true; // Optional: default to true on parse fail, or false? Let's default to true for now to avoid breaking existing workflows
            }
        }

        private bool EvaluateFilter(TriggerFilter filter, JsonElement evtData)
        {
             if (string.IsNullOrEmpty(filter.Field) || !evtData.TryGetProperty(filter.Field, out var element))
                return false;

             var elementValue = element.ToString()?.ToLower() ?? "";
             var compareValue = filter.Value?.ToString()?.ToLower() ?? "";

             return filter.Operator.ToLower() switch
             {
                 "equals" => elementValue == compareValue,
                 "notequals" => elementValue != compareValue,
                 "contains" => elementValue.Contains(compareValue),
                 _ => false // Unsupported operator
             };
        }
    }
}

