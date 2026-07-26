using Upkilo.Core.Interfaces.Workflow;

namespace Upkilo.Infrastructure.Workflow;

/// <summary>
/// Base class for all workflow step executors. 
/// Inherit this to implement concrete steps like SendEmailStep, WaitStep, etc.
/// </summary>
public abstract class WorkflowStepExecutorBase : IWorkflowStepExecutor
{
    public abstract Task<WorkflowStepResult> ExecuteAsync(IWorkflowStepConfig config, WorkflowContext context);

    public virtual bool HasCustomRetryPolicy()
    {
        return false; // Defaults to the global Polly retry policy used by the workflow engine
    }

    public virtual Task CompensateAsync(IWorkflowStepConfig config, WorkflowContext context)
    {
        // Default implementation is a no-op. Concrete executors should override if they have side effects to revert.
        return Task.CompletedTask;
    }
    
    protected WorkflowStepResult Success(string? outputJson = null)
    {
        return new WorkflowStepResult { Success = true, OutputDataJson = outputJson };
    }

    protected WorkflowStepResult Failure(string error)
    {
        return new WorkflowStepResult { Success = false, ErrorMessage = error };
    }

    protected WorkflowStepResult Wait(TimeSpan duration)
    {
        return new WorkflowStepResult { Success = true, RequiresWaitTime = true, WaitDuration = duration };
    }
}
