using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services.AI;

public interface IJsonSchemaEnforcer
{
    Task<JsonEnforcementResult> EnforceAsync(string aiOutput, string expectedSchema, CancellationToken ct = default);
}

public class JsonEnforcementResult
{
    public bool IsValid { get; init; }
    public string? ValidatedJson { get; init; }
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Validates and extracts well-formed JSON from AI output, ensuring required top-level
/// keys (as declared in the schema string) are present.
/// </summary>
public class JsonSchemaEnforcer : IJsonSchemaEnforcer
{
    private readonly ILogger<JsonSchemaEnforcer> _logger;

    public JsonSchemaEnforcer(ILogger<JsonSchemaEnforcer> logger)
    {
        _logger = logger;
    }

    public Task<JsonEnforcementResult> EnforceAsync(string aiOutput, string expectedSchema, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(aiOutput))
        {
            return Task.FromResult(new JsonEnforcementResult
            {
                IsValid = false,
                Errors = new List<string> { "AI output is empty." },
            });
        }

        // Strip markdown code fences if present
        var cleaned = aiOutput.Trim();
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[7..];
        else if (cleaned.StartsWith("```"))
            cleaned = cleaned[3..];
        if (cleaned.EndsWith("```"))
            cleaned = cleaned[..^3];
        cleaned = cleaned.Trim();

        // Attempt to parse the JSON
        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(cleaned);
        }
        catch (JsonException)
        {
            // Try to extract JSON between first '{' and last '}'
            var firstBrace = cleaned.IndexOf('{');
            var lastBrace = cleaned.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                var extracted = cleaned[firstBrace..(lastBrace + 1)];
                try
                {
                    document = JsonDocument.Parse(extracted);
                    cleaned = extracted;
                    _logger.LogInformation("JSON extracted from surrounding text in AI output.");
                }
                catch (JsonException ex2)
                {
                    _logger.LogWarning(ex2, "Failed to extract valid JSON from AI output.");
                    return Task.FromResult(new JsonEnforcementResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "AI output does not contain valid JSON.", ex2.Message },
                    });
                }
            }
            else
            {
                return Task.FromResult(new JsonEnforcementResult
                {
                    IsValid = false,
                    Errors = new List<string> { "AI output does not contain a JSON object." },
                });
            }
        }

        // Validate required keys from expectedSchema (comma-separated field names)
        var errors = new List<string>();
        if (!string.IsNullOrWhiteSpace(expectedSchema) && document != null)
        {
            var requiredKeys = expectedSchema
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in requiredKeys)
                {
                    if (!document.RootElement.TryGetProperty(key, out _))
                    {
                        errors.Add($"Required key '{key}' is missing from AI output.");
                    }
                }
            }
            else
            {
                errors.Add("AI output JSON root is not an object.");
            }
        }

        document?.Dispose();

        if (errors.Count > 0)
        {
            return Task.FromResult(new JsonEnforcementResult
            {
                IsValid = false,
                ValidatedJson = cleaned,
                Errors = errors,
            });
        }

        return Task.FromResult(new JsonEnforcementResult
        {
            IsValid = true,
            ValidatedJson = cleaned,
            Errors = new List<string>(),
        });
    }
}
