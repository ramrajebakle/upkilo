using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Interfaces.Workflow;
using Upkilo.Core.Enums;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service to execute individual steps within an automated workflow.
/// </summary>
public class WorkflowStepExecutor : Upkilo.Core.Interfaces.Workflow.IWorkflowStepExecutor
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IWebhookService _webhookService;
    private readonly SlackNotificationService _slackService;
    private readonly DiscordNotificationService _discordService;
    private readonly AppDbContext _context;
    private readonly ILogger<WorkflowStepExecutor> _logger;

    public WorkflowStepExecutor(
        IEmailService emailService,
        ISmsService smsService,
        IWebhookService webhookService,
        SlackNotificationService slackService,
        DiscordNotificationService discordService,
        AppDbContext context,
        ILogger<WorkflowStepExecutor> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _webhookService = webhookService;
        _slackService = slackService;
        _discordService = discordService;
        _context = context;
        _logger = logger;
    }

    public bool HasCustomRetryPolicy() => false;

    public async Task<WorkflowStepResult> ExecuteAsync(IWorkflowStepConfig config, WorkflowContext context)
    {
        _logger.LogInformation("Executing workflow step: {StepName} ({StepType}) for tenant {TenantId}",
            config.StepName, config.StepType, context.TenantId);

        try
        {
            var json = JsonSerializer.Serialize(config, config.GetType(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            using var doc = JsonDocument.Parse(json);
            var configElement = doc.RootElement.Clone();

            switch (config.StepType.ToLower())
            {
                case "email":
                    await HandleEmailStepAsync(configElement, context.State);
                    break;
                case "sms":
                    await HandleSmsStepAsync(context.TenantId, configElement, context.State);
                    break;
                case "webhook":
                    await HandleWebhookStepAsync(context.TenantId, configElement, context.State);
                    break;
                case "slack":
                    await HandleSlackStepAsync(configElement, context.State);
                    break;
                case "discord":
                    await HandleDiscordStepAsync(configElement, context.State);
                    break;
                case "create_task":
                    await HandleCreateTaskStepAsync(context.TenantId, configElement, context.State);
                    break;
                default:
                    return new WorkflowStepResult { Success = false, ErrorMessage = $"Unknown step type: {config.StepType}" };
            }

            return new WorkflowStepResult { Success = true };
        }
        catch (Exception ex)
        {
            return new WorkflowStepResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task CompensateAsync(IWorkflowStepConfig config, WorkflowContext context)
    {
        _logger.LogInformation("Compensating step: {StepName} ({StepType}) for tenant {TenantId}",
            config.StepName, config.StepType, context.TenantId);

        try
        {
            switch (config.StepType.ToLower())
            {
                case "email":
                    // Emails cannot be recalled — log the irreversibility for audit trail
                    _logger.LogWarning(
                        "Email step '{StepName}' cannot be compensated — email already delivered.",
                        config.StepName);
                    break;

                case "sms":
                    _logger.LogWarning(
                        "SMS step '{StepName}' cannot be compensated — message already delivered.",
                        config.StepName);
                    break;

                case "create_task":
                    // Remove the CRM task created by this step using the correlation from context state
                    var stateJson = JsonSerializer.Serialize(context.State);
                    using (var doc = JsonDocument.Parse(stateJson))
                    {
                        if (doc.RootElement.TryGetProperty("compensate_task_id", out var taskIdEl) &&
                            Guid.TryParse(taskIdEl.GetString(), out var taskId))
                        {
                            var task = await _context.Set<CrmTask>().FindAsync(taskId);
                            if (task != null && task.TenantId == context.TenantId)
                            {
                                _context.Set<CrmTask>().Remove(task);
                                await _context.SaveChangesAsync();
                                _logger.LogInformation("Compensated: removed CRM task {TaskId}", taskId);
                            }
                        }
                    }
                    break;

                case "webhook":
                    // Dispatch a compensation event to the webhook endpoint
                    var json = JsonSerializer.Serialize(config, config.GetType(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var configEl = doc.RootElement.Clone();
                        var eventType = configEl.TryGetProperty("eventType", out var et)
                            ? $"{et.GetString()}.compensated"
                            : "custom.workflow.compensated";
                        await _webhookService.DispatchEventAsync(context.TenantId, eventType, context.State);
                        _logger.LogInformation("Compensation webhook dispatched: {EventType}", eventType);
                    }
                    break;

                default:
                    _logger.LogInformation(
                        "No compensation logic for step type '{StepType}' — skipping.", config.StepType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compensation failed for step '{StepName}'", config.StepName);
        }
    }

    [Obsolete("Use ExecuteAsync instead")]
    public Task ExecuteStepAsync(Guid tenantId, JsonElement step, object contextData)
    {
        return Task.CompletedTask;
    }

    private async Task HandleEmailStepAsync(JsonElement config, object contextData)
    {
        var to = ReplaceTemplateVars(config.GetProperty("to").GetString() ?? "", contextData);
        var subject = ReplaceTemplateVars(config.GetProperty("subject").GetString() ?? "", contextData);
        var body = ReplaceTemplateVars(config.GetProperty("body").GetString() ?? "", contextData);

        await _emailService.SendEmailAsync(to, subject, body);
    }

    private async Task HandleSmsStepAsync(Guid tenantId, JsonElement config, object contextData)
    {
        var to = ReplaceTemplateVars(config.GetProperty("phoneNumber").GetString() ?? "", contextData);
        var message = ReplaceTemplateVars(config.GetProperty("message").GetString() ?? "", contextData);

        await _smsService.SendSmsAsync(tenantId, to, message);
    }

    private async Task HandleWebhookStepAsync(Guid tenantId, JsonElement config, object contextData)
    {
        var eventType = config.GetProperty("eventType").GetString() ?? "custom.workflow.event";
        await _webhookService.DispatchEventAsync(tenantId, eventType, contextData);
    }

    private async Task HandleSlackStepAsync(JsonElement config, object contextData)
    {
        var webhookUrl = config.GetProperty("webhookUrl").GetString();
        var message = ReplaceTemplateVars(config.GetProperty("message").GetString() ?? "", contextData);
        if (!string.IsNullOrEmpty(webhookUrl))
            await _slackService.SendNotificationAsync(webhookUrl, message);
    }

    private async Task HandleDiscordStepAsync(JsonElement config, object contextData)
    {
        var webhookUrl = config.GetProperty("webhookUrl").GetString();
        var message = ReplaceTemplateVars(config.GetProperty("message").GetString() ?? "", contextData);
        if (!string.IsNullOrEmpty(webhookUrl))
            await _discordService.SendNotificationAsync(webhookUrl, message);
    }

    private async Task HandleCreateTaskStepAsync(Guid tenantId, JsonElement config, object contextData)
    {
        var task = new CrmTask
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = ReplaceTemplateVars(config.GetProperty("title").GetString() ?? "New Task", contextData),
            Description = ReplaceTemplateVars(config.GetProperty("description").GetString() ?? "", contextData),
            Priority = config.TryGetProperty("priority", out var p) ? p.GetString() ?? "Medium" : "Medium",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        if (config.TryGetProperty("assignedTo", out var a) && Guid.TryParse(a.GetString(), out var staffId))
            task.AssignedTo = staffId;

        _context.Set<CrmTask>().Add(task);
        await _context.SaveChangesAsync();
    }

    private async Task HandleSplitTestStepAsync(Guid tenantId, JsonElement config, object contextData)
    {
        var random = new Random();
        var chooseA = random.NextDouble() < 0.5;
        var variantProp = chooseA ? "variantA" : "variantB";

        if (config.TryGetProperty(variantProp, out var variantSteps) && variantSteps.ValueKind == JsonValueKind.Array)
        {
            foreach (var innerStep in variantSteps.EnumerateArray())
            {
                // Note: We'd need to adapt this if we use the new interface
            }
        }
    }

    private string ReplaceTemplateVars(string template, object contextData)
    {
        if (string.IsNullOrEmpty(template)) return template;

        try
        {
            Dictionary<string, string> vars;

            if (contextData is JsonElement jsonEl)
            {
                vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (jsonEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in jsonEl.EnumerateObject())
                        vars[prop.Name] = prop.Value.ToString();
                }
            }
            else
            {
                var json = JsonSerializer.Serialize(contextData);
                var doc = JsonDocument.Parse(json);
                vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in doc.RootElement.EnumerateObject())
                    vars[prop.Name] = prop.Value.ToString();
            }

            return Regex.Replace(template, @"\{(\w+)\}", match =>
            {
                var key = match.Groups[1].Value;
                return vars.TryGetValue(key, out var val) ? val : match.Value;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to replace template variables in: {Template}", template);
            return template;
        }
    }
}
