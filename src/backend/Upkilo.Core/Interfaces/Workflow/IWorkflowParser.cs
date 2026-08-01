namespace Upkilo.Core.Interfaces.Workflow;

/// <summary>
/// Interface for parsing JSON/YAML workflow configurations into executable objects.
/// </summary>
public interface IWorkflowParser
{
    /// <summary>
    /// Parses a JSON configuration string representing workflow steps into actionable objects.
    /// </summary>
    List<IWorkflowStepConfig> ParseSteps(string jsonConfig);

    /// <summary>
    /// Validates the workflow configuration to ensure correct parameters and logical flow.
    /// </summary>
    bool ValidateConfig(string jsonConfig, out string? errorMessage);
}

/// <summary>
/// Base config interface for all types of workflow steps (SendEmail, Wait, Condition, etc.)
/// </summary>
public interface IWorkflowStepConfig
{
    string StepName { get; }
    string StepType { get; }
}
