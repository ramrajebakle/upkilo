using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Interfaces.Workflow;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Workflow;
using WorkflowEntity = Upkilo.Core.Entities.Workflow;

namespace Upkilo.Infrastructure.Services;

public class WorkflowService : IWorkflowService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IWebhookService _webhookService;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<WorkflowService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IEnumerable<IWorkflowStepExecutor> _stepExecutors;

    // Tier-based execution limits for tenant isolation.
    // Keys MUST match SubscriptionTier member names lowercased — the lookup below uses
    // tenant.SubscriptionTier.ToString().ToLower(), and a miss falls back to Free's limits.
    // "professional" was a dead key after the tier rename, so Growth tenants silently ran
    // with Free's 5-concurrent ceiling. Every enum member needs an entry here.
    private static readonly Dictionary<string, TenantExecutionLimits> TierLimits = new()
    {
        ["free"] = new(MaxConcurrent: 5, MaxStepsPerExecution: 20, StepTimeoutSeconds: 15, MaxExecutionDurationMinutes: 5),
        ["starter"] = new(MaxConcurrent: 15, MaxStepsPerExecution: 50, StepTimeoutSeconds: 30, MaxExecutionDurationMinutes: 15),
        ["growth"] = new(MaxConcurrent: 50, MaxStepsPerExecution: 100, StepTimeoutSeconds: 60, MaxExecutionDurationMinutes: 30),
        ["business"] = new(MaxConcurrent: 50, MaxStepsPerExecution: 100, StepTimeoutSeconds: 60, MaxExecutionDurationMinutes: 30),  // legacy
        ["agency"] = new(MaxConcurrent: 50, MaxStepsPerExecution: 100, StepTimeoutSeconds: 60, MaxExecutionDurationMinutes: 30),    // legacy
        ["enterprise"] = new(MaxConcurrent: 200, MaxStepsPerExecution: 500, StepTimeoutSeconds: 120, MaxExecutionDurationMinutes: 60),
    };

    public WorkflowService(
        AppDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        IWhatsAppService whatsAppService,
        IWebhookService webhookService,
        IBackgroundJobClient backgroundJobs,
        ILogger<WorkflowService> logger,
        INotificationService notificationService,
        IEnumerable<IWorkflowStepExecutor> stepExecutors)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _whatsAppService = whatsAppService;
        _webhookService = webhookService;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
        _notificationService = notificationService;
        _stepExecutors = stepExecutors;
    }

    public async Task ExecuteWorkflowAsync(WorkflowEntity workflow, WorkflowEvent triggerEvent)
    {
        _logger.LogInformation("Starting workflow {WorkflowName} ({WorkflowId}) for tenant {TenantId}",
            workflow.Name, workflow.Id, workflow.TenantId);

        var execution = new WorkflowExecution
        {
            Id = Guid.NewGuid(),
            TenantId = workflow.TenantId,
            WorkflowId = workflow.Id,
            Status = "Running",
            CurrentStepIndex = 0,
            TriggerEventData = JsonSerializer.Serialize(triggerEvent.Data) ?? "{}",
            StartedAt = DateTime.UtcNow
        };

        _context.WorkflowExecutions.Add(execution);
        await _context.SaveChangesAsync();

        // Tier-based throttling check
        var limits = await GetTenantLimitsAsync(workflow.TenantId);
        if (!await CheckThrottlingAsync(workflow.TenantId, limits.MaxConcurrent))
        {
            _logger.LogWarning("Workflow execution throttled for tenant {TenantId} (max {Max} concurrent)",
                workflow.TenantId, limits.MaxConcurrent);
            execution.Status = "Throttled";
            await _context.SaveChangesAsync();
            return;
        }

        await ExecuteStepAsync(workflow.Id, 0, triggerEvent, execution.Id);
    }

    public async Task ExecuteStepAsync(Guid workflowId, int stepIndex, WorkflowEvent triggerEvent, Guid? executionId = null)
    {
        var workflow = await _context.Workflows.FindAsync(workflowId);
        if (workflow == null || !workflow.IsActive) return;

        var steps = JsonSerializer.Deserialize<List<WorkflowStep>>(workflow.Steps,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (steps == null || stepIndex >= steps.Count)
        {
            _logger.LogInformation("Workflow {WorkflowId} completed at step {StepIndex}", workflowId, stepIndex);
            if (executionId.HasValue)
            {
                var exec = await _context.WorkflowExecutions.FindAsync(executionId.Value);
                if (exec != null)
                {
                    exec.Status = "Completed";
                    exec.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            return;
        }

        // Max execution duration guard
        if (executionId.HasValue)
        {
            var execution = await _context.WorkflowExecutions.FindAsync(executionId.Value);
            if (execution != null)
            {
                var limits = await GetTenantLimitsAsync(workflow.TenantId);
                var elapsed = DateTime.UtcNow - execution.StartedAt;
                if (elapsed.TotalMinutes > limits.MaxExecutionDurationMinutes)
                {
                    _logger.LogWarning("Workflow {WorkflowId} exceeded max duration ({Max} min) for tenant {TenantId}",
                        workflowId, limits.MaxExecutionDurationMinutes, workflow.TenantId);
                    execution.Status = "TimedOut";
                    execution.ErrorMessage = $"Exceeded maximum execution duration of {limits.MaxExecutionDurationMinutes} minutes";
                    execution.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _backgroundJobs.Enqueue<IWorkflowService>(x => x.ExecuteCompensatoryStepsAsync(execution.Id));
                    return;
                }

                // Max steps guard
                if (stepIndex >= limits.MaxStepsPerExecution)
                {
                    _logger.LogWarning("Workflow {WorkflowId} exceeded max steps ({Max}) for tenant {TenantId}",
                        workflowId, limits.MaxStepsPerExecution, workflow.TenantId);
                    execution.Status = "StepLimitExceeded";
                    execution.ErrorMessage = $"Exceeded maximum step count of {limits.MaxStepsPerExecution}";
                    execution.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return;
                }

                execution.CurrentStepIndex = stepIndex;
                await _context.SaveChangesAsync();
            }
        }

        var step = steps[stepIndex];
        _logger.LogDebug("Executing step {StepIndex} of type {StepType} for workflow {WorkflowId}",
            stepIndex, step.Type, workflowId);

        var startTime = DateTime.UtcNow;
        string logStatus = "Success";
        string? errorMessage = null;

        try
        {
            // Per-step timeout enforcement
            var limits2 = await GetTenantLimitsAsync(workflow.TenantId);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(limits2.StepTimeoutSeconds));

            if (step.Type.Equals("Wait", StringComparison.OrdinalIgnoreCase))
            {
                var waitConfig = step.Config.Deserialize<Upkilo.Core.Models.Workflows.WaitActionConfig>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                int delayMinutes = waitConfig?.DurationMinutes ?? 0;

                if (delayMinutes > 0)
                {
                    _backgroundJobs.Schedule<IWorkflowService>(
                        x => x.ResumeWorkflowAsync(workflowId, stepIndex + 1, triggerEvent, executionId),
                        TimeSpan.FromMinutes(delayMinutes));
                    return;
                }
            }
            else if (step.Type.Equals("Action", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessActionStep(step, triggerEvent, workflow.TenantId).WaitAsync(cts.Token);
            }
            else if (step.Type.Equals("Condition", StringComparison.OrdinalIgnoreCase))
            {
                await HandleConditionStep(step, workflowId, triggerEvent, stepIndex, executionId);
                return;
            }
            else if (step.Type.Equals("Jump", StringComparison.OrdinalIgnoreCase))
            {
                var conf = step.Config.Deserialize<Dictionary<string, JsonElement>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                if (conf.TryGetValue("TargetIndex", out var tProp) && tProp.TryGetInt32(out var tIdx))
                {
                    await ExecuteStepAsync(workflowId, tIdx, triggerEvent, executionId);
                    return;
                }
            }

            await ExecuteStepAsync(workflowId, stepIndex + 1, triggerEvent, executionId);
        }
        catch (OperationCanceledException)
        {
            logStatus = "TimedOut";
            errorMessage = $"Step {stepIndex} exceeded timeout";
            _logger.LogWarning("Workflow {WorkflowId} step {StepIndex} timed out", workflowId, stepIndex);
            if (executionId.HasValue)
            {
                var execution = await _context.WorkflowExecutions.FindAsync(executionId.Value);
                if (execution != null)
                {
                    execution.Status = "Failed";
                    execution.ErrorMessage = errorMessage;
                    execution.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    await _notificationService.EscalateAsync(workflow.TenantId, "Workflow",
                        $"Step {stepIndex} timed out", "Medium",
                        new { WorkflowId = workflowId, ExecutionId = executionId }, false);

                    _backgroundJobs.Enqueue<IWorkflowService>(x => x.ExecuteCompensatoryStepsAsync(execution.Id));
                }
            }
        }
        catch (Exception ex)
        {
            logStatus = "Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "Workflow {WorkflowId} step {StepIndex} failed", workflowId, stepIndex);
            if (executionId.HasValue)
            {
                var execution = await _context.WorkflowExecutions.FindAsync(executionId.Value);
                if (execution != null)
                {
                    execution.Status = "Failed";
                    execution.ErrorMessage = ex.Message;
                    execution.CompletedAt = DateTime.UtcNow;
                    execution.RetryCount++;
                    await _context.SaveChangesAsync();

                    // Enqueue saga compensation
                    _backgroundJobs.Enqueue<IWorkflowService>(x => x.ExecuteCompensatoryStepsAsync(execution.Id));
                }
            }
        }
        finally
        {
            if (executionId.HasValue && workflow != null)
            {
                var durationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _context.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = workflow.TenantId,
                    WorkflowExecutionId = executionId.Value,
                    StepIndex = stepIndex,
                    StepType = step.Type,
                    ActionType = step.ActionType ?? "N/A",
                    Status = logStatus,
                    DurationMs = durationMs,
                    ErrorDetails = errorMessage,
                    ExecutedAt = DateTime.UtcNow
                });
                // Telemetry must not fail the workflow step it is recording, so the exception is
                // still swallowed — but it is logged now. Silently dropping these left gaps in the
                // execution log that were indistinguishable from steps that never ran.
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Could not persist execution log for workflow {WorkflowId} step {StepIndex}",
                        workflow.Id, stepIndex);
                }
            }
        }
    }

    public async Task ResumeWorkflowAsync(Guid workflowId, int stepIndex, WorkflowEvent triggerEvent, Guid? executionId = null)
    {
        if (executionId.HasValue)
        {
            var execution = await _context.WorkflowExecutions.FindAsync(executionId.Value);
            if (execution != null && execution.Status == "Paused")
            {
                execution.Status = "Running";
                await _context.SaveChangesAsync();
            }
        }
        await ExecuteStepAsync(workflowId, stepIndex, triggerEvent, executionId);
    }

    /// <summary>
    /// Saga compensation: walks backwards through completed steps and applies their compensation actions.
    /// </summary>
    public async Task ExecuteCompensatoryStepsAsync(Guid executionId)
    {
        var execution = await _context.WorkflowExecutions
            .Include(e => e.Workflow)
            .Include(e => e.Logs)
            .FirstOrDefaultAsync(e => e.Id == executionId);

        if (execution == null || execution.IsCompensated)
        {
            _logger.LogInformation("Compensation skipped for execution {ExecutionId} (already compensated or not found)", executionId);
            return;
        }

        if (execution.Workflow == null)
        {
            _logger.LogWarning("Cannot compensate execution {ExecutionId}: workflow not found", executionId);
            return;
        }

        _logger.LogInformation("Starting saga compensation for workflow execution {ExecutionId}", executionId);
        execution.Status = "Compensating";
        await _context.SaveChangesAsync();

        try
        {
            var steps = JsonSerializer.Deserialize<List<WorkflowStep>>(execution.Workflow.Steps,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (steps == null)
            {
                execution.Status = "CompensationFailed";
                execution.ErrorMessage += " | Compensation failed: could not parse workflow steps";
                await _context.SaveChangesAsync();
                return;
            }

            // Get all successfully completed step logs, ordered by step index descending (reverse order)
            var completedLogs = execution.Logs
                .Where(l => l.Status == "Success")
                .OrderByDescending(l => l.StepIndex)
                .ToList();

            foreach (var log in completedLogs)
            {
                if (log.StepIndex >= steps.Count) continue;

                var step = steps[log.StepIndex];
                var compensation = step.Compensation;

                // Skip if no compensation defined or explicitly skipped
                if (compensation == null || compensation.SkipCompensation)
                {
                    _logger.LogDebug("Skipping compensation for step {StepIndex} (no compensation defined or skipped)", log.StepIndex);
                    continue;
                }

                try
                {
                    await ExecuteCompensationAction(compensation, step, execution.Workflow.TenantId, log.StepIndex);

                    _context.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
                    {
                        Id = Guid.NewGuid(),
                        TenantId = execution.TenantId,
                        WorkflowExecutionId = executionId,
                        StepIndex = log.StepIndex,
                        StepType = "Compensation",
                        ActionType = compensation.CompensationType,
                        Status = "Success",
                        DurationMs = 0,
                        ExecutedAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Compensation failed for step {StepIndex} in execution {ExecutionId}", log.StepIndex, executionId);

                    _context.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
                    {
                        Id = Guid.NewGuid(),
                        TenantId = execution.TenantId,
                        WorkflowExecutionId = executionId,
                        StepIndex = log.StepIndex,
                        StepType = "Compensation",
                        ActionType = compensation.CompensationType,
                        Status = "Failed",
                        DurationMs = 0,
                        ErrorDetails = ex.Message,
                        ExecutedAt = DateTime.UtcNow
                    });
                }
            }

            execution.Status = "Compensated";
            execution.IsCompensated = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Saga compensation completed for execution {ExecutionId}", executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga compensation failed for execution {ExecutionId}", executionId);
            execution.Status = "CompensationFailed";
            execution.ErrorMessage += $" | Compensation error: {ex.Message}";
            await _context.SaveChangesAsync();

            await _notificationService.EscalateAsync(execution.TenantId, "Workflow",
                "Saga compensation failed - manual intervention required", "Critical",
                new { ExecutionId = executionId, Error = ex.Message }, true);
        }
    }

    private async Task ExecuteCompensationAction(WorkflowStepCompensation compensation, WorkflowStep originalStep, Guid tenantId, int stepIndex)
    {
        switch (compensation.CompensationType.ToLower())
        {
            case "undotag":
                // Reverse tag operation: if original added a tag, remove it; if removed, add it back
                if (originalStep.ActionType.Equals("addtag", StringComparison.OrdinalIgnoreCase))
                    await HandleRemoveTag(originalStep.Config, new WorkflowEvent { Data = new() }, tenantId);
                else if (originalStep.ActionType.Equals("removetag", StringComparison.OrdinalIgnoreCase))
                    await HandleAddTag(originalStep.Config, new WorkflowEvent { Data = new() }, tenantId);
                break;

            case "compensatingwebhook":
                if (compensation.CompensationConfig.HasValue)
                {
                    var url = compensation.CompensationConfig.Value.TryGetProperty("Url", out var urlProp)
                        ? urlProp.GetString() : null;
                    if (!string.IsNullOrEmpty(url))
                    {
                        await _webhookService.SendWebhookRequestAsync(url, "POST",
                            new { compensationFor = originalStep.ActionType, stepIndex },
                            new Dictionary<string, string>());
                    }
                }
                break;

            case "sendnotification":
                if (compensation.CompensationConfig.HasValue)
                {
                    var email = compensation.CompensationConfig.Value.TryGetProperty("NotifyEmail", out var emailProp)
                        ? emailProp.GetString() : null;
                    if (!string.IsNullOrEmpty(email))
                    {
                        await _emailService.SendSystemEmailAsync(email,
                            "Workflow Compensation Notice",
                            $"Step {stepIndex} ({originalStep.ActionType}) has been rolled back due to a workflow failure.");
                    }
                }
                break;

            case "logonly":
            default:
                _logger.LogInformation("Compensation logged for step {StepIndex} ({ActionType}) — no rollback action taken",
                    stepIndex, originalStep.ActionType);
                break;
        }
    }

    private async Task ProcessActionStep(WorkflowStep step, WorkflowEvent triggerEvent, Guid tenantId)
    {
        switch (step.ActionType.ToLower())
        {
            case "sendemail": await HandleSendEmail(step.Config, triggerEvent, tenantId); break;
            case "sendsms": await HandleSendSms(step.Config, triggerEvent, tenantId); break;
            case "whatsapp": await HandleSendWhatsApp(step.Config, triggerEvent, tenantId); break;
            case "addtag": await HandleAddTag(step.Config, triggerEvent, tenantId); break;
            case "removetag": await HandleRemoveTag(step.Config, triggerEvent, tenantId); break;
            case "webhook": await HandleWebhook(step.Config, triggerEvent, tenantId); break;
        }
    }

    private async Task HandleWebhook(JsonElement config, WorkflowEvent triggerEvent, Guid tenantId)
    {
        var url = config.TryGetProperty("Url", out var urlElement) ? urlElement.GetString() : null;
        if (string.IsNullOrEmpty(url)) return;
        await _webhookService.SendWebhookRequestAsync(url, "POST", new { trigger = triggerEvent.Data }, new());
    }

    private async Task HandleAddTag(JsonElement config, WorkflowEvent triggerEvent, Guid tenantId)
    {
        _logger.LogInformation("AddTag action executed for tenant {TenantId}", tenantId);
    }

    private async Task HandleRemoveTag(JsonElement config, WorkflowEvent triggerEvent, Guid tenantId)
    {
        _logger.LogInformation("RemoveTag action executed for tenant {TenantId}", tenantId);
    }

    private async Task HandleSendEmail(JsonElement config, WorkflowEvent triggerEvent, Guid tenantId)
    {
        var to = config.TryGetProperty("To", out var t) ? t.GetString() : null;
        if (!string.IsNullOrEmpty(to)) await _emailService.SendSystemEmailAsync(to, "Workflow Alert", "Body");
    }

    private async Task HandleSendSms(JsonElement config, WorkflowEvent triggerEvent, Guid tenantId)
    {
        var to = config.TryGetProperty("To", out var t) ? t.GetString() : null;
        if (!string.IsNullOrEmpty(to)) await _smsService.SendSmsAsync(tenantId, to, "Workflow Msg");
    }

    private async Task HandleSendWhatsApp(JsonElement config, WorkflowEvent triggerEvent, Guid tenantId)
    {
        var to = config.TryGetProperty("To", out var t) ? t.GetString() : null;
        if (!string.IsNullOrEmpty(to)) await _whatsAppService.SendWhatsAppAsync(tenantId, to, "Workflow WA", null);
    }

    private async Task HandleConditionStep(WorkflowStep step, Guid workflowId, WorkflowEvent triggerEvent,
        int stepIndex, Guid? executionId = null)
    {
        _logger.LogInformation("Evaluating condition step {StepIndex} for workflow {WorkflowId}", stepIndex, workflowId);

        try
        {
            var config = step.Config.ValueKind != JsonValueKind.Undefined
                ? JsonSerializer.Deserialize<ConditionStepConfig>(step.Config.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                : null;

            if (config == null || string.IsNullOrWhiteSpace(config.Expression))
            {
                _logger.LogWarning("Condition step {StepIndex} has empty or invalid config. Defaulting to true path.", stepIndex);
                await ExecuteStepAsync(workflowId, stepIndex + 1, triggerEvent, executionId);
                return;
            }

            var context = new WorkflowContext
            {
                TenantId = triggerEvent.TenantId,
                WorkflowInstanceId = executionId ?? Guid.Empty,
                State = FlattenEventData(triggerEvent.Data)
            };

            var engine = new WorkflowConditionEngine();
            bool result = engine.EvaluateCondition(config.Expression, context);

            _logger.LogInformation("Condition step {StepIndex} expression '{Expression}' evaluated to: {Result}",
                stepIndex, config.Expression, result);

            int nextIndex = result
                ? (config.TrueStepIndex ?? (stepIndex + 1))
                : (config.FalseStepIndex ?? -1);

            if (nextIndex >= 0)
            {
                await ExecuteStepAsync(workflowId, nextIndex, triggerEvent, executionId);
            }
            else
            {
                _logger.LogInformation("Condition evaluated to False and FalseStepIndex was not set or set to terminate. Ending workflow execution.");
                if (executionId.HasValue)
                {
                    var exec = await _context.WorkflowExecutions.FindAsync(executionId.Value);
                    if (exec != null)
                    {
                        exec.Status = "Completed";
                        exec.CompletedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate condition step {StepIndex}", stepIndex);
            await ExecuteStepAsync(workflowId, stepIndex + 1, triggerEvent, executionId);
        }
    }

    private Dictionary<string, object> FlattenEventData(object? data)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (data == null) return result;

        try
        {
            JsonElement element;
            if (data is JsonElement je)
            {
                element = je;
            }
            else
            {
                var json = JsonSerializer.Serialize(data);
                using var doc = JsonDocument.Parse(json);
                element = doc.RootElement.Clone();
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    var val = ConvertJsonElement(prop.Value);
                    if (val != null)
                    {
                        result[prop.Name] = val;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to flatten workflow event data for condition evaluation.");
        }

        return result;
    }

    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : (element.TryGetDouble(out var d) ? d : (object?)null),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    public class ConditionStepConfig
    {
        public string Expression { get; set; } = string.Empty;
        public int? TrueStepIndex { get; set; }
        public int? FalseStepIndex { get; set; }
    }

    private async Task<bool> CheckThrottlingAsync(Guid tenantId, int maxConcurrent)
    {
        var count = await _context.WorkflowExecutions
            .CountAsync(e => e.TenantId == tenantId && e.Status == "Running");
        return count < maxConcurrent;
    }

    private async Task<TenantExecutionLimits> GetTenantLimitsAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        var tierName = tenant?.SubscriptionTier.ToString().ToLower() ?? "free";

        if (TierLimits.TryGetValue(tierName, out var limits))
            return limits;

        return TierLimits["free"];
    }

    // CRUD operations
    public async Task<IEnumerable<WorkflowEntity>> GetWorkflowsAsync(Guid tenantId)
        => await _context.Workflows.Where(w => w.TenantId == tenantId).ToListAsync();

    public async Task<WorkflowEntity?> GetWorkflowAsync(Guid id, Guid tenantId)
        => await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);

    public async Task<WorkflowEntity> CreateWorkflowAsync(WorkflowEntity workflow)
    {
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();
        return workflow;
    }

    public async Task<WorkflowEntity?> UpdateWorkflowAsync(WorkflowEntity workflow) { return workflow; }
    public async Task<bool> DeleteWorkflowAsync(Guid id, Guid tenantId) { return true; }
}

public record TenantExecutionLimits(
    int MaxConcurrent,
    int MaxStepsPerExecution,
    int StepTimeoutSeconds,
    int MaxExecutionDurationMinutes
);
