using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.ClientModel;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for interacting with Azure OpenAI and tracking AI usage.
/// Model selection is tier-driven via IAiModelResolver — never hardcode a model at call sites.
/// </summary>
public class AiService : IAIService
{
    private readonly AzureOpenAIClient _client;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiService> _logger;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IContentModerationService _contentModerationService;
    private readonly IAiModelResolver _modelResolver;
    // The per-tenant SemaphoreSlim dictionary that used to live here was removed with the move to
    // AiQuotaGate. It guarded quota reservation within a single process only, and its entries —
    // undisposed SemaphoreSlim instances keyed by tenant — were never evicted.

    public AiService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<AiService> logger,
        ISubscriptionService subscriptionService,
        IContentModerationService contentModerationService,
        IAiModelResolver modelResolver)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _subscriptionService = subscriptionService;
        _contentModerationService = contentModerationService;
        _modelResolver = modelResolver;

        var endpoint = _configuration["Azure:OpenAI:Endpoint"];
        var key = _configuration["Azure:OpenAI:Key"];

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
        {
            _client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
        }
        else
        {
            _logger.LogWarning("Azure OpenAI endpoint or key not configured. AI features will be unavailable.");
            _client = null!;
        }
    }

    // Resolves the model: caller-supplied override wins, otherwise resolves from tenant tier.
    private async Task<string> GetModelAsync(Guid tenantId, string? callerModel)
        => string.IsNullOrWhiteSpace(callerModel)
            ? await _modelResolver.ResolveAsync(tenantId)
            : callerModel;

    public async Task<AIGenerationResult> GenerateTextAsync(Guid tenantId, Guid? userId, string prompt, string? model = null)
    {
        if (_client == null) return new AIGenerationResult { Success = false, Error = "AI not configured" };

        var resolvedModel = await GetModelAsync(tenantId, model);

        // Quota check and reservation are serialized per tenant by a database-held advisory lock.
        // A static SemaphoreSlim was used here previously, which only serializes within one
        // process — under multiple replicas two instances could both observe a below-budget total
        // before either wrote its reservation, overspending the tenant's AI budget.
        var reservationId = await AiQuotaGate.WithTenantLockAsync(_context, tenantId, async () =>
        {
            if (!await CheckQuotaAsync(tenantId))
                return Guid.Empty;

            int estimatedMaxTokens = resolvedModel.Contains("sonnet") || resolvedModel.Contains("gpt-4") ? 4096 : 2048;
            decimal estimatedCost = CalculateCost(resolvedModel, prompt.Length / 4, estimatedMaxTokens);
            var reservation = new AIUsageLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Feature = "text-generation-reservation",
                InputTokens = prompt.Length / 4,
                OutputTokens = estimatedMaxTokens,
                Cost = estimatedCost,
                CreatedAt = DateTime.UtcNow
            };
            _context.AIUsageLogs.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation.Id;
        });

        if (reservationId == Guid.Empty)
            return new AIGenerationResult { Success = false, Error = "Daily quota exceeded" };

        try
        {
            var chatClient = _client.GetChatClient(resolvedModel);
            var response = await chatClient.CompleteChatAsync(new UserChatMessage(prompt));
            var completion = response.Value;

            var result = new AIGenerationResult
            {
                Success = true,
                Content = completion.Content[0].Text,
                InputTokens = completion.Usage.InputTokenCount,
                OutputTokens = completion.Usage.OutputTokenCount,
                Cost = CalculateCost(resolvedModel, completion.Usage.InputTokenCount, completion.Usage.OutputTokenCount)
            };

            var reservationLog = await _context.AIUsageLogs.FindAsync(reservationId);
            if (reservationLog != null)
            {
                reservationLog.InputTokens = result.InputTokens;
                reservationLog.OutputTokens = result.OutputTokens;
                reservationLog.Cost = result.Cost;
                reservationLog.Feature = "text-generation";
                await _context.SaveChangesAsync();
                
                await ReportUsageToStripeAsync(tenantId, result.Cost);
            }

            return result;
        }
        catch (Exception ex)
        {
            var reservationLog = await _context.AIUsageLogs.FindAsync(reservationId);
            if (reservationLog != null)
            {
                reservationLog.Cost = 0; // Refund on failure
                await _context.SaveChangesAsync();
            }
            _logger.LogError(ex, "AI Text Generation failed for tenant {TenantId}", tenantId);
            return new AIGenerationResult { Success = false, Error = ex.Message };
        }
    }

    public async IAsyncEnumerable<string> GenerateTextStreamAsync(Guid tenantId, Guid? userId, string prompt, string? model = null)
    {
        if (_client == null)
        {
            yield return "AI not configured";
            yield break;
        }

        var resolvedModel = await GetModelAsync(tenantId, model);

        // Same cross-replica reservation gate as GenerateTextAsync. The quota outcome is resolved
        // before anything is yielded, because a lambda cannot contain `yield return`.
        var reservationId = await AiQuotaGate.WithTenantLockAsync(_context, tenantId, async () =>
        {
            if (!await CheckQuotaAsync(tenantId))
                return Guid.Empty;

            int estimatedMaxTokens = resolvedModel.Contains("sonnet") || resolvedModel.Contains("gpt-4") ? 4096 : 2048;
            decimal estimatedCost = CalculateCost(resolvedModel, prompt.Length / 4, estimatedMaxTokens);
            var reservation = new AIUsageLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Feature = "text-generation-stream-reservation",
                InputTokens = prompt.Length / 4,
                OutputTokens = estimatedMaxTokens,
                Cost = estimatedCost,
                CreatedAt = DateTime.UtcNow
            };
            _context.AIUsageLogs.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation.Id;
        });

        if (reservationId == Guid.Empty)
        {
            yield return "Daily quota exceeded";
            yield break;
        }

        ChatClient? chatClient = null;
        string? initError = null;
        try
        {
            chatClient = _client.GetChatClient(resolvedModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ChatClient for model {Model}", model);
            initError = $"Error: {ex.Message}";
        }

        if (initError != null)
        {
            var res = await _context.AIUsageLogs.FindAsync(reservationId);
            if (res != null) { res.Cost = 0; await _context.SaveChangesAsync(); }
            yield return initError;
            yield break;
        }

        var fullContent = new System.Text.StringBuilder();

        AsyncCollectionResult<StreamingChatCompletionUpdate>? updates = null;
        string? streamError = null;
        try
        {
            updates = chatClient!.CompleteChatStreamingAsync(new UserChatMessage(prompt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Text Streaming failed for tenant {TenantId}", tenantId);
            streamError = $"Error: {ex.Message}";
        }

        if (streamError != null)
        {
            var res = await _context.AIUsageLogs.FindAsync(reservationId);
            if (res != null) { res.Cost = 0; await _context.SaveChangesAsync(); }
            yield return streamError;
            yield break;
        }

        await foreach (var update in updates!)
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    fullContent.Append(contentPart.Text);
                    yield return contentPart.Text;
                }
            }
        }

        // Update reservation with actuals
        try
        {
            int inputTokens = prompt.Length / 4;
            int outputTokens = fullContent.Length / 4;
            var cost = CalculateCost(resolvedModel, inputTokens, outputTokens);
            
            var reservationLog = await _context.AIUsageLogs.FindAsync(reservationId);
            if (reservationLog != null)
            {
                reservationLog.InputTokens = inputTokens;
                reservationLog.OutputTokens = outputTokens;
                reservationLog.Cost = cost;
                reservationLog.Feature = "text-generation-stream";
                await _context.SaveChangesAsync();
                
                await ReportUsageToStripeAsync(tenantId, cost);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log usage for streamed AI response");
        }
    }

    public async Task<AIGenerationResult> GenerateImageAsync(Guid tenantId, Guid? userId, string prompt)
    {
        if (_client == null) return new AIGenerationResult { Success = false, Error = "AI not configured" };

        try
        {
            var imageClient = _client.GetImageClient("dall-e-3");
            var options = new ImageGenerationOptions
            {
                Size = GeneratedImageSize.W1024xH1024,
                ResponseFormat = GeneratedImageFormat.Uri
            };

            var response = await imageClient.GenerateImageAsync(prompt, options);
            var imageUrl = response.Value.ImageUri.ToString();

            var result = new AIGenerationResult
            {
                Success = true,
                ImageUrl = imageUrl,
                Cost = 0.04m // Estimated DALL-E 3 cost
            };

            await LogUsageAsync(tenantId, userId, "dall-e-3", "image-generation", 0, 0, result.Cost);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Image Generation failed for tenant {TenantId}", tenantId);
            return new AIGenerationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<AIGenerationResult> AnalyzeSentimentAsync(Guid tenantId, Guid? userId, string content)
    {
        var prompt = $"Analyze the sentiment of the following text and return a score between -1.0 (very negative) and 1.0 (very positive), followed by a brief 1-sentence reason. Format: [Score] | [Reason]\n\nText: {content}";
        
        var result = await GenerateTextAsync(tenantId, userId, prompt);
        if (result.Success)
        {
            _logger.LogInformation("Sentiment analysis for tenant {TenantId} completed", tenantId);
        }
        return result;
    }

    public async Task<AIGenerationResult> GenerateDiscoveryReportAsync(Guid tenantId, string businessType, string niche)
    {
        var prompt = $"Act as an SEO and Market Discovery expert. Generate a comprehensive discovery report for a {businessType} business focusing on the {niche} niche. " +
                     "Include: 5 high-converting keywords, 3 content gaps in the current market, and 2 suggested marketing campaigns. " +
                     "Format as professional Markdown.";
        
        return await GenerateTextAsync(tenantId, null, prompt);
    }

    public async Task<AIUsageStats> GetUsageStatsAsync(Guid tenantId, DateTime? from = null, DateTime? to = null)
    {
        var start = from ?? DateTime.UtcNow.AddDays(-30);
        var end = to ?? DateTime.UtcNow;

        var logs = await _context.AIUsageLogs
            .Where(l => l.TenantId == tenantId && l.CreatedAt >= start && l.CreatedAt <= end)
            .ToListAsync();

        return new AIUsageStats
        {
            TotalRequests = logs.Count,
            TotalInputTokens = logs.Sum(l => l.InputTokens),
            TotalOutputTokens = logs.Sum(l => l.OutputTokens),
            TotalCost = logs.Sum(l => l.Cost),
            CostByModel = logs.GroupBy(l => l.Model)
                              .ToDictionary(g => g.Key!, g => g.Sum(l => l.Cost))
        };
    }

    public async Task<bool> CheckQuotaAsync(Guid tenantId)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.PricingPlan)
            .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null) return false;

        decimal monthlyLimit = subscription.AiMonthlyBudget;

        // Self-heal: rows created before AiMonthlyBudget column existed have a default of 0.
        // Apply a $5 budget for any active paid plan rather than permanently blocking AI access.
        if (monthlyLimit <= 0m && subscription.PricingPlan != null &&
            !string.Equals(subscription.PricingPlan.Name, "Free", StringComparison.OrdinalIgnoreCase))
        {
            subscription.AiMonthlyBudget = 5.00m;
            await _context.SaveChangesAsync();
            monthlyLimit = 5.00m;
        }

        if (monthlyLimit <= 0m) return false;

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var totalThisMonth = await _context.AIUsageLogs
            .Where(l => l.TenantId == tenantId && l.CreatedAt >= startOfMonth)
            .SumAsync(l => l.Cost);

        return totalThisMonth < monthlyLimit;
    }

    public async Task<bool> CheckSafetyAsync(string content)
    {
        if (string.IsNullOrEmpty(content)) return true;
        var result = await _contentModerationService.ModerateTextAsync(content);
        return result.IsAllowed;
    }

    private async Task ReportUsageToStripeAsync(Guid tenantId, decimal cost)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.PricingPlan)
            .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
            .FirstOrDefaultAsync();

        var aiPriceId = subscription?.PricingPlan?.StripeAiUsagePriceId;

        if (aiPriceId != null)
        {
            var cents = (long)(cost * 100);
            if (cents > 0)
                await _subscriptionService.ReportUsageAsync(tenantId, aiPriceId, cents);
        }
    }

    private async Task LogUsageAsync(Guid tenantId, Guid? userId, string model, string feature, int input, int output, decimal cost)
    {
        var log = new AIUsageLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Feature = feature,
            InputTokens = input,
            OutputTokens = output,
            Cost = cost,
            CreatedAt = DateTime.UtcNow
        };

        _context.AIUsageLogs.Add(log);
        await _context.SaveChangesAsync();

        await ReportUsageToStripeAsync(tenantId, cost);
    }

    private static decimal CalculateCost(string model, int input, int output)
    {
        // Claude Haiku 4.5  — $0.80 / 1M input,  $4.00 / 1M output
        // Claude Sonnet 4.6 — $3.00 / 1M input, $15.00 / 1M output
        // GPT-4             — $30.00 / 1M input, $60.00 / 1M output  (legacy)
        // GPT-3.5-turbo     — $0.50 / 1M input,  $1.50 / 1M output  (legacy)
        decimal inputRate, outputRate;

        if (model.Contains("haiku"))
        {
            inputRate  = 0.00000080m;
            outputRate = 0.00000400m;
        }
        else if (model.Contains("sonnet") || model.Contains("claude"))
        {
            inputRate  = 0.00000300m;
            outputRate = 0.00001500m;
        }
        else if (model.Contains("gpt-3.5"))
        {
            inputRate  = 0.00000050m;
            outputRate = 0.00000150m;
        }
        else
        {
            // Default: GPT-4 class
            inputRate  = 0.00003000m;
            outputRate = 0.00006000m;
        }

        return (input * inputRate) + (output * outputRate);
    }
}
