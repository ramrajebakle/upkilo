using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities
{
    [Table("workflow_executions")]
    public class WorkflowExecution : TenantEntity
    {
        [Required]
        [Column("workflow_id")]
        public Guid WorkflowId { get; set; }

        public Workflow Workflow { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Running"; // Running, Completed, Failed, Paused

        [Required]
        [Column("current_step_index")]
        public int CurrentStepIndex { get; set; } = 0;

        [Column("trigger_event_data", TypeName = "jsonb")]
        public string TriggerEventData { get; set; } = "{}";

        [Column("started_at")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("retry_count")]
        public int RetryCount { get; set; } = 0;

        [Column("is_compensated")]
        public bool IsCompensated { get; set; } = false;

        public ICollection<WorkflowExecutionLog> Logs { get; set; } = new List<WorkflowExecutionLog>();
    }
}
