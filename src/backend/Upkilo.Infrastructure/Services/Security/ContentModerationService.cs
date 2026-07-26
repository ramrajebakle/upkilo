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

        if (hostEnvironment.IsProduction() && !_isEnabled)
        {
            throw new InvalidOperationException("Azure Content Safety API client is not configured but is mandatory in Production environment. Please configure AzureContentSafety:Endpoint and AzureContentSafety:ApiKey.");
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
