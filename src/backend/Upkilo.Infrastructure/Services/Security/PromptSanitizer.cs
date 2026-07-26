using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services.Security;

/// <summary>
/// Sanitizes user input before sending to AI models to prevent prompt injection attacks.
/// Implements defense-in-depth: pattern detection, input normalization, length limits, and jailbreak detection.
/// </summary>
public class PromptSanitizer : IPromptSanitizer
{
    private readonly ILogger<PromptSanitizer> _logger;

    // Known prompt injection patterns
    private static readonly string[] InjectionPatterns =
    {
        @"ignore\s+(all\s+)?(previous|prior|above|earlier)\s+(instructions?|prompts?|rules?|directives?)",
        @"disregard\s+(all\s+)?(previous|prior|above)\s+(instructions?|prompts?)",
        @"forget\s+(everything|all|your)\s+(instructions?|rules?|training|guidelines)",
        @"you\s+are\s+now\s+(a|an)\s+",
        @"act\s+as\s+(if\s+)?(a|an|you)\s+",
        @"pretend\s+(to\s+be|you\s+are)\s+",
        @"new\s+instructions?:\s*",
        @"system\s*:\s*",
        @"<\|?(system|assistant|user)\|?>",
        @"\[INST\]",
        @"\[/INST\]",
        @"###\s*(instruction|system|human|assistant)",
        @"do\s+not\s+follow\s+(any|the)\s+(previous|above)",
        @"override\s+(your|all|the)\s+(instructions?|rules?|constraints?)",
        @"jailbreak",
        @"DAN\s+mode",
        @"developer\s+mode",
        @"bypass\s+(your\s+)?(restrictions?|filters?|safety|guidelines)",
    };

    private static readonly Regex[] CompiledPatterns = InjectionPatterns
        .Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled))
        .ToArray();

    // Maximum input length (characters)
    private const int MaxInputLength = 10_000;
    private const int MaxPromptLength = 50_000;

    public PromptSanitizer(ILogger<PromptSanitizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sanitizes user-provided input before it is injected into a prompt template.
    /// </summary>
    public SanitizationResult SanitizeUserInput(string input, Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return SanitizationResult.Safe(input);

        // Length enforcement
        if (input.Length > MaxInputLength)
        {
            _logger.LogWarning("Input truncated from {Original} to {Max} chars for tenant {TenantId}",
                input.Length, MaxInputLength, tenantId);
            input = input[..MaxInputLength];
        }

        // Normalize whitespace and remove control characters
        input = NormalizeInput(input);

        // Check for injection patterns
        var detectedPatterns = new List<string>();
        foreach (var pattern in CompiledPatterns)
        {
            if (pattern.IsMatch(input))
            {
                detectedPatterns.Add(pattern.ToString());
            }
        }

        if (detectedPatterns.Count > 0)
        {
            _logger.LogWarning(
                "Prompt injection attempt detected for tenant {TenantId}. Patterns: {Patterns}. Input (truncated): {Input}",
                tenantId, string.Join(", ", detectedPatterns), input[..Math.Min(200, input.Length)]);

            // Strip the injection content
            var sanitized = input;
            foreach (var pattern in CompiledPatterns)
            {
                sanitized = pattern.Replace(sanitized, "[REDACTED]");
            }

            return new SanitizationResult
            {
                IsClean = false,
                SanitizedInput = sanitized,
                OriginalInput = input,
                DetectedPatterns = detectedPatterns,
                RiskLevel = detectedPatterns.Count >= 3 ? RiskLevel.Critical : RiskLevel.High
            };
        }

        return SanitizationResult.Safe(input);
    }

    /// <summary>
    /// Sanitizes a complete prompt (system + user combined) for structural integrity.
    /// </summary>
    public SanitizationResult SanitizeFullPrompt(string prompt, Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return SanitizationResult.Safe(prompt);

        if (prompt.Length > MaxPromptLength)
        {
            _logger.LogWarning("Full prompt truncated from {Original} to {Max} chars", prompt.Length, MaxPromptLength);
            prompt = prompt[..MaxPromptLength];
        }

        // Detect multiple system-role markers (indicator of injection)
        var systemMarkerCount = Regex.Matches(prompt, @"(system\s*:|<\|?system\|?>|###\s*system)", RegexOptions.IgnoreCase).Count;
        if (systemMarkerCount > 1)
        {
            _logger.LogWarning("Multiple system markers detected ({Count}) in prompt for tenant {TenantId}", systemMarkerCount, tenantId);
            return new SanitizationResult
            {
                IsClean = false,
                SanitizedInput = prompt,
                OriginalInput = prompt,
                DetectedPatterns = new List<string> { "multiple_system_markers" },
                RiskLevel = RiskLevel.Critical
            };
        }

        return SanitizationResult.Safe(prompt);
    }

    private static string NormalizeInput(string input)
    {
        // Remove zero-width characters that can be used to bypass filters
        input = Regex.Replace(input, @"[\u200B-\u200F\u2028-\u202F\uFEFF]", string.Empty);

        // Replace control characters (except newlines/tabs)
        input = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);

        // Normalize excessive whitespace
        input = Regex.Replace(input, @"[ \t]{10,}", "  ");

        return input.Trim();
    }
}
