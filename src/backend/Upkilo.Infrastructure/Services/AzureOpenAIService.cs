using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class AzureOpenAIService : IAIService
{
    // Deployment used when the tier-appropriate model fails. Must exist in the Azure OpenAI
    // resource and be present in every AllowedAiModels list, or the fallback fails too.
    private const string FallbackModel = "gpt-5-mini";

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly INotificationService _notificationService;
    private readonly IContentModerationService _contentModerationService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StackExchange.Redis.IConnectionMultiplexer _redis;
    private readonly IPiiScrubberService _piiScrubber;
    private readonly IAiModelResolver _modelResolver;


    public AzureOpenAIService(
        AppDbContext context,
        IConfiguration configuration,
        ISecretProvider secretProvider,
        ILogger<AzureOpenAIService> logger,
        HttpClient httpClient,
        IDistributedCache cache,
        INotificationService notificationService,
        IContentModerationService contentModerationService,
        IServiceScopeFactory scopeFactory,
        StackExchange.Redis.IConnectionMultiplexer redis,
        IPiiScrubberService piiScrubber,
        IAiModelResolver modelResolver)
    {
        _context = context;
        _configuration = configuration;
        _secretProvider = secretProvider;
        _logger = logger;
        _httpClient = httpClient;   // typed client — carries Polly retry + circuit breaker
        _cache = cache;
        _notificationService = notificationService;
        _contentModerationService = contentModerationService;
        _scopeFactory = scopeFactory;
        _redis = redis;
        _piiScrubber = piiScrubber;
        _modelResolver = modelResolver;
    }

    public async Task<AIGenerationResult> GenerateTextAsync(Guid tenantId, Guid? userId, string prompt, string? model = null)
    {
        var startTime = DateTime.UtcNow;
        bool isFallback = false;
        // Resolve tier-appropriate model; caller override respected if provided
        model = string.IsNullOrWhiteSpace(model)
            ? await _modelResolver.ResolveAsync(tenantId)
            : model;
        string originalModel = model;
        decimal estimatedCost = 0m;

        try
        {
            if (!await IsModelAllowedAsync(tenantId, model))
            {
                return new AIGenerationResult { Success = false, Error = $"AI Model '{model}' is not allowed for your subscription." };
            }

            int estimatedInputTokens = prompt.Length / 4;
            // Full-size models get the larger estimate; "mini"/"nano" variants the smaller one.
            // This previously keyed off Contains("gpt-4"), which silently stopped matching once
            // the models moved to the gpt-5 family.
            bool isCompactModel = model.Contains("mini") || model.Contains("nano");
            int estimatedOutputTokens = isCompactModel ? 2048 : 4096;
            estimatedCost = CalculateCost(model, estimatedInputTokens, estimatedOutputTokens);

            if (!await ReserveQuotaAsync(tenantId, estimatedCost))
            {
                return new AIGenerationResult { Success = false, Error = "AI quota exceeded for this billing period" };
            }

            // --- AI RESPONSE CACHING ---
            var promptHash = ComputeSha256Hash(prompt);
            var cacheKey = $"ai:cache:{tenantId}:{model}:{promptHash}";

            var cachedResponse = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResponse))
            {
                _logger.LogInformation("Returning cached AI response for tenant {TenantId}, model {Model}", tenantId, model);

                var cachedResult = JsonSerializer.Deserialize<AIGenerationResult>(cachedResponse);
                if (cachedResult != null)
                {
                    var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    await LogUsageAsync(tenantId, userId, model + " (cached)", "text-generation",
                        cachedResult.InputTokens, cachedResult.OutputTokens, 0m, latency, true, null, estimatedCost);

                    cachedResult.Cost = 0m;
                    return cachedResult;
                }
            }

            // Call Azure OpenAI API with Fallback Logic
            AIGenerationResult? result = null;
            try
            {
                result = await ExecuteApiCallAsync(tenantId, prompt, model);
            }
            // Degrade to the economy deployment when the richer model fails. The guard used to
            // be model.StartsWith("gpt-4") falling back to gpt-3.5-turbo — after the move to the
            // gpt-5 family that condition never matched, so the fallback was dead code, and
            // gpt-3.5-turbo has no deployment in the Azure resource to fall back TO.
            catch (Exception ex) when (model != FallbackModel)
            {
                _logger.LogWarning(ex, "API call failed for {Model}. Falling back to {Fallback}.", model, FallbackModel);
                model = FallbackModel;
                isFallback = true;
                result = await ExecuteApiCallAsync(tenantId, prompt, model);
            }

            if (result == null || !result.Success)
            {
                throw new Exception(result?.Error ?? "Unknown API error");
            }

            // Save to cache on success
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), cacheOptions);

            var latencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var loggedModel = isFallback ? $"{originalModel} (fallback to {model})" : model;

            // --- CONFIDENCE HEURISTIC ---
            // Simple heuristic based on content analysis for uncertainty
            var uncertaintyMarkers = new[] { "i am not sure", "i don't have enough information", "as an ai", "it is uncertain", "potentially", "maybe" };
            double confidence = 95.0; // Assume high, then deduct
            foreach (var marker in uncertaintyMarkers)
            {
                if (result.Content != null && result.Content.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    confidence -= 20.0;
            }
            result.ConfidenceScore = Math.Max(0, confidence);

            if (result.ConfidenceScore < 70)
            {
                result.RequiresApproval = true;
                await _notificationService.EscalateAsync(tenantId, "AI",
                    $"Low confidence ({result.ConfidenceScore:F0}/100) on text-generation", "Medium",
                    new { Model = model, Content = result.Content?.Substring(0, Math.Min(50, result.Content.Length)) + "..." },
                    true);
            }

            await LogUsageAsync(tenantId, userId, loggedModel, "text-generation",
                result.InputTokens, result.OutputTokens, result.Cost, latencyMs, true, null, estimatedCost);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI text generation failed for tenant {TenantId}", tenantId);
            var loggedModel = isFallback ? $"{originalModel} (fallback failed)" : model;
            await LogUsageAsync(tenantId, userId, loggedModel, "text-generation", 0, 0, 0, 0, false, ex.Message, estimatedCost);

            return new AIGenerationResult { Success = false, Error = ex.Message };
        }
    }

    public async IAsyncEnumerable<string> GenerateTextStreamAsync(Guid tenantId, Guid? userId, string prompt, string? model = null)
    {
        model = string.IsNullOrWhiteSpace(model)
            ? await _modelResolver.ResolveAsync(tenantId)
            : model;
        decimal estimatedCost = 0m;
        if (!await IsModelAllowedAsync(tenantId, model))
        {
            yield return $"Error: AI Model '{model}' is not allowed for your subscription.";
            yield break;
        }

        int estimatedInputTokens = prompt.Length / 4;
        int estimatedOutputTokens = model.Contains("gpt-4") ? 4096 : 2048;
        estimatedCost = CalculateCost(model, estimatedInputTokens, estimatedOutputTokens);

        if (!await ReserveQuotaAsync(tenantId, estimatedCost))
        {
            yield return "Daily quota exceeded";
            yield break;
        }

        var endpoint = _secretProvider.GetSecret("AzureOpenAI:Endpoint") ?? _configuration["AzureOpenAI:Endpoint"];
        var apiKey = _secretProvider.GetSecret("AzureOpenAI:ApiKey") ?? _configuration["AzureOpenAI:ApiKey"];
        var deploymentName = _configuration[$"AzureOpenAI:Deployments:{model}"] ?? model;

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            await LogUsageAsync(tenantId, userId, model, "text-generation-stream", 0, 0, 0, 0, false, "Configuration missing", estimatedCost);
            yield return "Error: AI service is not configured.";
            yield break;
        }

        HttpResponseMessage? response = null;
        string? initError = null;
        var streamCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(120));
        try
        {
            // Scrub PII from prompt before sending
            var safePrompt = _piiScrubber.Scrub(prompt);

            // Shares BuildChatRequest with the non-streaming path so a model-compatibility rule can
            // never again be fixed in one builder and missed in the other — which is exactly what
            // happened here: max_tokens was corrected in both, temperature in neither.
            var requestBody = BuildChatRequest(safePrompt, model, maxCompletionTokens: 2000, stream: true);

            var apiUrl = $"{endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version=2024-02-01";
            // Per-request header avoids thread-unsafe mutation of DefaultRequestHeaders on shared HttpClient.
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Add("api-key", apiKey);
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, streamCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP streaming call failed for model {Model}", model);
            initError = $"Error: {ex.Message}";
        }

        if (initError != null)
        {
            await LogUsageAsync(tenantId, userId, model, "text-generation-stream", 0, 0, 0, 0, false, initError, estimatedCost);
            yield return initError;
            yield break;
        }

        if (response == null || !response.IsSuccessStatusCode)
        {
            var errorBody = response != null ? await response.Content.ReadAsStringAsync() : "Unknown connection error";
            _logger.LogError(
                "Azure OpenAI streaming HTTP error {StatusCode} for tenant {TenantId} model {Model}: {ErrorBody}",
                response != null ? (int)response.StatusCode : 0, tenantId, model, errorBody);
            response?.Dispose();
            await LogUsageAsync(tenantId, userId, model, "text-generation-stream", 0, 0, 0, 0, false, errorBody, estimatedCost);
            yield return $"Error: {errorBody}";
            yield break;
        }

        var fullContent = new StringBuilder();
        System.IO.Stream? stream = null;
        try
        {
            stream = await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stream from response content");
            initError = $"Error: {ex.Message}";
        }

        if (initError != null)
        {
            response.Dispose();
            yield return initError;
            yield break;
        }

        try
        {
            using (var reader = new System.IO.StreamReader(stream!))
            {
                while (!reader.EndOfStream)
                {
                    string? line = null;
                    try
                    {
                        line = await reader.ReadLineAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading line from stream");
                        break;
                    }

                    if (line == null) continue;

                    if (line.StartsWith("data: "))
                    {
                        var data = line["data: ".Length..].Trim();
                        if (data == "[DONE]") break;

                        string? content = null;
                        try
                        {
                            using var doc = JsonDocument.Parse(data);
                            if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                            {
                                var choice = choices[0];
                                if (choice.TryGetProperty("delta", out var delta)
                                    && delta.TryGetProperty("content", out var contentProp)
                                    && contentProp.ValueKind != JsonValueKind.Null)
                                {
                                    content = contentProp.GetString();
                                }
                            }
                        }
                        catch
                        {
                            // Ignore JSON parsing errors for malformed lines
                        }

                        if (!string.IsNullOrEmpty(content))
                        {
                            fullContent.Append(content);
                            yield return content;
                        }
                    }
                }
            }
        }
        finally
        {
            if (stream != null) await stream.DisposeAsync();
            response.Dispose();
        }

        // Log usage at the end
        try
        {
            int inputTokens = Math.Max(1, prompt.Length / 4);
            int outputTokens = Math.Max(0, fullContent.Length / 4);
            var cost = CalculateCost(model, inputTokens, outputTokens);
            await LogUsageAsync(tenantId, userId, model, "text-generation-stream", inputTokens, outputTokens, cost, 0, true, null, estimatedCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log streaming usage in AzureOpenAIService");
        }
    }

    /// <summary>
    /// Whether an explicit temperature may be sent for this model.
    ///
    /// The gpt-5 family accepts ONLY the default (1) and rejects any explicit value outright:
    ///   "Unsupported value: 'temperature' does not support 0.7 with this model.
    ///    Only the default (1) value is supported."  (HTTP 400, code unsupported_value)
    ///
    /// Verified against the live gpt-5-mini deployment. Both request builders previously hardcoded
    /// temperature = 0.7, and AiModelResolver returns nothing BUT gpt-5 models — so every text
    /// generation in the product failed with a 400 before reaching the model. Mocked tests could
    /// not catch it: the payload is only rejected by the real endpoint.
    ///
    /// Kept as a predicate rather than deleting temperature outright because gpt-4 and
    /// gpt-3.5-turbo are still in IsModelAllowedAsync's default list and do honour it.
    /// </summary>
    private static bool SupportsCustomTemperature(string model) =>
        !model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Builds the chat-completions payload, including temperature only where the model allows it.
    /// A dictionary rather than an anonymous type so the key can be omitted entirely — sending
    /// null would be rejected just the same.
    /// </summary>
    private static Dictionary<string, object> BuildChatRequest(
        string prompt, string model, int maxCompletionTokens, bool stream)
    {
        var body = new Dictionary<string, object>
        {
            ["messages"] = new[] { new { role = "user", content = prompt } },
            // The gpt-5 family also rejects max_tokens; max_completion_tokens is the replacement.
            ["max_completion_tokens"] = maxCompletionTokens,
        };

        if (SupportsCustomTemperature(model)) body["temperature"] = 0.7;
        if (stream) body["stream"] = true;

        return body;
    }

    private async Task<AIGenerationResult> ExecuteApiCallAsync(Guid tenantId, string prompt, string model)
    {
        var endpoint = _secretProvider.GetSecret("AzureOpenAI:Endpoint") ?? _configuration["AzureOpenAI:Endpoint"];
        var apiKey = _secretProvider.GetSecret("AzureOpenAI:ApiKey") ?? _configuration["AzureOpenAI:ApiKey"];
        var deploymentName = _configuration[$"AzureOpenAI:Deployments:{model}"] ?? model;

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("AI service is not configured. Please set AzureOpenAI:Endpoint and AzureOpenAI:ApiKey in configuration.");
        }

        // Scrub PII from prompt before sending
        var safePrompt = _piiScrubber.Scrub(prompt);

        // Model-aware token limit
        var maxTokens = model.Contains("gpt-4") || model.Contains("sonnet") ? 4096 : 2000;
        var requestBody = BuildChatRequest(safePrompt, model, maxTokens, stream: false);

        var apiUrl = $"{endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version=2024-02-01";
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(90));
        // Per-request header avoids thread-unsafe mutation of DefaultRequestHeaders on shared HttpClient.
        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(requestBody);
        var response = await _httpClient.SendAsync(request, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Azure OpenAI text generation HTTP error {StatusCode} for tenant {TenantId} model {Model}: {ErrorBody}",
                (int)response.StatusCode, tenantId, model, errorBody);
            throw new Exception($"AI service returned {response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<AzureOpenAIResponse>();
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        var inputTokens = result?.Usage?.PromptTokens ?? (prompt.Length / 4);
        var outputTokens = result?.Usage?.CompletionTokens ?? (content.Length / 4);

        var cost = CalculateCost(model, inputTokens, outputTokens);

        return new AIGenerationResult
        {
            Success = true,
            Content = content,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Cost = cost
        };
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    public async Task<AIGenerationResult> GenerateImageAsync(Guid tenantId, Guid? userId, string prompt)
    {
        decimal estimatedCost = 0m;
        try
        {
            // Check if model is allowed
            if (!await IsModelAllowedAsync(tenantId, "dall-e-3"))
            {
                return new AIGenerationResult
                {
                    Success = false,
                    Error = "Image generation (DALL-E 3) is not allowed for your subscription."
                };
            }

            estimatedCost = 0.04m; // DALL-E 3 per image
            if (!await ReserveQuotaAsync(tenantId, estimatedCost))
            {
                return new AIGenerationResult
                {
                    Success = false,
                    Error = "AI quota exceeded for this billing period"
                };
            }

            var endpoint = _secretProvider.GetSecret("AzureOpenAI:Endpoint") ?? _configuration["AzureOpenAI:Endpoint"];
            var apiKey = _secretProvider.GetSecret("AzureOpenAI:ApiKey") ?? _configuration["AzureOpenAI:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                await LogUsageAsync(tenantId, userId, "dall-e-3", "image-generation", 0, 0, 0, 0, false, "Configuration missing", estimatedCost);
                return new AIGenerationResult
                {
                    Success = false,
                    Error = "AI service is not configured. Please set AzureOpenAI:Endpoint and AzureOpenAI:ApiKey."
                };
            }

            var requestBody = new
            {
                prompt = prompt,
                n = 1,
                size = "1024x1024"
            };

            var apiUrl = $"{endpoint.TrimEnd('/')}/openai/deployments/dall-e-3/images/generations?api-version=2024-02-01";
            using var imageCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60));
            // Per-request header avoids thread-unsafe mutation of DefaultRequestHeaders on shared HttpClient.
            var imageRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            imageRequest.Headers.Add("api-key", apiKey);
            imageRequest.Content = JsonContent.Create(requestBody);
            var response = await _httpClient.SendAsync(imageRequest, imageCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Azure OpenAI DALL-E error: {StatusCode} {Error}", response.StatusCode, errorBody);
                await LogUsageAsync(tenantId, userId, "dall-e-3", "image-generation", 0, 0, 0, 0, false, errorBody, estimatedCost);
                return new AIGenerationResult
                {
                    Success = false,
                    Error = $"Image generation failed: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<DalleResponse>();
            var imageUrl = result?.Data?.FirstOrDefault()?.Url ?? "";

            var cost = 0.04m; // DALL-E 3 per image
            await LogUsageAsync(tenantId, userId, "dall-e-3", "image-generation", 0, 0, cost, 0, true, null, estimatedCost);

            return new AIGenerationResult
            {
                Success = true,
                ImageUrl = imageUrl,
                Cost = cost
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI image generation failed for tenant {TenantId}", tenantId);
            await LogUsageAsync(tenantId, userId, "dall-e-3", "image-generation", 0, 0, 0, 0, false, ex.Message, estimatedCost);
            return new AIGenerationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<AIGenerationResult> AnalyzeSentimentAsync(Guid tenantId, Guid? userId, string content)
    {
        var prompt = $"Analyze the sentiment of the following content: '{content}'. Provide a score from -1 (Extremely Negative) to 1 (Extremely Positive) and a brief explanation. Format as JSON: {{ \"score\": 0.5, \"explanation\": \"...\" }}";
        // Sentiment analysis uses the cheapest model — no need for Sonnet-level quality
        var result = await GenerateTextAsync(tenantId, userId, prompt, _modelResolver.ResolveForTier("Starter"));
        return result;
    }

    public async Task<AIGenerationResult> GenerateDiscoveryReportAsync(Guid tenantId, string businessType, string niche)
    {
        var prompt = $"Generate a market discovery report for a {businessType} in the {niche} niche. " +
                     "Include competitor analysis, market gaps, and suggested marketing strategies. Format as Markdown.";
        var result = await GenerateTextAsync(tenantId, null, prompt);
        return result;
    }

    public async Task<AIUsageStats> GetUsageStatsAsync(Guid tenantId, DateTime? from = null, DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddMonths(-1);
        to ??= DateTime.UtcNow;

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var query = dbContext.Set<AIUsageLog>()
                .Where(l => l.TenantId == tenantId && l.CreatedAt >= from && l.CreatedAt <= to);

            var totalStats = await query
                .GroupBy(l => 1)
                .Select(g => new
                {
                    TotalRequests = g.Count(),
                    TotalInputTokens = g.Sum(l => l.InputTokens),
                    TotalOutputTokens = g.Sum(l => l.OutputTokens),
                    TotalCost = g.Sum(l => l.Cost)
                })
                .FirstOrDefaultAsync();

            var requestsByFeature = await query
                .GroupBy(l => l.Feature)
                .Select(g => new { Feature = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Feature, x => x.Count);

            var costByModel = await query
                .GroupBy(l => l.Model)
                .Select(g => new { Model = g.Key, Cost = g.Sum(l => l.Cost) })
                .ToDictionaryAsync(x => x.Model, x => x.Cost);

            return new AIUsageStats
            {
                TotalRequests = totalStats?.TotalRequests ?? 0,
                TotalInputTokens = totalStats?.TotalInputTokens ?? 0,
                TotalOutputTokens = totalStats?.TotalOutputTokens ?? 0,
                TotalCost = totalStats?.TotalCost ?? 0m,
                RequestsByFeature = requestsByFeature ?? new Dictionary<string, int>(),
                CostByModel = costByModel ?? new Dictionary<string, decimal>()
            };
        }
    }

    /// <summary>
    /// Monthly spend ceiling for Upkilo's own platform assistant, in USD. Configurable so the cap
    /// can be raised without a deploy; the default is deliberately modest because the endpoint it
    /// protects is anonymous.
    /// </summary>
    private decimal PlatformMonthlyBudget =>
        _configuration.GetValue<decimal?>("Ai:PlatformMonthlyBudget") ?? 25.00m;

    /// <summary>
    /// Reserve-then-release against a calendar-month Redis counter, mirroring the per-tenant path.
    /// Keyed by month rather than by billing period because there is no subscription to take a
    /// period from.
    /// </summary>
    private async Task<bool> ReservePlatformQuotaAsync(decimal estimatedCost)
    {
        var budget = PlatformMonthlyBudget;
        if (budget <= 0m) return false;

        var redisKey = $"ai:usage:platform:{DateTime.UtcNow:yyyy-MM}";
        var redisDb = _redis.GetDatabase();

        var newUsage = (decimal)await redisDb.StringIncrementAsync(redisKey, (double)estimatedCost);

        if (newUsage > budget)
        {
            // Give the reservation back, exactly as the tenant path does, so a rejected turn does
            // not permanently consume budget it never spent.
            await redisDb.StringIncrementAsync(redisKey, -(double)estimatedCost);
            _logger.LogWarning(
                "Platform support AI budget exhausted: {Usage} of {Budget} this month.", newUsage, budget);
            return false;
        }

        return true;
    }

    public async Task<bool> CheckQuotaAsync(Guid tenantId)
    {
        // Same reasoning as ReserveQuotaAsync: the platform assistant has no subscription, so the
        // generic path would report "no quota" for an identity that is in fact within budget.
        if (tenantId == UpkiloPlatform.TenantId)
        {
            var redisKey = $"ai:usage:platform:{DateTime.UtcNow:yyyy-MM}";
            var usageValue = await _redis.GetDatabase().StringGetAsync(redisKey);
            var used = usageValue.HasValue && decimal.TryParse(usageValue.ToString(), out var u) ? u : 0m;
            return used < PlatformMonthlyBudget;
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var subscription = await dbContext.Set<Subscription>()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (subscription == null) return false;

            decimal budget = subscription.AiMonthlyBudget;

            // All AI usage MUST have a strict budget map. Negative or zero budgets mean no access.
            if (budget <= 0m) return false;

            // Use Redis atomic counter for real-time quota enforcement
            var billingPeriodStr = subscription.CurrentPeriodStart.ToString("yyyy-MM-dd");
            var redisKey = $"ai:usage:{tenantId}:{billingPeriodStr}";

            var redisDb = _redis.GetDatabase();
            var currentUsageString = await redisDb.StringGetAsync(redisKey);
            decimal currentUsage = 0m;

            if (currentUsageString.HasValue && decimal.TryParse(currentUsageString.ToString(), out var parsedUsage))
            {
                currentUsage = parsedUsage;
            }

            // --- ESCALATE ON 90% BUDGET ---
            if (currentUsage > budget * 0.9m)
            {
                await _notificationService.EscalateAsync(tenantId, "Billing",
                    $"AI Budget reach {(currentUsage / budget) * 100:F0}% ({currentUsage}/{budget})",
                    "Medium", null, false);
            }

            return currentUsage < budget;
        }
    }

    private async Task<bool> ReserveQuotaAsync(Guid tenantId, decimal estimatedCost)
    {
        // Upkilo's own marketing-site assistant has no Subscription row and never will - the
        // visitor using it is not a customer yet. The generic path below rejects a missing
        // subscription outright, so without this branch the public support bot fails EVERY
        // request with "AI quota exceeded" and only ever emits its fallback message.
        //
        // It is still capped, just against a configured ceiling rather than a plan, because this
        // endpoint is anonymous and internet-reachable: once the month's spend is gone the bot
        // stops answering instead of billing Upkilo indefinitely.
        if (tenantId == UpkiloPlatform.TenantId)
            return await ReservePlatformQuotaAsync(estimatedCost);

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var subscription = await dbContext.Set<Subscription>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (subscription == null) return false;

            decimal budget = subscription.AiMonthlyBudget;

            if (budget <= 0m) return false;

            var billingPeriodStr = subscription.CurrentPeriodStart.ToString("yyyy-MM-dd");
            var redisKey = $"ai:usage:{tenantId}:{billingPeriodStr}";

            var redisDb = _redis.GetDatabase();

            var newUsage = await redisDb.StringIncrementAsync(redisKey, (double)estimatedCost);

            if ((decimal)newUsage > budget)
            {
                await redisDb.StringIncrementAsync(redisKey, -(double)estimatedCost);
                return false;
            }

            if ((decimal)newUsage > budget * 0.9m)
            {
                await _notificationService.EscalateAsync(tenantId, "Billing",
                    $"AI Budget reach {((decimal)newUsage / budget) * 100:F0}% ({newUsage}/{budget})",
                    "Medium", null, false);
            }

            return true;
        }
    }

    public async Task<bool> CheckSafetyAsync(string content)
    {
        if (string.IsNullOrEmpty(content)) return true;
        var result = await _contentModerationService.ModerateTextAsync(content);
        return result.IsAllowed;
    }

    public async Task<bool> IsModelAllowedAsync(Guid tenantId, string model)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var subscription = await dbContext.Set<Subscription>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            // A missing subscription row must NOT decide whether AI is allowed. Whether the
            // tenant may use AI at all is settled upstream by the entitlement engine and the
            // [RequiresFeature]/[FeatureGuard] gate on the controller; this method's only job
            // is to say WHICH MODEL may be dispatched.
            //
            // Denying here contradicted that engine. A TenantFeatureOverride deliberately
            // outranks the subscription lifecycle (EntitlementService, "Deliberately outranks
            // the lifecycle gate"), so an admin granting ai_copilot to a tenant with no
            // subscription row passed the controller gate and was then refused here as
            // "model not allowed" — a second, uncoordinated gate overruling the central one,
            // with a message that named the wrong cause.
            //
            // With no subscription there is no AllowedAiModels list, so the defaults apply,
            // which is the same answer a subscription that lists nothing already gets.

            // If no specifically allowed models are listed, allow all Upkilo-tier models
            if (subscription?.AllowedAiModels == null || !subscription.AllowedAiModels.Any())
            {
                // Must stay in sync with AiModelResolver — a model it returns but this list
                // omits is rejected here before dispatch. gpt-4o was missing while the
                // resolver named Claude models, so nothing surfaced it.
                var defaults = new[]
                {
                    "gpt-5.4-mini",
                    "gpt-5-mini",
                    "gpt-4",
                    "gpt-3.5-turbo"
                };
                return defaults.Contains(model.ToLower());
            }

            return subscription.AllowedAiModels.Any(m => m.Equals(model, StringComparison.OrdinalIgnoreCase));
        }
    }

    private async Task LogUsageAsync(Guid tenantId, Guid? userId, string model, string feature,
        int inputTokens, int outputTokens, decimal cost, int latencyMs, bool success, string? error = null, decimal estimatedCost = 0m)
    {
        var log = new AIUsageLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Model = model,
            Feature = feature,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Cost = cost,
            LatencyMs = latencyMs,
            Success = success,
            ErrorMessage = error
        };

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Set<AIUsageLog>().Add(log);

            // The platform assistant reconciles against its own month-keyed counter. Without this
            // it would keep the ESTIMATE forever - and the estimate assumes a full-length
            // completion - so the budget would bite several times earlier than the configured
            // ceiling actually says.
            if (tenantId == UpkiloPlatform.TenantId)
            {
                var platformKey = $"ai:usage:platform:{DateTime.UtcNow:yyyy-MM}";
                var platformDb = _redis.GetDatabase();

                var platformAdjustment = cost - estimatedCost;
                if (platformAdjustment != 0)
                    await platformDb.StringIncrementAsync(platformKey, (double)platformAdjustment);

                await platformDb.KeyExpireAsync(platformKey, TimeSpan.FromDays(32));
                await dbContext.SaveChangesAsync();
                return;
            }

            // Update Redis usage counter atomically
            var subscription = await dbContext.Set<Subscription>().AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            // Metered AI overage to Stripe.
            //
            // This existed only in AiService, which is registered nowhere, so it had never once
            // run - PricingPlan.StripeAiUsagePriceId was configurable by admins and read by
            // nothing. Note what it is and is not: the protection against a tenant running up
            // Upkilo's Azure bill is ReserveQuotaAsync, which already blocks them at
            // AiMonthlyBudget on every call. This reports the spend so it can be BILLED rather
            // than merely capped.
            //
            // Off unless Billing:ReportAiUsage is explicitly true, because switching it on starts
            // charging real money to every tenant whose plan has an AI usage price configured.
            // Only successful, non-zero, non-cached usage is reported.
            if (success && cost > 0 && _configuration.GetValue<bool>("Billing:ReportAiUsage"))
                await ReportUsageToStripeAsync(scope, dbContext, tenantId, cost);

            if (subscription != null && cost > 0)
            {
                var billingPeriodStr = subscription.CurrentPeriodStart.ToString("yyyy-MM-dd");
                var redisKey = $"ai:usage:{tenantId}:{billingPeriodStr}";
                var redisDb = _redis.GetDatabase();

                var adjustment = cost - estimatedCost;
                if (adjustment != 0)
                {
                    await redisDb.StringIncrementAsync(redisKey, (double)adjustment);
                }

                // Set expiry to 32 days if not set
                await redisDb.KeyExpireAsync(redisKey, TimeSpan.FromDays(32));
            }

            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Reports one turn's AI cost to Stripe as metered usage, when the tenant's plan carries an
    /// AI usage price. Ported from the unregistered AiService, which is the only place it ever
    /// existed - see the call site for why it is gated.
    ///
    /// ISubscriptionService is resolved from the scope rather than injected, matching how this
    /// class already takes its DbContext. A constructor dependency would also change every
    /// existing construction of this service for a path most calls never take.
    ///
    /// Failures here are logged and swallowed: a billing-reporting outage must not turn a
    /// successful AI answer into an error for the user. The AIUsageLog row is still written, so
    /// the usage is recoverable and can be re-reported.
    /// </summary>
    private async Task ReportUsageToStripeAsync(
        IServiceScope scope, AppDbContext dbContext, Guid tenantId, decimal cost)
    {
        try
        {
            var aiPriceId = await dbContext.Set<Subscription>()
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
                .Select(s => s.PricingPlan!.StripeAiUsagePriceId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(aiPriceId)) return;

            // Stripe meters integer quantities, so cost is reported in cents. Sub-cent turns
            // round to zero and are skipped rather than billed as one cent each.
            var cents = (long)(cost * 100);
            if (cents <= 0) return;

            var subscriptions = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            await subscriptions.ReportUsageAsync(tenantId, aiPriceId, cents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to report AI usage to Stripe for tenant {TenantId} ({Cost} USD). "
                + "The AIUsageLog row is still written.", tenantId, cost);
        }
    }

    private decimal CalculateCost(string model, int inputTokens, int outputTokens)
    {
        // Input and output tokens have DIFFERENT per-token prices — never combine them.
        // Configuration keys: AzureOpenAI:PricingInput:{model} and AzureOpenAI:PricingOutput:{model}
        // Defaults are GPT-4 Turbo pricing per token (not per 1K).
        var inputRateStr = _configuration[$"AzureOpenAI:PricingInput:{model}"];
        var outputRateStr = _configuration[$"AzureOpenAI:PricingOutput:{model}"];

        decimal inputRate = decimal.TryParse(inputRateStr, out var ir) ? ir : 0.000001m;  // $1.00/1M input
        decimal outputRate = decimal.TryParse(outputRateStr, out var or) ? or : 0.000003m;  // $3.00/1M output

        return (inputTokens * inputRate) + (outputTokens * outputRate);
    }
}

// Azure OpenAI response DTOs
internal class AzureOpenAIResponse
{
    [JsonPropertyName("choices")]
    public List<AzureOpenAIChoice>? Choices { get; set; }
    [JsonPropertyName("usage")]
    public AzureOpenAIUsage? Usage { get; set; }
}

internal class AzureOpenAIChoice
{
    [JsonPropertyName("message")]
    public AzureOpenAIMessage? Message { get; set; }
}

internal class AzureOpenAIMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

internal class AzureOpenAIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}

internal class DalleResponse
{
    [JsonPropertyName("data")]
    public List<DalleImage>? Data { get; set; }
}

internal class DalleImage
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
