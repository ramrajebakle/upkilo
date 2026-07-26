using Upkilo.Core.Entities;
using WorkflowEntity = Upkilo.Core.Entities.Workflow;

namespace Upkilo.Core.Interfaces
{
    public interface IWorkflowService
    {
        // CRUD
        Task<IEnumerable<WorkflowEntity>> GetWorkflowsAsync(Guid tenantId);
        Task<WorkflowEntity?> GetWorkflowAsync(Guid id, Guid tenantId);
        Task<WorkflowEntity> CreateWorkflowAsync(WorkflowEntity workflow);
        Task<WorkflowEntity?> UpdateWorkflowAsync(WorkflowEntity workflow);
        Task<bool> DeleteWorkflowAsync(Guid id, Guid tenantId);

        // Engine
        Task ExecuteWorkflowAsync(WorkflowEntity workflow, WorkflowEvent triggerEvent);
        Task ExecuteStepAsync(Guid workflowId, int stepIndex, WorkflowEvent triggerEvent, Guid? executionId = null);
        Task ResumeWorkflowAsync(Guid workflowId, int stepIndex, WorkflowEvent triggerEvent, Guid? executionId = null);

        // Compensation (Saga Pattern)
        Task ExecuteCompensatoryStepsAsync(Guid executionId);
    }

    public class WorkflowStep
    {
        public string Type { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public System.Text.Json.JsonElement Config { get; set; }

        /// <summary>
        /// Compensation configuration for saga rollback. Defines how to undo this step on failure.
        /// </summary>
        public WorkflowStepCompensation? Compensation { get; set; }
    }

    public class WorkflowStepCompensation
    {
        /// <summary>
        /// The type of compensation action: "UndoTag", "CompensatingWebhook", "LogOnly", "SendNotification"
        /// </summary>
        public string CompensationType { get; set; } = "LogOnly";

        /// <summary>
        /// Configuration for the compensation action (e.g., URL for compensating webhook).
        /// </summary>
        public System.Text.Json.JsonElement? CompensationConfig { get; set; }

        /// <summary>
        /// Whether compensation should be skipped for this step (e.g., idempotent actions).
        /// </summary>
        public bool SkipCompensation { get; set; } = false;
    }
}
