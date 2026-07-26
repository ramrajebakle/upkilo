using System;

namespace Upkilo.Core.Entities;

public class AIMessage : TenantEntity
{
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ToolCalls { get; set; } // JSON list of OpenAI tool calls
    public string? ToolOutputs { get; set; } // JSON output of tools
    public decimal? TokenUsage { get; set; }
    public decimal? Cost { get; set; }
    public string? Metadata { get; set; } // Channel-specific metadata

    // Navigation
    public virtual AIConversation? Conversation { get; set; }
}

public enum MessageRole
{
    System,
    User,
    Assistant,
    Tool
}
