namespace Upkilo.Core.Interfaces;

public interface IPromptSanitizer
{
    SanitizationResult SanitizeUserInput(string input, Guid? tenantId = null);
    SanitizationResult SanitizeFullPrompt(string prompt, Guid? tenantId = null);
}

public class SanitizationResult
{
    public bool IsClean { get; set; }
    public string SanitizedInput { get; set; } = string.Empty;
    public string OriginalInput { get; set; } = string.Empty;
    public List<string> DetectedPatterns { get; set; } = new();
    public RiskLevel RiskLevel { get; set; } = RiskLevel.None;

    public static SanitizationResult Safe(string input) => new()
    {
        IsClean = true,
        SanitizedInput = input,
        OriginalInput = input,
        RiskLevel = RiskLevel.None
    };
}

public enum RiskLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
