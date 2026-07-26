using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Attributes;

namespace Upkilo.API.Controllers;

/// <summary>
/// Workflow automation controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[FeatureGuard("ai_workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly ITenantProvider _tenantProvider;
    private readonly AppDbContext _context;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowService workflowService,
        ITenantProvider tenantProvider,
        AppDbContext context,
        ILogger<WorkflowsController> logger)
    {
        _workflowService = workflowService;
        _tenantProvider = tenantProvider;
        _context = context;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Get all workflows
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWorkflows()
    {
        var workflows = await _workflowService.GetWorkflowsAsync(GetTenantId());
        return Ok(new { data = workflows });
    }

    /// <summary>
    /// Get workflow by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWorkflow(Guid id)
    {
        var workflow = await _workflowService.GetWorkflowAsync(id, GetTenantId());
        if (workflow == null) return NotFound();

        // Deserialize Steps/Config for API response if needed, or send raw JSON
        return Ok(workflow);
    }

    /// <summary>
    /// Create new workflow
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowRequest request)
    {
        var workflow = new Workflow
        {
            TenantId = GetTenantId(),
            Name = request.Name,
            Description = request.Description ?? "",
            TriggerType = request.TriggerType,
            IsActive = true, // Default to true or draft
            Steps = JsonSerializer.Serialize(request.Steps ?? new List<WorkflowStepRequest>()),
            TriggerConfig = "{}" // Default empty config
        };

        var created = await _workflowService.CreateWorkflowAsync(workflow);

        _logger.LogInformation("Workflow created: {Id}", created.Id);

        return CreatedAtAction(nameof(GetWorkflow), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update workflow
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkflow(Guid id, [FromBody] UpdateWorkflowRequest request)
    {
        var tenantId = GetTenantId();
        var existing = await _workflowService.GetWorkflowAsync(id, tenantId);
        if (existing == null) return NotFound();

        if (request.Name != null) existing.Name = request.Name;
        if (request.Description != null) existing.Description = request.Description;
        if (request.TriggerType != null) existing.TriggerType = request.TriggerType;
        if (request.Steps != null) existing.Steps = JsonSerializer.Serialize(request.Steps);
        // Add TriggerConfig support if needed

        var updated = await _workflowService.UpdateWorkflowAsync(existing);
        return Ok(updated);
    }

    /// <summary>
    /// Delete workflow
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkflow(Guid id)
    {
        var success = await _workflowService.DeleteWorkflowAsync(id, GetTenantId());
        if (!success) return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Get execution history for a workflow
    /// </summary>
    [HttpGet("{id}/executions")]
    public async Task<IActionResult> GetWorkflowExecutions(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var tenantId = GetTenantId();

        var query = _context.WorkflowExecutions
            .Where(e => e.WorkflowId == id && e.TenantId == tenantId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);

        var total = await query.CountAsync();
        var executions = await query
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.WorkflowId,
                e.Status,
                e.CurrentStepIndex,
                e.StartedAt,
                e.CompletedAt,
                e.ErrorMessage,
                DurationMs = e.CompletedAt.HasValue
                    ? (int)(e.CompletedAt.Value - e.StartedAt).TotalMilliseconds
                    : (int?)null
            })
            .ToListAsync();

        return Ok(new { data = executions, total, page, pageSize });
    }

    /// <summary>
    /// Get step-level logs for a specific execution
    /// </summary>
    [HttpGet("{id}/executions/{executionId}/logs")]
    public async Task<IActionResult> GetExecutionLogs(Guid id, Guid executionId)
    {
        var tenantId = GetTenantId();

        var logs = await _context.WorkflowExecutionLogs
            .Where(l => l.WorkflowExecutionId == executionId && l.TenantId == tenantId)
            .OrderBy(l => l.StepIndex)
            .Select(l => new
            {
                l.StepIndex,
                l.StepType,
                l.ActionType,
                l.Status,
                l.DurationMs,
                l.ExecutedAt,
                l.Message,
                l.ErrorDetails
            })
            .ToListAsync();

        return Ok(new { data = logs });
    }

    /// <summary>
    /// Get available trigger types for the workflow builder UI
    /// </summary>
    [HttpGet("builder/triggers")]
    public IActionResult GetAvailableTriggers()
    {
        var triggers = new[]
        {
            new { id = "client.created", name = "Client Created", description = "Fires when a new client is added", category = "CRM" },
            new { id = "client.updated", name = "Client Updated", description = "Fires when client info is updated", category = "CRM" },
            new { id = "booking.confirmed", name = "Booking Confirmed", description = "Fires when a booking is confirmed", category = "Booking" },
            new { id = "booking.cancelled", name = "Booking Cancelled", description = "Fires when a booking is cancelled", category = "Booking" },
            new { id = "booking.completed", name = "Booking Completed", description = "Fires when a service is completed", category = "Booking" },
            new { id = "deal.created", name = "Deal Created", description = "Fires when a new deal is added to the pipeline", category = "Sales" },
            new { id = "deal.stage_changed", name = "Deal Stage Changed", description = "Fires when a deal moves stages", category = "Sales" },
            new { id = "deal.won", name = "Deal Won", description = "Fires when a deal is marked as won", category = "Sales" },
            new { id = "deal.lost", name = "Deal Lost", description = "Fires when a deal is marked as lost", category = "Sales" },
            new { id = "payment.received", name = "Payment Received", description = "Fires when a payment is processed", category = "Billing" },
            new { id = "invoice.created", name = "Invoice Created", description = "Fires when an invoice is generated", category = "Billing" },
            new { id = "form.submitted", name = "Form Submitted", description = "Fires when a form submission is received", category = "Marketing" },
            new { id = "campaign.completed", name = "Campaign Completed", description = "Fires when a campaign finishes sending", category = "Marketing" }
        };

        return Ok(new { data = triggers });
    }

    /// <summary>
    /// Get available action types for the workflow builder UI
    /// </summary>
    [HttpGet("builder/actions")]
    public IActionResult GetAvailableActions()
    {
        var actions = new List<object>
        {
            new { id = "sendemail", name = "Send Email", description = "Send an email to the target contact", category = "Communication", icon = "mail",
                  configFields = new[] { "To", "Subject", "Body" } },
            new { id = "sendsms", name = "Send SMS", description = "Send an SMS text message", category = "Communication", icon = "message-square",
                  configFields = new[] { "To", "Message" } },
            new { id = "whatsapp", name = "Send WhatsApp", description = "Send a WhatsApp message", category = "Communication", icon = "phone",
                  configFields = new[] { "To", "Message", "TemplateName" } },
            new { id = "voicecall", name = "Voice Call", description = "Initiate an automated voice call", category = "Communication", icon = "phone-call",
                  configFields = new[] { "To", "Script" } },
            new { id = "addtag", name = "Add Tag", description = "Add a tag to the client", category = "CRM", icon = "tag",
                  configFields = new[] { "Tag" } },
            new { id = "removetag", name = "Remove Tag", description = "Remove a tag from the client", category = "CRM", icon = "x-circle",
                  configFields = new[] { "Tag" } },
            new { id = "updateleadscore", name = "Update Lead Score", description = "Adjust the client's lead score", category = "CRM", icon = "trending-up",
                  configFields = new[] { "ScoreChange" } },
            new { id = "movedeal", name = "Move Deal", description = "Move a deal to a different pipeline stage", category = "Sales", icon = "git-branch",
                  configFields = new[] { "StageId" } },
            new { id = "webhook", name = "HTTP Request", description = "Send data to an external URL", category = "Integration", icon = "globe",
                  configFields = new[] { "Url", "Method", "Headers", "Payload" } }
        };

        var stepTypes = new[]
        {
            new { id = "Action", name = "Action", description = "Execute an action" },
            new { id = "Wait", name = "Wait/Delay", description = "Pause for a specified duration" },
            new { id = "Condition", name = "If/Then", description = "Branch based on a condition" },
            new { id = "ABTest", name = "A/B Test", description = "Randomly split traffic between two paths" },
            new { id = "Jump", name = "Go To Step", description = "Jump to a specific step index" }
        };

        return Ok(new { actions, stepTypes });
    }

    /// <summary>
    /// Update workflow status (active/paused/archived)
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateWorkflowStatus(Guid id, [FromBody] WorkflowUpdateStatusRequest request)
    {
        var tenantId = GetTenantId();
        var existing = await _workflowService.GetWorkflowAsync(id, tenantId);
        if (existing == null) return NotFound();

        if (request.Status != null)
            existing.IsActive = request.Status == "active";
        var updated = await _workflowService.UpdateWorkflowAsync(existing);
        return Ok(updated);
    }

    /// <summary>
    /// Duplicate a workflow
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> DuplicateWorkflow(Guid id)
    {
        var tenantId = GetTenantId();
        var source = await _workflowService.GetWorkflowAsync(id, tenantId);
        if (source == null) return NotFound();

        var duplicate = new Workflow
        {
            TenantId = tenantId,
            Name = $"{source.Name} (Copy)",
            Description = source.Description,
            TriggerType = source.TriggerType,
            Steps = source.Steps,
            TriggerConfig = source.TriggerConfig ?? "{}",
            IsActive = false,
        };

        var created = await _workflowService.CreateWorkflowAsync(duplicate);
        return Ok(new { data = created });
    }

    /// <summary>
    /// Test execute a workflow (dry run)
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestWorkflow(Guid id, [FromBody] TestWorkflowRequest? request = null)
    {
        var tenantId = GetTenantId();
        var workflow = await _workflowService.GetWorkflowAsync(id, tenantId);
        if (workflow == null) return NotFound();

        try
        {
            var testEvent = new WorkflowEvent
            {
                EventName = workflow.TriggerType,
                TenantId = tenantId,
                Data = request?.Payload != null
                    ? (object)request.Payload
                    : new { test = true, triggeredBy = "manual_test" }
            };
            await _workflowService.ExecuteWorkflowAsync(workflow, testEvent);
            return Ok(new { success = true, message = "Test execution triggered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test execution failed for workflow {WorkflowId}", id);
            return Ok(new { success = false, error = ex.Message, message = "Test execution failed" });
        }
    }

    /// <summary>
    /// Get aggregate workflow analytics for the tenant
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetWorkflowAnalytics([FromQuery] string period = "30d")
    {
        var tenantId = GetTenantId();
        var days = period switch { "7d" => 7, "90d" => 90, _ => 30 };
        var since = DateTime.UtcNow.AddDays(-days);

        var workflows = await _context.Workflows
            .Where(w => w.TenantId == tenantId)
            .Select(w => new { w.Id, w.Name, w.TriggerType, IsActive = w.IsActive })
            .ToListAsync();

        var executions = await _context.WorkflowExecutions
            .Where(e => e.TenantId == tenantId && e.StartedAt >= since)
            .ToListAsync();

        var totalExecutions = executions.Count;
        var successfulExecutions = executions.Count(e => e.Status == "completed");
        var failedExecutions = executions.Count(e => e.Status == "failed");

        var executionsByDay = executions
            .GroupBy(e => e.StartedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("MMM dd"),
                executions = g.Count(),
                successes = g.Count(e => e.Status == "completed"),
                failures = g.Count(e => e.Status == "failed")
            })
            .ToList();

        var topWorkflows = executions
            .GroupBy(e => e.WorkflowId)
            .Select(g => new
            {
                id = g.Key,
                name = workflows.FirstOrDefault(w => w.Id == g.Key)?.Name ?? "Unknown",
                executions = g.Count(),
                successRate = g.Count() > 0 ? Math.Round((double)g.Count(e => e.Status == "completed") / g.Count() * 100, 1) : 0
            })
            .OrderByDescending(w => w.executions)
            .Take(10)
            .ToList();

        var triggerBreakdown = workflows
            .GroupBy(w => w.TriggerType)
            .Select(g => new { triggerType = g.Key, count = g.Count() })
            .ToList();

        var recentFailures = await _context.WorkflowExecutions
            .Where(e => e.TenantId == tenantId && e.Status == "failed" && e.StartedAt >= since)
            .OrderByDescending(e => e.StartedAt)
            .Take(10)
            .Select(e => new
            {
                workflowName = _context.Workflows.Where(w => w.Id == e.WorkflowId).Select(w => w.Name).FirstOrDefault() ?? "Unknown",
                failedAt = e.StartedAt,
                errorMessage = e.ErrorMessage
            })
            .ToListAsync();

        return Ok(new
        {
            data = new
            {
                totalWorkflows = workflows.Count,
                activeWorkflows = workflows.Count(w => w.IsActive),
                totalExecutions,
                successfulExecutions,
                failedExecutions,
                avgSuccessRate = totalExecutions > 0 ? Math.Round((double)successfulExecutions / totalExecutions * 100, 1) : 0.0,
                executionsByDay,
                topWorkflows,
                triggerBreakdown,
                recentFailures
            }
        });
    }
}

// Request DTOs
public class CreateWorkflowRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public object? Steps { get; set; } // Stores full graph { nodes, edges }
}

public class UpdateWorkflowRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? TriggerType { get; set; }
    public object? Steps { get; set; }
}

public class WorkflowStepRequest
{
    public string Id { get; set; } = string.Empty; // Added ID for ReactFlow
    public string Type { get; set; } = string.Empty;
    public object? Data { get; set; } // Generic data object
    public object? Position { get; set; } // ReactFlow position
}

public class WorkflowUpdateStatusRequest
{
    public string? Status { get; set; }
}

public class TestWorkflowRequest
{
    public Dictionary<string, object>? Payload { get; set; }
}

