using System.Text.Json;
using System.Text.Json.Nodes;

namespace Upkilo.Infrastructure.Utils;

/// <summary>
/// Utility for validating JSONB objects against a defined schema.
/// </summary>
public static class JsonSchemaValidator
{
    public static (bool IsValid, string? Error) Validate(string json, Dictionary<string, string> schema)
    {
        try
        {
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            foreach (var field in schema)
            {
                if (!root.TryGetProperty(field.Key, out var property))
                {
                    return (false, $"Missing required property: {field.Key}");
                }

                var expectedType = field.Value.ToLower();
                var isValidType = expectedType switch
                {
                    "string" => property.ValueKind == JsonValueKind.String,
                    "number" => property.ValueKind == JsonValueKind.Number,
                    "boolean" => property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False,
                    "object" => property.ValueKind == JsonValueKind.Object,
                    "array" => property.ValueKind == JsonValueKind.Array,
                    _ => true
                };

                if (!isValidType)
                {
                    return (false, $"Invalid type for property: {field.Key}. Expected: {expectedType}");
                }
            }

            return (true, null);
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON format: {ex.Message}");
        }
    }
}
