using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services.AI;

public interface IPromptSanitizer
{
    PromptSanitizationResult Sanitize(string prompt, PromptContext context);
}

public record PromptContext(Guid TenantId, string Feature, bool AllowSystemInstructions = false);

public class PromptSanitizationResult
{
    public string SanitizedPrompt { get; init; } = string.Empty;
    public bool WasModified { get; init; }
    public List<string> DetectedThreats { get; init; } = new();
    public bool IsBlocked { get; init; }
    public string? BlockReason { get; init; }
}

/// <summary>
/// AI-layer prompt injection defense. Detects and strips injection patterns, jailbreaks,
/// and data-exfiltration attempts before prompts reach the LLM.
/// </summary>
public class AIPromptSanitizer : IPromptSanitizer
{
    private readonly ILogger<AIPromptSanitizer> _logger;

    private const int MaxPromptLength = 8000;

    private static readonly (Regex Pattern, string ThreatName)[] ThreatPatterns =
    {
        (new Regex(@"ignore\s+(previous|above|all)\s+instructions?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "ignore_instructions"),
        (new Regex(@"reveal\s+(your\s+)?(system\s+)?prompt|show\s+(your\s+)?(instructions|prompt)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "system_prompt_leakage"),
        (new Regex(@"you\s+are\s+now|act\s+as\s+(a\s+)?(different|new|evil)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "role_hijacking"),
        (new Regex(@"DAN\s+mode|do\s+anything\s+now|developer\s+mode", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "jailbreak"),
        (new Regex(@"output\s+(all|your|my)\s+(data|database|users|customers)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "data_exfiltration"),
    };

    private static readonly HashSet<string> BlockOnDetect = new(StringComparer.OrdinalIgnoreCase)
    {
        "data_exfiltration",
        "jailbreak"
    };

    public AIPromptSanitizer(ILogger<AIPromptSanitizer> logger)
    {
        _logger = logger;
    }

    public PromptSanitizationResult Sanitize(string prompt, PromptContext context)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return new PromptSanitizationResult
            {
                SanitizedPrompt = prompt ?? string.Empty,
                WasModified = false,
            };
        }

        var detectedThreats = new List<string>();
        var sanitized = prompt;
        bool wasModified = false;

        // Check each pattern
        foreach (var (pattern, threatName) in ThreatPatterns)
        {
            if (pattern.IsMatch(sanitized))
            {
                detectedThreats.Add(threatName);
                sanitized = pattern.Replace(sanitized, string.Empty);
                wasModified = true;

                _logger.LogWarning(
                    "AI prompt threat detected. TenantId={TenantId}, Feature={Feature}, Threat={Threat}",
                    context.TenantId, context.Feature, threatName);
            }
        }

        // Block if high-severity threat or too many threats detected
        bool isBlocked = false;
        string? blockReason = null;

        var hasSevereThreat = detectedThreats.Any(t => BlockOnDetect.Contains(t));
        if (hasSevereThreat || detectedThreats.Count > 2)
        {
            isBlocked = true;
            blockReason = hasSevereThreat
                ? $"High-severity threat detected: {detectedThreats.First(t => BlockOnDetect.Contains(t))}"
                : $"Multiple injection threats detected ({detectedThreats.Count})";

            _logger.LogWarning(
                "AI prompt BLOCKED. TenantId={TenantId}, Feature={Feature}, Reason={Reason}",
                context.TenantId, context.Feature, blockReason);
        }

        // Truncate to max allowed length
        if (sanitized.Length > MaxPromptLength)
        {
            sanitized = sanitized[..MaxPromptLength];
            wasModified = true;
        }

        return new PromptSanitizationResult
        {
            SanitizedPrompt = sanitized.Trim(),
            WasModified = wasModified,
            DetectedThreats = detectedThreats,
            IsBlocked = isBlocked,
            BlockReason = blockReason,
        };
    }
}
