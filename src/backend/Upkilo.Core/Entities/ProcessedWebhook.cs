using System;

namespace Upkilo.Core.Entities;

public class ProcessedWebhook : BaseEntity
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
