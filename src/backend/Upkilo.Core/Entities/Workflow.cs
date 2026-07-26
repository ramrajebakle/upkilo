using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Upkilo.Core.Entities
{
    [Table("workflows")]
    public class Workflow : TenantEntity
    {
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [MaxLength(50)]
        [Column("trigger_type")]
        public string TriggerType { get; set; } = string.Empty; // e.g., "ClientCreated", "BookingConfirmed"

        [Column("trigger_config", TypeName = "jsonb")]
        public string TriggerConfig { get; set; } = "{}"; // JSON config for the trigger (filters, etc.)

        [Column("steps", TypeName = "jsonb")]
        public string Steps { get; set; } = "[]"; // JSON array of steps/actions
    }
}
