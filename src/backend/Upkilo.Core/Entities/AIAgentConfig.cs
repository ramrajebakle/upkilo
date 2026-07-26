using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

public class AIAgentConfig : TenantEntity
{
    public string AgentName { get; set; } = "BookingAssistant";
    public string SystemPrompt { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public decimal HandoffConfidenceThreshold { get; set; } = 0.70m;
    public bool AutoBookEnabled { get; set; } = true;
    public string AllowedChannels { get; set; } = "WebChat,SMS"; // Comma-separated
    public Dictionary<string, string> HandoffTriggers { get; set; } = new(); // Condition -> Reasoning
}
