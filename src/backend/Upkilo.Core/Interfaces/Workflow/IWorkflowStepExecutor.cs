namespace Upkilo.Core.Interfaces.Workflow;

/// <summary>
/// Base executor for resolving a single step inside a workflow instance.
/// </summary>
public interface IWorkflowStepExecutor
{
    /// <summary>
    /// Executes the logical business domain of the workflow step.
    /// </summary>
    Task<WorkflowStepResult> ExecuteAsync(IWorkflowStepConfig config, WorkflowContext context);

    /// <summary>
    /// Defines if this executor dictates a custom retry policy or relies on the global configuration.
    /// </summary>
    bool HasCustomRetryPolicy();

    /// <summary>
    /// Reverts or compensates the effects of this step if a subsequent step fails.
    /// </summary>
    Task CompensateAsync(IWorkflowStepConfig config, WorkflowContext context);
}

/// <summary>
/// Contains ongoing state data for a workflow instance's execution boundary.
/// </summary>
public class WorkflowContext
{
    public Guid TenantId { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string TargetEntityId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
}

/// <summary>
/// Result object wrapping step execution outcome.
/// </summary>
public class WorkflowStepResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputDataJson { get; set; }
    public bool RequiresWaitTime { get; set; } // E.g., for "Delay" steps
    public TimeSpan? WaitDuration { get; set; }
}
