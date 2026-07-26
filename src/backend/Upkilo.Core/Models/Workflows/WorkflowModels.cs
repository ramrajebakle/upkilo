using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Upkilo.Core.Models.Workflows;

/// <summary>
/// Trigger configuration object representation of Workflow.TriggerConfig JSON blob
/// </summary>
public class TriggerConfig
{
    [JsonPropertyName("filters")]
    public List<TriggerFilter> Filters { get; set; } = new();

    [JsonPropertyName("debounceSeconds")]
    public int? DebounceSeconds { get; set; }

    [JsonPropertyName("scheduledCron")]
    public string? ScheduledCron { get; set; }
}

public class TriggerFilter
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "Equals"; // Equals, NotEquals, Contains, GreaterThan, LessThan

    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

/// <summary>
/// Config for Delay / Wait actions
/// </summary>
public class WaitActionConfig
{
    [JsonPropertyName("durationMinutes")]
    public int? DurationMinutes { get; set; }

    [JsonPropertyName("waitUntil")]
    public DateTime? WaitUntil { get; set; }

    [JsonPropertyName("waitForEvent")]
    public string? WaitForEvent { get; set; } // e.g. "PaymentSuccess"
}

public class StepExecutionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ShouldRetry { get; set; }
    public int RetryDelaySeconds { get; set; }
}
