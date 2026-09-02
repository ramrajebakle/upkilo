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

    /// <summary>
    /// Refuses the content. Used both for a genuine policy hit and for the fail-closed path
    /// when moderation cannot run at all in an environment that requires it — an unavailable
    /// moderator must never read as "this text is fine".
    /// </summary>
    public static ModerationResult Blocked(string category, int severity = int.MaxValue) => new()
    {
        IsAllowed = false,
        FlaggedCategories = { new FlaggedCategory { Category = category, Severity = severity } },
    };
}

public class FlaggedCategory
{
    public string Category { get; set; } = string.Empty;
    public int Severity { get; set; }
}
