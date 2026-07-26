using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities
{
    [Table("workflow_execution_logs")]
    public class WorkflowExecutionLog : TenantEntity
    {
        [Required]
        [Column("workflow_execution_id")]
        public Guid WorkflowExecutionId { get; set; }

        public WorkflowExecution Execution { get; set; } = null!;

        [Required]
        [Column("step_index")]
        public int StepIndex { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("step_type")]
        public string StepType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("action_type")]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Success"; // Success, Failed

        [Column("executed_at")]
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        [Column("duration_ms")]
        public int DurationMs { get; set; }

        [Column("message")]
        public string? Message { get; set; }

        [Column("error_details")]
        public string? ErrorDetails { get; set; }
    }
}
