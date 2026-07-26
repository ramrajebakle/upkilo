namespace Upkilo.Core.Interfaces;

public interface IContentModerationService
{
    Task<ModerationResult> ModerateTextAsync(string text, int? severityThreshold = null, CancellationToken ct = default);
    Task<(string output, ModerationResult moderation)> ModerateAndSanitizeAsync(string aiOutput, CancellationToken ct = default);
}

public class ModerationResult
{
    public bool IsAllowed { get; set; }
    public List<FlaggedCategory> FlaggedCategories { get; set; } = new();
    public Dictionary<string, int> RawScores { get; set; } = new();

    public static ModerationResult Allowed() => new() { IsAllowed = true };
}

public class FlaggedCategory
{
    public string Category { get; set; } = string.Empty;
    public int Severity { get; set; }
}
