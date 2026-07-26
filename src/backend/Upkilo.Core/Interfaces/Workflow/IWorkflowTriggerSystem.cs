namespace Upkilo.Core.Interfaces.Workflow;

/// <summary>
/// Contract for registering triggers that begin a workflow execution.
/// </summary>
public interface IWorkflowTriggerSystem
{
    /// <summary>
    /// Evaluates incoming domain events to see if they match any active Workflow Definitions.
    /// Should be called by EventDispatcher/Outbox pattern.
    /// </summary>
    Task EvaluateEventTriggerAsync(string eventType, string eventDataJson, Guid tenantId);
    
    /// <summary>
    /// Manual trigger bypassing event criteria. Used for user-spawned ad-hoc workflows.
    /// </summary>
    Task<Guid> TriggerWorkflowManuallyAsync(Guid workflowDefinitionId, string targetEntityId);
}
