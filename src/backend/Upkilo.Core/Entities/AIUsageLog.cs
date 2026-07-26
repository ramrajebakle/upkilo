namespace Upkilo.Core.Entities;

/// <summary>
/// AI usage tracking for billing and limits
/// </summary>
public class AIUsageLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Model { get; set; } = "gpt-4"; // gpt-4, gpt-3.5-turbo, dall-e-3
    public string Feature { get; set; } = string.Empty; // copywriting, image-gen, chatbot
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; } // In USD
    public int? LatencyMs { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
