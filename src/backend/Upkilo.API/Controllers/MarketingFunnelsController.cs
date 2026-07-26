using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using System.Text.Json;
using Upkilo.Core.Interfaces.Workflow;

namespace Upkilo.API.Controllers;

/// <summary>
/// Handles GoHighLevel-style multi-step automated marketing funnels and workflows.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class MarketingFunnelsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<MarketingFunnelsController> _logger;

    public MarketingFunnelsController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        IWorkflowService workflowService,
        ILogger<MarketingFunnelsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _workflowService = workflowService;
        _logger = logger;
    }

    /// <summary>
    /// Get all workflows/funnels for the current tenant.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFunnels()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var funnels = await _context.Workflows
            .Where(w => w.TenantId == tenantId && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.Description,
                w.TriggerType,
                w.IsActive,
                w.CreatedAt,
                w.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { data = funnels });
    }

    /// <summary>
    /// Get a workflow/funnel by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFunnel(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var funnel = await _context.Workflows
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted);

        if (funnel == null) return NotFound();

        return Ok(funnel);
    }

    /// <summary>
    /// Create or update a workflow/funnel.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveFunnel([FromBody] SaveFunnelRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        if (string.IsNullOrWhiteSpace(request.TriggerType))
            return BadRequest(new { error = "Trigger type is required." });

        // Basic JSON validation
        try
        {
            if (!string.IsNullOrEmpty(request.TriggerConfig))
                JsonDocument.Parse(request.TriggerConfig);
            if (!string.IsNullOrEmpty(request.Steps))
                JsonDocument.Parse(request.Steps);
        }
        catch (JsonException ex)
        {
            return BadRequest(new { error = $"Invalid JSON config: {ex.Message}" });
        }

        Workflow? funnel = null;

        if (request.Id.HasValue)
        {
            funnel = await _context.Workflows
                .FirstOrDefaultAsync(w => w.Id == request.Id.Value && w.TenantId == tenantId && !w.IsDeleted);

            if (funnel == null) return NotFound();
        }
        else
        {
            funnel = new Workflow
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.Workflows.Add(funnel);
        }

        funnel.Name = request.Name;
        funnel.Description = request.Description ?? string.Empty;
        funnel.TriggerType = request.TriggerType;
        funnel.TriggerConfig = request.TriggerConfig ?? "{}";
        funnel.Steps = request.Steps ?? "[]";
        funnel.IsActive = request.IsActive;
        funnel.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Marketing funnel saved: {FunnelId} - {Name} for Tenant {TenantId}", funnel.Id, funnel.Name, tenantId);

        return Ok(funnel);
    }

    /// <summary>
    /// Delete a workflow/funnel (soft delete).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFunnel(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var funnel = await _context.Workflows
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted);

        if (funnel == null) return NotFound();

        funnel.IsDeleted = true;
        funnel.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Marketing funnel deleted: {FunnelId} for Tenant {TenantId}", id, tenantId);

        return NoContent();
    }

    /// <summary>
    /// Get executions and step-by-step logs/telemetry for a workflow.
    /// </summary>
    [HttpGet("{id}/executions")]
    public async Task<IActionResult> GetExecutions(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var executions = await _context.WorkflowExecutions
            .Where(e => e.WorkflowId == id && e.TenantId == tenantId)
            .OrderByDescending(e => e.StartedAt)
            .Take(50)
            .ToListAsync();

        var executionIds = executions.Select(e => e.Id).ToList();

        var logs = await _context.WorkflowExecutionLogs
            .Where(l => executionIds.Contains(l.WorkflowExecutionId) && l.TenantId == tenantId)
            .OrderByDescending(l => l.ExecutedAt)
            .ToListAsync();

        var telemetry = executions.Select(e => new
        {
            e.Id,
            e.Status,
            e.CurrentStepIndex,
            e.TriggerEventData,
            e.StartedAt,
            e.CompletedAt,
            e.ErrorMessage,
            e.RetryCount,
            e.IsCompensated,
            Steps = logs.Where(l => l.WorkflowExecutionId == e.Id).Select(l => new
            {
                l.StepIndex,
                l.StepType,
                l.ActionType,
                l.Status,
                l.DurationMs,
                l.ErrorDetails,
                l.ExecutedAt
            }).OrderBy(l => l.StepIndex).ToList()
        }).ToList();

        // Calculate aggregates
        int totalExecutions = executions.Count;
        int successfulExecutions = executions.Count(e => e.Status == "Completed");
        int failedExecutions = executions.Count(e => e.Status == "Failed");
        int throttledExecutions = executions.Count(e => e.Status == "Throttled");
        double avgDurationSeconds = executions
            .Where(e => e.CompletedAt.HasValue)
            .Select(e => (e.CompletedAt!.Value - e.StartedAt).TotalSeconds)
            .DefaultIfEmpty(0)
            .Average();

        return Ok(new
        {
            summary = new
            {
                totalExecutions,
                successfulExecutions,
                failedExecutions,
                throttledExecutions,
                avgDurationSeconds = Math.Round(avgDurationSeconds, 2)
            },
            data = telemetry
        });
    }

    /// <summary>
    /// Triggers a test execution of a workflow.
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestFunnel(Guid id, [FromBody] Dictionary<string, object> testData)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var funnel = await _context.Workflows
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted);

        if (funnel == null) return NotFound();

        var testEvent = new WorkflowEvent
        {
            EventName = funnel.TriggerType,
            Data = testData,
            TenantId = tenantId.Value,
            OccurredAt = DateTime.UtcNow
        };

        try
        {
            await _workflowService.ExecuteWorkflowAsync(funnel, testEvent);
            return Ok(new { success = true, message = "Test execution triggered successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run test execution for workflow {WorkflowId}", id);
            return StatusCode(500, new { error = $"Trigger failed: {ex.Message}" });
        }
    }
}

public record SaveFunnelRequest(
    Guid? Id,
    string Name,
    string? Description,
    string TriggerType,
    string? TriggerConfig,
    string? Steps,
    bool IsActive
);
