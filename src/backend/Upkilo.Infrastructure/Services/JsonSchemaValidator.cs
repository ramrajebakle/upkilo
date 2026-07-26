using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Validates JSONB fields against expected schemas before persistence
/// </summary>
public class JsonSchemaValidator
{
    private readonly ILogger<JsonSchemaValidator> _logger;

    private static readonly Dictionary<string, HashSet<string>> _schemas = new()
    {
        ["TenantSettings"] = new() { "timezone", "locale", "dateFormat", "currency", "businessType", "Enforce2FA", "notificationEmail", "bookingBuffer", "cancellationPolicy", "autoConfirm" },
        ["TenantMetadata"] = new() { "industry", "size", "website", "phone", "address", "city", "state", "country", "zipCode", "logoUrl" },
        ["UserPreferences"] = new() { "timezone", "locale", "theme", "notificationEmail", "notificationSms", "notificationPush", "dashboardLayout" },
        ["ServiceSettings"] = new() { "requireDeposit", "depositPercent", "bufferBefore", "bufferAfter", "maxAdvanceBookingDays", "minAdvanceBookingHours", "allowRecurring" },
        ["BookingMetadata"] = new() { "source", "referralCode", "notes", "customFields", "guestCount", "specialRequests" },
        ["ClientCustomFields"] = new() { },  // Dynamic — any keys allowed
        ["CampaignAudienceFilters"] = new() { "tags", "segments", "lastVisitDays", "minSpend", "maxSpend", "location", "serviceHistory" },
        ["WorkflowTriggerConfig"] = new() { "event", "conditions", "delay", "schedule" },
        ["WorkflowSteps"] = new() { "type", "action", "params", "nextOnSuccess", "nextOnFailure" }
    };

    public JsonSchemaValidator(ILogger<JsonSchemaValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates that a JSONB dictionary only contains allowed keys for the given schema
    /// </summary>
    public ValidationResult Validate(string schemaName, Dictionary<string, object>? data)
    {
        if (data == null) return ValidationResult.Valid();

        if (!_schemas.TryGetValue(schemaName, out var allowedKeys))
        {
            _logger.LogWarning("Unknown JSONB schema: {SchemaName}", schemaName);
            return ValidationResult.Valid(); // Unknown schemas pass (forward compatible)
        }

        if (allowedKeys.Count == 0) return ValidationResult.Valid(); // Dynamic schema

        var unknownKeys = data.Keys.Where(k => !allowedKeys.Contains(k)).ToList();
        if (unknownKeys.Any())
        {
            _logger.LogWarning("JSONB validation warning for {Schema}: unknown keys [{Keys}]",
                schemaName, string.Join(", ", unknownKeys));
            return new ValidationResult(true, unknownKeys); // Warn but allow (forward compatible)
        }

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Validates a raw JSON string against expected schema
    /// </summary>
    public ValidationResult ValidateJson(string schemaName, string? json)
    {
        if (string.IsNullOrEmpty(json)) return ValidationResult.Valid();

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return Validate(schemaName, data);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON for schema {Schema}", schemaName);
            return new ValidationResult(false, new List<string> { "Invalid JSON format" });
        }
    }
}

public class ValidationResult
{
    public bool IsValid { get; }
    public List<string> Warnings { get; }

    public ValidationResult(bool isValid, List<string>? warnings = null)
    {
        IsValid = isValid;
        Warnings = warnings ?? new();
    }

    public static ValidationResult Valid() => new(true);
}
