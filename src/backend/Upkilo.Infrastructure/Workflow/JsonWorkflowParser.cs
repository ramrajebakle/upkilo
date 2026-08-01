using System.Text.Json;
using Upkilo.Core.Interfaces.Workflow;

namespace Upkilo.Infrastructure.Workflow;

public class JsonWorkflowParser : IWorkflowParser
{
    public List<IWorkflowStepConfig> ParseSteps(string jsonConfig)
    {
        if (string.IsNullOrWhiteSpace(jsonConfig)) return new List<IWorkflowStepConfig>();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            // For a real implementation, this would use polymorphic deserialization based on StepType
            return JsonSerializer.Deserialize<List<IWorkflowStepConfig>>(jsonConfig, options)
                   ?? new List<IWorkflowStepConfig>();
        }
        catch
        {
            return new List<IWorkflowStepConfig>();
        }
    }

    public bool ValidateConfig(string jsonConfig, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            JsonDocument.Parse(jsonConfig);
            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = "Invalid JSON format: " + ex.Message;
            return false;
        }
    }
}
