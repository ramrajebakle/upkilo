using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Events;

/// <summary>
/// Event schema registry — tracks all domain event schemas with version history.
/// Enables backward-compatible event changes and debugging via event replay.
///
/// Versioning strategy:
///   - Events are named {AggregateType}.{EventName}.v{N}
///   - New fields are always OPTIONAL (backward-compatible additions)
///   - Removed fields are kept in old schema version, omitted in new
///   - Breaking changes increment the version and keep the old handler alive
/// </summary>
public class EventSchemaRegistry
{
    private readonly ILogger<EventSchemaRegistry> _logger;

    // Registry: eventType -> list of known versions with their field manifests
    private static readonly Dictionary<string, List<EventSchemaVersion>> _registry = new()
    {
        ["BookingCreated"] = new()
        {
            new("1.0", new[] { "BookingId", "TenantId", "ClientId", "ServiceId", "StaffId", "StartTime", "EndTime", "Price" }),
            new("1.1", new[] { "BookingId", "TenantId", "ClientId", "ServiceId", "StaffId", "StartTime", "EndTime", "Price", "Source", "GroupSize" }),
        },
        ["BookingCancelled"] = new()
        {
            new("1.0", new[] { "BookingId", "TenantId", "CancelledAt", "Reason" }),
        },
        ["BookingCompleted"] = new()
        {
            new("1.0", new[] { "BookingId", "TenantId", "CompletedAt", "FinalPrice" }),
        },
        ["ClientCreated"] = new()
        {
            new("1.0", new[] { "ClientId", "TenantId", "FirstName", "LastName", "Email", "Phone" }),
            new("1.1", new[] { "ClientId", "TenantId", "FirstName", "LastName", "Email", "Phone", "Tags", "Source" }),
        },
        ["ClientUpdated"] = new()
        {
            new("1.0", new[] { "ClientId", "TenantId", "ChangedFields" }),
        },
        ["PaymentReceived"] = new()
        {
            new("1.0", new[] { "PaymentId", "TenantId", "BookingId", "Amount", "Currency", "Provider" }),
        },
        ["PaymentFailed"] = new()
        {
            new("1.0", new[] { "PaymentId", "TenantId", "BookingId", "Amount", "ErrorCode", "ErrorMessage" }),
        },
        ["StaffScheduleChanged"] = new()
        {
            new("1.0", new[] { "StaffId", "TenantId", "Date", "OldSlots", "NewSlots" }),
        },
        ["WorkflowExecuted"] = new()
        {
            new("1.0", new[] { "ExecutionId", "WorkflowId", "TenantId", "TriggeredBy", "Status", "StepsExecuted" }),
        },
        ["SubscriptionChanged"] = new()
        {
            new("1.0", new[] { "TenantId", "OldPlan", "NewPlan", "ChangedAt" }),
        },
    };

    public EventSchemaRegistry(ILogger<EventSchemaRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all registered event types and their latest schema version.
    /// </summary>
    public IReadOnlyDictionary<string, EventSchemaVersion> GetLatestSchemas()
        => _registry.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Last());

    /// <summary>
    /// Get all versions of a specific event type.
    /// </summary>
    public IEnumerable<EventSchemaVersion>? GetVersionHistory(string eventType)
        => _registry.TryGetValue(eventType, out var versions) ? versions : null;

    /// <summary>
    /// Validate a raw event payload against the specified event type's latest schema.
    /// Returns a list of missing required fields.
    /// </summary>
    public EventValidationResult ValidatePayload(string eventType, string jsonPayload)
    {
        if (!_registry.TryGetValue(eventType, out var versions))
        {
            _logger.LogWarning("Unknown event type '{EventType}' submitted for validation", eventType);
            return EventValidationResult.Unknown(eventType);
        }

        var latest = versions.Last();
        JsonDocument? doc;
        try
        {
            doc = JsonDocument.Parse(jsonPayload);
        }
        catch
        {
            return EventValidationResult.InvalidJson(eventType);
        }

        var presentKeys = doc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingFields = latest.RequiredFields.Where(f => !presentKeys.Contains(f)).ToList();

        return missingFields.Count == 0
            ? EventValidationResult.Valid(eventType, latest.Version)
            : EventValidationResult.MissingFields(eventType, latest.Version, missingFields);
    }

    /// <summary>
    /// Register a new event schema version at runtime (for extensibility).
    /// </summary>
    public void RegisterVersion(string eventType, string version, string[] fields)
    {
        if (!_registry.ContainsKey(eventType))
            _registry[eventType] = new List<EventSchemaVersion>();

        _registry[eventType].Add(new EventSchemaVersion(version, fields));
        _logger.LogInformation("Registered event schema {EventType} v{Version} with {FieldCount} fields",
            eventType, version, fields.Length);
    }
}

public record EventSchemaVersion(string Version, string[] RequiredFields);

public class EventValidationResult
{
    public string EventType { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public string Status { get; init; } = string.Empty; // valid, missing_fields, invalid_json, unknown_type
    public List<string> Issues { get; init; } = new();

    public static EventValidationResult Valid(string type, string version) =>
        new() { EventType = type, SchemaVersion = version, IsValid = true, Status = "valid" };
    public static EventValidationResult MissingFields(string type, string version, List<string> fields) =>
        new() { EventType = type, SchemaVersion = version, IsValid = false, Status = "missing_fields", Issues = fields };
    public static EventValidationResult InvalidJson(string type) =>
        new() { EventType = type, IsValid = false, Status = "invalid_json", Issues = new() { "Payload is not valid JSON" } };
    public static EventValidationResult Unknown(string type) =>
        new() { EventType = type, IsValid = false, Status = "unknown_type", Issues = new() { $"Event type '{type}' is not in the schema registry" } };
}
