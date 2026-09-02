using Azure;
using Azure.AI.ContentSafety;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services.Security;

/// <summary>
/// Azure AI Content Safety integration for screening AI-generated outputs using the official Azure SDK.
/// Checks for harmful content categories: Hate, Violence, SelfHarm, Sexual.
/// </summary>
public class ContentModerationService : IContentModerationService
{
    private readonly ContentSafetyClient? _client;
    private readonly ILogger<ContentModerationService> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly bool _isEnabled;

    // Threshold: 0=safe, 2=low, 4=medium, 6=high severity
    private const int DefaultSeverityThreshold = 2;

    public ContentModerationService(
        IConfiguration configuration,
        ILogger<ContentModerationService> logger,
        IHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        var endpoint = configuration["AzureContentSafety:Endpoint"] ?? string.Empty;
        var apiKey = configuration["AzureContentSafety:ApiKey"] ?? string.Empty;
        _isEnabled = !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey);

        // Logged, NOT thrown. This is a scoped service injected into AiService and
        // AzureOpenAIService, which are themselves injected widely — so throwing here failed
        // DI resolution for every endpoint whose dependency graph merely touched them, whether
        // or not it moderated anything. Production had no AzureContentSafety settings at all,
        // so a large part of the API answered
        //
        //   InvalidOperationException: Azure Content Safety API client is not configured
        //
        // and the dashboard showed "Couldn't load this. Check your connection." on page after
        // page, for the owner included.
        //
        // The safety requirement is real and is kept — it just belongs at the moderation call,
        // not at construction. See ModerateTextAsync, which now REFUSES in Production rather
        // than returning Allowed(). That is strictly safer than the previous arrangement: the
        // throw only ever protected content by taking the whole service down, and if it had
        // been caught anywhere, moderation would have silently failed open.
        if (hostEnvironment.IsProduction() && !_isEnabled)
        {
            _logger.LogCritical(
                "Azure Content Safety is not configured (AzureContentSafety:Endpoint / :ApiKey). " +
                "Content moderation cannot run, so every moderated operation will be REFUSED in " +
                "Production until it is configured.");
        }

        if (_isEnabled)
        {
            try
            {
                _client = new ContentSafetyClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Content Safety Client with endpoint: {Endpoint}", endpoint);
                _isEnabled = false;
            }
        }
    }

    /// <summary>
    /// Screens text content for harmful material. Returns moderation result with category scores.
    /// </summary>
    public async Task<ModerationResult> ModerateTextAsync(string text, int? severityThreshold = null, CancellationToken ct = default)
    {
        if (!_isEnabled || _client == null)
        {
            // Fail CLOSED in Production. "The moderator is unavailable" is not evidence that
            // the text is safe, so a caller that asked to moderate gets a refusal, not a pass.
            // Outside Production it stays permissive so local and CI runs need no Azure
            // resource.
            //
            // Scope, accurately: the only production caller is ContentModerationController.
            // AI generation does NOT pass through here. IAIService.CheckSafetyAsync wraps this
            // method but nothing outside tests calls it, so prompts and completions currently
            // reach Azure OpenAI unmoderated. That is a pre-existing gap in the generation
            // path, not something this refusal covers - do not read this method as protecting
            // it.
            if (_hostEnvironment.IsProduction())
            {
                _logger.LogError(
                    "Refusing content: Azure Content Safety is not configured, so moderation "
                    + "cannot run and this operation cannot be allowed in Production.");
                return ModerationResult.Blocked("ModerationUnavailable");
            }

            _logger.LogDebug("Content moderation is disabled (no Azure Content Safety endpoint configured)");
            return ModerationResult.Allowed();
        }

        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Allowed();

        var threshold = severityThreshold ?? DefaultSeverityThreshold;

        try
        {
            // Truncate to Azure Content Safety limit (10,000 chars per request)
            if (text.Length > 10_000)
                text = text[..10_000];

            var options = new AnalyzeTextOptions(text);
            var response = await _client.AnalyzeTextAsync(options, ct);

            if (response?.Value == null)
            {
                _logger.LogWarning("Content moderation API returned null response");
                return ModerationResult.Allowed();
            }

            var flaggedCategories = new List<FlaggedCategory>();
            bool isBlocked = false;
            var rawScores = new Dictionary<string, int>();

            foreach (var analysis in response.Value.CategoriesAnalysis)
            {
                var categoryName = analysis.Category.ToString();
                var severity = analysis.Severity ?? 0;
                rawScores[categoryName] = severity;

                if (severity >= threshold)
                {
                    isBlocked = true;
                    flaggedCategories.Add(new FlaggedCategory
                    {
                        Category = categoryName,
                        Severity = severity
                    });
                }
            }

            if (isBlocked)
            {
                _logger.LogWarning("Content blocked by moderation. Categories: {Categories}",
                    string.Join(", ", flaggedCategories.Select(f => $"{f.Category}:{f.Severity}")));
            }

            return new ModerationResult
            {
                IsAllowed = !isBlocked,
                FlaggedCategories = flaggedCategories,
                RawScores = rawScores
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content moderation service error");
            // Fail open to avoid blocking legitimate content
            return ModerationResult.Allowed();
        }
    }

    /// <summary>
    /// Screens AI-generated output and returns sanitized version if content is flagged.
    /// </summary>
    public async Task<(string output, ModerationResult moderation)> ModerateAndSanitizeAsync(
        string aiOutput, CancellationToken ct = default)
    {
        var result = await ModerateTextAsync(aiOutput, ct: ct);
        if (result.IsAllowed)
            return (aiOutput, result);

        // Replace flagged content with safe message
        var safeOutput = "[Content removed: This response was flagged by our safety system. " +
                         $"Categories: {string.Join(", ", result.FlaggedCategories.Select(f => f.Category))}. " +
                         "Please try rephrasing your request.]";

        return (safeOutput, result);
    }
}
