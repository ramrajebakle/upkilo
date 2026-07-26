using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;

/// <summary>
/// Tracks versioned prompt templates for AI agents with rollback capability.
/// Each prompt is stored with its version, allowing hot-swapping and rollback.
/// </summary>
[Table("prompt_versions")]
public class PromptVersion : TenantEntity
{
    /// <summary>
    /// Unique key identifying the prompt (e.g., "booking_assistant", "copywriting_agent", "churn_predictor").
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("prompt_key")]
    public string PromptKey { get; set; } = string.Empty;

    /// <summary>
    /// Semantic version of this prompt (e.g., "1.0.0", "1.1.0").
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("version")]
    public new string Version { get; set; } = "1.0.0";

    /// <summary>
    /// The system prompt content.
    /// </summary>
    [Required]
    [Column("system_prompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Optional user prompt template with {{placeholder}} variables.
    /// </summary>
    [Column("user_prompt_template")]
    public string? UserPromptTemplate { get; set; }

    /// <summary>
    /// Whether this version is the currently active version for the given prompt key.
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Description of changes from the previous version (changelog).
    /// </summary>
    [MaxLength(500)]
    [Column("change_description")]
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// Model to use with this prompt (e.g., "gpt-4", "gpt-4o", "gpt-3.5-turbo").
    /// </summary>
    [MaxLength(50)]
    [Column("model")]
    public string Model { get; set; } = "gpt-4";

    /// <summary>
    /// Temperature setting for this prompt version.
    /// </summary>
    [Column("temperature")]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Max tokens for this prompt version.
    /// </summary>
    [Column("max_tokens")]
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Who created/modified this version.
    /// </summary>
    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// When this version was activated or last promoted.
    /// </summary>
    [Column("activated_at")]
    public DateTime? ActivatedAt { get; set; }

    /// <summary>
    /// When this version was rolled back from (null if never rolled back).
    /// </summary>
    [Column("rolled_back_at")]
    public DateTime? RolledBackAt { get; set; }

    /// <summary>
    /// JSON metadata for additional model parameters (top_p, frequency_penalty, etc.)
    /// </summary>
    [Column("model_params", TypeName = "jsonb")]
    public string ModelParams { get; set; } = "{}";
}
