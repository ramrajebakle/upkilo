using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Attributes;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[FeatureGuard("ai_copilot")]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AIController> _logger;
    private readonly AppDbContext _context;
    private readonly IPromptSanitizer _promptSanitizer;
    private readonly JsonSchemaValidator _schemaValidator;

    private readonly INotificationService _notificationService;

    public AIController(
        IAIService aiService,
        ITenantProvider tenantProvider,
        ILogger<AIController> logger,
        AppDbContext context,
        IPromptSanitizer promptSanitizer,
        JsonSchemaValidator schemaValidator,
        INotificationService notificationService)
    {
        _aiService = aiService;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _context = context;
        _promptSanitizer = promptSanitizer;
        _schemaValidator = schemaValidator;
        _notificationService = notificationService;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");
    private Guid GetUserId() => _tenantProvider.GetUserId()
        ?? throw new UnauthorizedAccessException("User context not available");

    /// <summary>
    /// Generate text using AI
    /// </summary>
    [HttpPost("generate")]
    [RequiresFeature("AiFeatures")]
    [ChecksUsage(UsageType.AiCredits)]
    public async Task<IActionResult> GenerateText([FromBody] AIGenerateRequest request)
    {
        var tenantId = GetTenantId();
        var sanitized = _promptSanitizer.SanitizeUserInput(request.Prompt, tenantId);
        if (!sanitized.IsClean && sanitized.RiskLevel == RiskLevel.Critical)
            return BadRequest(ApiResponse.Fail("Prompt rejected: potential injection attempt detected"));

        var result = await _aiService.GenerateTextAsync(
            tenantId,
            GetUserId(),
            sanitized.SanitizedInput ?? request.Prompt,
            request.Model ?? "gpt-4"
        );

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            content = result.Content,
            usage = new
            {
                inputTokens = result.InputTokens,
                outputTokens = result.OutputTokens,
                cost = result.Cost
            }
        });
    }

    /// <summary>
    /// Generate image using AI
    /// </summary>
    [HttpPost("generate-image")]
    [RequiresFeature("AiFeatures")]
    [ChecksUsage(UsageType.AiCredits)]
    public async Task<IActionResult> GenerateImage([FromBody] AIImageRequest request)
    {
        var result = await _aiService.GenerateImageAsync(GetTenantId(), GetUserId(), request.Prompt);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            imageUrl = result.ImageUrl,
            cost = result.Cost
        });
    }

    /// <summary>
    /// Get AI usage statistics
    /// </summary>
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var stats = await _aiService.GetUsageStatsAsync(GetTenantId(), from, to);
        return Ok(stats);
    }

    /// <summary>
    /// Check AI quota
    /// </summary>
    [HttpGet("quota")]
    public async Task<IActionResult> CheckQuota()
    {
        var tenantId = GetTenantId();

        // R4: surface a clear 402 when the subscription has no AI budget configured,
        // instead of silently returning hasQuota=false with no explanation.
        var sub = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (sub?.AiMonthlyBudget <= 0)
        {
            return StatusCode(402, new
            {
                error = "ai_budget_not_configured",
                message = "Your plan does not have an AI budget configured. Please upgrade your plan or contact support to enable AI features.",
                upgradeUrl = "/billing/upgrade"
            });
        }

        var hasQuota = await _aiService.CheckQuotaAsync(tenantId);
        var stats = await _aiService.GetUsageStatsAsync(tenantId);
        var monthlyLimit = await GetAiMonthlyLimitAsync(tenantId);

        return Ok(new
        {
            hasQuota,
            currentUsage = stats.TotalCost,
            monthlyLimit,
            usagePercentage = monthlyLimit > 0 ? (stats.TotalCost / monthlyLimit) * 100 : 0
        });
    }

    private async Task<decimal> GetAiMonthlyLimitAsync(Guid tenantId)
    {
        // Get AI budget from the active subscription (set during plan setup or overridden per-tenant)
        var subscription = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null) return 0.00m; // Free tier has no AI budget

        return subscription.AiMonthlyBudget > 0 ? subscription.AiMonthlyBudget : 5.00m;
    }

    /// <summary>
    /// Generate marketing copy
    /// </summary>
    [HttpPost("copywriting")]
    [RequiresFeature("AiFeatures")]
    [ChecksUsage(UsageType.AiCredits)]
    public async Task<IActionResult> GenerateCopy([FromBody] CopywritingRequest request)
    {
        var prompt = $"Write a {request.Type} for a {request.BusinessType} business. " +
                    $"Tone: {request.Tone}. " +
                    $"Key points: {string.Join(", ", request.KeyPoints ?? Array.Empty<string>())}";

        var result = await _aiService.GenerateTextAsync(GetTenantId(), GetUserId(), prompt);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            content = result.Content,
            type = request.Type,
            cost = result.Cost
        });
    }

    /// <summary>
    /// Analyze sentiment of a text
    /// </summary>
    [HttpPost("analyze-sentiment")]
    [RequiresFeature("AiFeatures")]
    [ChecksUsage(UsageType.AiCredits)]
    public async Task<IActionResult> AnalyzeSentiment([FromBody] SentimentAnalysisRequest request)
    {
        var result = await _aiService.AnalyzeSentimentAsync(GetTenantId(), GetUserId(), request.Content);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            content = result.Content,
            cost = result.Cost
        });
    }

    /// <summary>
    /// Stream AI response using Server-Sent Events (SSE)
    /// </summary>
    [HttpPost("generate/stream")]
    [RequiresFeature("AiFeatures")]
    [ChecksUsage(UsageType.AiCredits)]
    public async Task StreamGenerateText([FromBody] AIGenerateRequest request)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        Guid tenantId;
        try { tenantId = GetTenantId(); }
        catch { Response.StatusCode = 401; return; }

        var sanitized = _promptSanitizer.SanitizeUserInput(request.Prompt, tenantId);
        if (!sanitized.IsClean && sanitized.RiskLevel == RiskLevel.Critical)
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("data: {\"error\":\"Prompt rejected: potential injection attempt\"}\n\n");
            return;
        }

        // Use the native streaming version
        var stream = _aiService.GenerateTextStreamAsync(tenantId, GetUserId(), sanitized.SanitizedInput ?? request.Prompt, request.Model ?? "gpt-4");

        var fullContent = new System.Text.StringBuilder();

        await foreach (var token in stream)
        {
            if (token.StartsWith("Error:") || token == "AI not configured" || token == "Daily quota exceeded")
            {
                await Response.WriteAsync($"data: {{\"error\":\"{token}\"}}\n\n");
                await Response.Body.FlushAsync();
                return;
            }

            fullContent.Append(token);
            var chunk = System.Text.Json.JsonSerializer.Serialize(new { delta = token });
            await Response.WriteAsync($"data: {chunk}\n\n");
            await Response.Body.FlushAsync();
        }

        // Estimate tokens and cost for the completion signal
        string model = request.Model ?? "gpt-4";
        int inputTokens = request.Prompt.Length / 4;
        int outputTokens = fullContent.Length / 4;

        decimal inputRate = 0.03m / 1000;
        decimal outputRate = 0.06m / 1000;
        if (model.Contains("gpt-3.5"))
        {
            inputRate = 0.0015m / 1000;
            outputRate = 0.002m / 1000;
        }
        decimal cost = (inputTokens * inputRate) + (outputTokens * outputRate);

        // Send completion signal
        var done = System.Text.Json.JsonSerializer.Serialize(new { done = true, usage = new { cost = cost, inputTokens = inputTokens, outputTokens = outputTokens } });
        await Response.WriteAsync($"data: {done}\n\n");
        await Response.Body.FlushAsync();
    }

    /// <summary>
    /// Get AI execution audit logs
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = GetTenantId();
        var stats = await _aiService.GetUsageStatsAsync(tenantId);

        var featureLogs = await _context.AIUsageLogs
            .Where(l => l.TenantId == tenantId)
            .GroupBy(l => l.Feature)
            .Select(g => new
            {
                Feature = g.Key,
                RequestCount = g.Count(),
                TotalCost = g.Sum(l => l.Cost),
                LastUsed = g.Max(l => l.CreatedAt)
            })
            .ToListAsync();

        var logs = featureLogs.Select(l => (object)new
        {
            feature = l.Feature,
            requestCount = l.RequestCount,
            cost = l.TotalCost,
            lastUsed = l.LastUsed.ToString("o"),
            status = "success"
        }).ToList();

        return Ok(new { logs, total = logs.Count, totalRequests = stats.TotalRequests, totalCost = stats.TotalCost });
    }

    /// <summary>
    /// Get recent discovery reports for the tenant
    /// </summary>
    [HttpGet("discovery-reports")]
    public async Task<IActionResult> GetDiscoveryReports()
    {
        var reports = await _context.AIDiscoveryReports
            .Where(r => r.TenantId == GetTenantId())
            .OrderByDescending(r => r.GeneratedAt)
            .Take(10)
            .ToListAsync();

        return Ok(reports);
    }

    /// <summary>
    /// Get a specific discovery report
    /// </summary>
    [HttpGet("discovery-reports/{id}")]
    public async Task<IActionResult> GetDiscoveryReport(Guid id)
    {
        var report = await _context.AIDiscoveryReports
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == GetTenantId());

        if (report == null) return NotFound();

        return Ok(report);
    }

    /// <summary>
    /// Generate text with confidence scoring — auto-queues low-confidence results
    /// </summary>
    [HttpPost("generate/scored")]
    [RequiresFeature("AiFeatures")]
    [ChecksUsage(UsageType.AiCredits)]
    public async Task<IActionResult> GenerateWithScoring([FromBody] AIGenerateRequest request, [FromQuery] double threshold = 70.0)
    {
        var tenantId = GetTenantId();
        var sanitized = _promptSanitizer.SanitizeUserInput(request.Prompt, tenantId);
        if (!sanitized.IsClean && sanitized.RiskLevel == RiskLevel.Critical)
            return BadRequest(ApiResponse.Fail("Prompt rejected: potential injection attempt detected"));

        var result = await _aiService.GenerateTextAsync(tenantId, GetUserId(), sanitized.SanitizedInput ?? request.Prompt, request.Model ?? "gpt-4");

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        // Confidence scoring heuristic: based on output length, completeness, and token ratio
        var content = result.Content ?? string.Empty;
        var score = ComputeConfidenceScore(content, request.Prompt, result.InputTokens, result.OutputTokens);
        var requiresApproval = score < threshold;

        if (requiresApproval)
        {
            var severity = score < 40 ? "High" : "Medium";
            var reason = score < 40 ? "Very low confidence — output too short or incomplete"
                                    : "Below confidence threshold — review recommended";

            await _notificationService.EscalateAsync(
                tenantId,
                "AI",
                reason,
                severity,
                new { Prompt = request.Prompt, Content = content, Score = score },
                true);
        }

        return Ok(new
        {
            content = content,
            confidenceScore = score,
            requiresApproval = requiresApproval,
            threshold = threshold,
            queuedForReview = requiresApproval,
            usage = new { inputTokens = result.InputTokens, outputTokens = result.OutputTokens, cost = result.Cost }
        });
    }

    // Approval endpoints removed — now handled by EscalationsController

    /// <summary>
    /// POST /api/v1/ai/generate/structured — generate JSON-structured AI output with schema validation.
    /// Instructs the AI to return valid JSON, then validates it matches the declared schema.
    /// </summary>
    [HttpPost("generate/structured")]
    [RequiresFeature("AiFeatures")]
    [ChecksUsage(UsageType.AiCredits)]
    public async Task<IActionResult> GenerateStructured([FromBody] StructuredAIRequest request)
    {
        var tenantId = GetTenantId();
        var sanitized = _promptSanitizer.SanitizeUserInput(request.Prompt, tenantId);
        if (!sanitized.IsClean && sanitized.RiskLevel == RiskLevel.Critical)
            return BadRequest(ApiResponse.Fail("Prompt rejected: potential injection attempt detected"));

        // Wrap the prompt to enforce JSON output
        var structuredPrompt = string.IsNullOrEmpty(request.JsonSchema)
            ? $"Respond only with valid JSON. No markdown, no explanation.\n\n{sanitized.SanitizedInput}"
            : $"Respond only with valid JSON matching this schema:\n{request.JsonSchema}\n\nTask: {sanitized.SanitizedInput}";

        var result = await _aiService.GenerateTextAsync(tenantId, GetUserId(), structuredPrompt, request.Model ?? "gpt-4");
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error ?? "AI generation failed"));

        var content = result.Content?.Trim() ?? string.Empty;

        // Strip markdown fences if model wraps output
        if (content.StartsWith("```json")) content = content[7..];
        else if (content.StartsWith("```")) content = content[3..];
        if (content.EndsWith("```")) content = content[..^3];
        content = content.Trim();

        // Validate JSON is parseable
        System.Text.Json.JsonDocument? parsed = null;
        try { parsed = System.Text.Json.JsonDocument.Parse(content); }
        catch
        {
            _logger.LogWarning("AI returned non-JSON content for structured request. Tenant: {TenantId}", tenantId);
            return UnprocessableEntity(ApiResponse.Fail("AI did not return valid JSON. Try rephrasing your prompt."));
        }

        // Optionally validate against named schema
        ValidationResult? validation = null;
        if (!string.IsNullOrEmpty(request.SchemaName))
        {
            validation = _schemaValidator.ValidateJson(request.SchemaName, content);
            if (!validation.IsValid)
                return UnprocessableEntity(ApiResponse.Fail($"JSON schema validation failed: {string.Join(", ", validation.Warnings)}"));
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            json = parsed?.RootElement,
            raw = content,
            schemaWarnings = validation?.Warnings,
            usage = new { inputTokens = result.InputTokens, outputTokens = result.OutputTokens, cost = result.Cost },
        }));
    }

    private static double ComputeConfidenceScore(string content, string prompt, int inputTokens, int outputTokens)
    {
        if (string.IsNullOrWhiteSpace(content)) return 0;

        double score = 50; // baseline

        // Reward longer, more complete responses
        var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount >= 100) score += 20;
        else if (wordCount >= 50) score += 15;
        else if (wordCount >= 20) score += 8;
        else if (wordCount < 5) score -= 25;

        // Token output ratio (more output relative to input = more confident answer)
        if (inputTokens > 0)
        {
            var ratio = (double)outputTokens / inputTokens;
            if (ratio >= 0.5) score += 15;
            else if (ratio >= 0.2) score += 8;
            else score -= 10;
        }

        // Penalize truncated-looking content
        if (content.TrimEnd().EndsWith("...") || content.TrimEnd().EndsWith("—"))
            score -= 15;

        // Reward structured responses
        if (content.Contains('\n') || content.Contains("1.") || content.Contains("•"))
            score += 5;

        return Math.Max(0, Math.Min(100, Math.Round(score, 1)));
    }

    /// <summary>
    /// GET /api/v1/ai/fill-my-calendar — scan next 7 days for open slots and match lapsed clients.
    /// Returns slot/client pairs with AI-generated SMS preview for each match.
    /// </summary>
    [HttpGet("fill-my-calendar")]
    [FeatureGuard("ai_insights")]
    public async Task<IActionResult> FillMyCalendar(
        [FromQuery][System.ComponentModel.DataAnnotations.Range(1, 30)] int daysAhead = 7,
        [FromServices] CalendarGapAnalyzer gapAnalyzer = null!,
        [FromServices] ClientMatchingService matchingService = null!)
    {
        var tenantId = GetTenantId();

        var openSlots = await gapAnalyzer.GetOpenSlotsAsync(tenantId, daysAhead);
        if (!openSlots.Any())
            return Ok(new { totalOpenSlots = 0, matches = Array.Empty<object>() });

        var matches = await matchingService.FindMatchesAsync(tenantId, openSlots);

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        var businessName = tenant?.Name ?? "our business";

        return Ok(new
        {
            totalOpenSlots = openSlots.Count,
            totalMatches = matches.Count,
            matches = matches.Select(m => new
            {
                slot = new
                {
                    staffId = m.Slot.StaffId,
                    staffName = m.Slot.StaffName,
                    start = m.Slot.Start,
                    end = m.Slot.End,
                    durationMinutes = m.Slot.DurationMinutes
                },
                clients = m.MatchedClients.Select(c => new
                {
                    clientId = c.ClientId,
                    name = c.Name,
                    phone = c.Phone,
                    lastServiceName = c.LastServiceName,
                    daysSinceLastVisit = c.DaysSinceLastVisit,
                    hasSmsConsent = c.HasSmsConsent,
                    score = c.Score
                })
            })
        });
    }

    /// <summary>
    /// POST /api/v1/ai/fill-my-calendar/generate-sms — generate personalized outreach SMS for a slot/client pair.
    /// </summary>
    [HttpPost("fill-my-calendar/generate-sms")]
    [FeatureGuard("ai_insights")]
    [ChecksUsage(UsageType.AiCredits)]
    public async Task<IActionResult> GenerateFillCalendarSms([FromBody] FillCalendarSmsRequest request)
    {
        var tenantId = GetTenantId();
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        var businessName = tenant?.Name ?? "us";

        var slotTime = request.SlotStart.ToString("dddd, MMM d 'at' h:mm tt");

        var prompt = $"""
            Write a warm, personalized SMS (under 160 characters) inviting {request.ClientName} back to {businessName}.
            Their last service was "{request.LastServiceName}" about {request.DaysSinceLastVisit} days ago.
            We have an opening on {slotTime}. Include a friendly nudge and end with a booking link placeholder [BOOK_LINK].
            No hashtags. No emojis unless absolutely natural. First-person from the business.
            """;

        var result = await _aiService.GenerateTextAsync(tenantId, GetUserId(), prompt);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail("Failed to generate message"));

        return Ok(new
        {
            message = result.Content?.Trim(),
            clientId = request.ClientId,
            slotStart = request.SlotStart,
            tokenCost = result.Cost
        });
    }

    /// <summary>
    /// POST /api/v1/ai/fill-my-calendar/send — fire SMS campaign for selected slot/client pairs.
    /// </summary>
    [HttpPost("fill-my-calendar/send")]
    [FeatureGuard("ai_insights")]
    public async Task<IActionResult> SendFillCalendarCampaign(
        [FromBody] FillCalendarSendRequest request,
        [FromServices] ISmsService smsService = null!)
    {
        if (request.Items.Count > 50)
            return BadRequest(new { error = "Maximum 50 messages per batch. Use Campaigns for larger sends." });

        var tenantId = GetTenantId();
        int sent = 0, failed = 0;

        foreach (var item in request.Items)
        {
            if (string.IsNullOrEmpty(item.Phone) || string.IsNullOrEmpty(item.Message)) continue;

            var result = await smsService.SendSmsAsync(tenantId, item.Phone, item.Message, item.ClientId);
            if (result.Success) sent++;
            else failed++;
        }

        _logger.LogInformation("[FillMyCalendar] Sent {Sent} outreach SMS, {Failed} failed. Tenant: {TenantId}", sent, failed, tenantId);

        return Ok(new { sent, failed, total = request.Items.Count });
    }

    /// <summary>
    /// Returns at-risk clients sorted by LTV. Used to populate the retention dashboard widget.
    /// Requires ai_insights feature gate.
    /// </summary>
    [HttpGet("client-insights/at-risk")]
    [FeatureGuard("ai_insights")]
    public async Task<IActionResult> GetAtRiskClients(
        [FromQuery] int limit = 50,
        [FromServices] ClientRetentionService retentionService = null!)
    {
        var tenantId = GetTenantId();
        var clients = await retentionService.GetAtRiskClientsAsync(tenantId, limit);
        return Ok(new { data = clients, total = clients.Count });
    }

    /// <summary>
    /// Generates an AI-personalized re-engagement SMS message for a specific at-risk client.
    /// </summary>
    [HttpPost("client-insights/re-engagement-message")]
    [FeatureGuard("ai_insights")]
    [ChecksUsage(UsageType.AiCredits)]
    public async Task<IActionResult> GenerateReEngagementMessage(
        [FromBody] ReEngagementRequest request,
        [FromServices] ClientRetentionService retentionService = null!)
    {
        var tenantId = GetTenantId();

        var atRiskClient = new AtRiskClient
        {
            ClientId = request.ClientId,
            FullName = request.ClientFullName,
            Phone = request.ClientPhone,
            LifetimeValue = request.LifetimeValue,
            TotalBookings = request.TotalBookings,
            DaysSinceLastVisit = request.DaysSinceLastVisit
        };

        var message = await retentionService.GenerateReEngagementMessageAsync(
            tenantId, atRiskClient, request.BusinessName, request.ServiceType);

        return Ok(new { message });
    }
}

public class ApprovalQueueItem
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FinalContent { get; set; }
    public double ConfidenceScore { get; set; }
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    public string Reason { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public record ApprovalActionRequest(string? Note, string? EditedContent);

public class StructuredAIRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? Model { get; set; }
    /// <summary>Named schema to validate against (e.g., "WorkflowTriggerConfig")</summary>
    public string? SchemaName { get; set; }
    /// <summary>Optional inline JSON schema description to include in the AI prompt</summary>
    public string? JsonSchema { get; set; }
}

public record AIGenerateRequest(string Prompt, string? Model);

public class ReEngagementRequest
{
    public Guid ClientId { get; set; }
    public string ClientFullName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public decimal LifetimeValue { get; set; }
    public int TotalBookings { get; set; }
    public int DaysSinceLastVisit { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
}
public record AIImageRequest(string Prompt, string? Size, string? Style);
public record CopywritingRequest(
    string Type, // email, sms, social, ad
    string BusinessType,
    string Tone, // professional, friendly, casual, urgent
    string[]? KeyPoints
);

public record SentimentAnalysisRequest(string Content);

public class FillCalendarSmsRequest
{
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string LastServiceName { get; set; } = string.Empty;
    public int DaysSinceLastVisit { get; set; }
    public DateTime SlotStart { get; set; }
}

public class FillCalendarSendItem
{
    public Guid ClientId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class FillCalendarSendRequest
{
    public List<FillCalendarSendItem> Items { get; set; } = new();
}

