using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services.Agents;

/// <summary>
/// AI Voice Agent — handles real-time Twilio voice interactions, accumulates per-call transcripts,
/// and produces post-call summaries with no-show risk scoring (A2, A3).
/// </summary>
public class VoiceAgentService
{
    private readonly IAIService _aiService;
    private readonly ISmsService _smsService;
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<VoiceAgentService> _logger;

    private static readonly DistributedCacheEntryOptions _callTtl = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
    };

    public VoiceAgentService(
        IAIService aiService,
        ISmsService smsService,
        AppDbContext context,
        IDistributedCache cache,
        ILogger<VoiceAgentService> logger)
    {
        _aiService = aiService;
        _smsService = smsService;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Processes incoming Speech-To-Text transcript from Twilio and returns AI-synthesized TTS response.
    /// Appends each turn to Redis-backed call transcript for post-call summary.
    /// </summary>
    public async Task<string> ProcessVoiceRequestAsync(Guid tenantId, string speechResult, string callSid)
    {
        _logger.LogInformation("[VoiceAgent] Tenant={TenantId} CallSid={CallSid} Speech={Speech}",
            tenantId, callSid, speechResult[..Math.Min(80, speechResult.Length)]);

        if (string.IsNullOrWhiteSpace(speechResult))
            return "Hello! I'm the AI booking assistant. How can I help you today?";

        // Accumulate transcript for post-call summary
        await AppendTranscriptAsync(callSid, tenantId, "caller", speechResult);

        var prompt = $"You are a phone booking assistant for a service business. " +
                     $"The caller said: '{speechResult}'. " +
                     $"Respond professionally and concisely (max 2 sentences) to help them book an appointment.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);

        var response = result.Success
            ? result.Content ?? "I'm sorry, I couldn't understand that."
            : "I'm experiencing connection difficulties. Please try again.";

        await AppendTranscriptAsync(callSid, tenantId, "agent", response);

        return response;
    }

    /// <summary>
    /// A2: Called by status-callback when Twilio reports call completed.
    /// Generates structured post-call summary, sends confirmation SMS, and scores no-show risk.
    /// </summary>
    public async Task HandleCallCompletedAsync(
        Guid tenantId,
        string callSid,
        string callerPhone,
        int callDurationSeconds)
    {
        var transcript = await LoadTranscriptAsync(callSid);
        if (transcript.Count == 0)
        {
            _logger.LogInformation("[VoiceAgent] No transcript for {CallSid} — skipping summary", callSid);
            return;
        }

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return;

        var transcriptText = string.Join("\n", transcript.Select(t => $"{t.Speaker}: {t.Text}"));

        var jsonSchema = """
            {
              "intent": "booking|inquiry|complaint|other",
              "serviceMentioned": "service name or null",
              "preferredDate": "date string or null",
              "bookingOutcome": "booked|not_booked|follow_up_needed",
              "sentimentScore": 0.0-1.0,
              "noShowRiskScore": 0.0-1.0,
              "actionItems": ["action1", "action2"],
              "summary": "1-2 sentence plain English summary"
            }
            """;

        var summaryPrompt =
            $"Analyze this phone call transcript for a service booking business ({tenant.Name}).\n" +
            $"Return ONLY valid JSON with no markdown, no explanation:\n" +
            jsonSchema + "\n" +
            $"Transcript:\n{transcriptText}\n\n" +
            "Scoring guide — noShowRiskScore:\n" +
            "- 0.0-0.3: client was engaged, confirmed time, gave name\n" +
            "- 0.4-0.6: vague interest, no firm commitment\n" +
            "- 0.7-1.0: hesitant, missed information, past tense frustration signals";

        PostCallSummary? summary = null;
        try
        {
            var result = await _aiService.GenerateTextAsync(tenantId, Guid.Empty, summaryPrompt);
            if (result.Success && !string.IsNullOrEmpty(result.Content))
            {
                var json = result.Content.Trim();
                // Strip any accidental markdown fences
                if (json.StartsWith("```")) json = json.Split('\n').Skip(1).TakeWhile(l => !l.StartsWith("```")).Aggregate((a, b) => a + "\n" + b);
                summary = JsonSerializer.Deserialize<PostCallSummary>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VoiceAgent] Summary generation failed for {CallSid}", callSid);
        }

        summary ??= new PostCallSummary
        {
            Intent = "unknown",
            BookingOutcome = "follow_up_needed",
            SentimentScore = 0.5m,
            NoShowRiskScore = 0.5m,
            Summary = $"Call of {callDurationSeconds}s — summary unavailable.",
            ActionItems = new List<string> { "Review call manually" }
        };

        _logger.LogInformation(
            "[VoiceAgent] PostCallSummary CallSid={CallSid} Outcome={Outcome} NoShowRisk={Risk:P0} Sentiment={Sentiment:P0}",
            callSid, summary.BookingOutcome, summary.NoShowRiskScore, summary.SentimentScore);

        // A2: Send booking confirmation SMS to caller
        if (summary.BookingOutcome == "booked" && !string.IsNullOrEmpty(callerPhone))
        {
            var confirmationMsg = summary.ServiceMentioned != null
                ? $"Hi! Your {summary.ServiceMentioned} booking at {tenant.Name} is confirmed. We'll send a reminder closer to your appointment."
                : $"Hi! Your booking at {tenant.Name} is confirmed. We'll send a reminder closer to your appointment.";

            await _smsService.SendSmsAsync(tenantId, callerPhone, confirmationMsg);
            _logger.LogInformation("[VoiceAgent] Confirmation SMS sent to {Phone}", callerPhone);
        }

        // A2: Send post-call summary to business owner/staff
        if (tenant.Settings.TryGetValue("staff_notification_phone", out var staffPhoneObj)
            && !string.IsNullOrEmpty(staffPhoneObj?.ToString()))
        {
            var summaryMsg = $"Call summary: {summary.Summary} | Outcome: {summary.BookingOutcome} | No-show risk: {summary.NoShowRiskScore:P0}";
            if (summaryMsg.Length > 320) summaryMsg = summaryMsg[..317] + "...";
            await _smsService.SendSmsAsync(tenantId, staffPhoneObj.ToString()!, summaryMsg);
        }

        // A3: Auto-request deposit for high-risk bookings
        if (summary.BookingOutcome == "booked" && summary.NoShowRiskScore >= 0.7m && !string.IsNullOrEmpty(callerPhone))
        {
            var depositMsg = $"To confirm your booking at {tenant.Name}, a small deposit is required. Reply YES to receive the payment link, or call us to discuss.";
            await _smsService.SendSmsAsync(tenantId, callerPhone, depositMsg);
            _logger.LogInformation("[VoiceAgent] High no-show risk ({Risk:P0}) — deposit request sent to {Phone}",
                summary.NoShowRiskScore, callerPhone);
        }

        // Persist summary to DB for dashboard display
        await PersistCallSummaryAsync(tenantId, callSid, callerPhone, callDurationSeconds, summary, transcriptText);

        // Clean up transcript from Redis
        await _cache.RemoveAsync($"voice:transcript:{callSid}");
    }

    private async Task AppendTranscriptAsync(string callSid, Guid tenantId, string speaker, string text)
    {
        var key = $"voice:transcript:{callSid}";
        var existing = await LoadTranscriptAsync(callSid);
        existing.Add(new TranscriptTurn { Speaker = speaker, Text = text, TenantId = tenantId, At = DateTime.UtcNow });
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(existing), _callTtl);
    }

    private async Task<List<TranscriptTurn>> LoadTranscriptAsync(string callSid)
    {
        var json = await _cache.GetStringAsync($"voice:transcript:{callSid}");
        if (string.IsNullOrEmpty(json)) return new List<TranscriptTurn>();
        return JsonSerializer.Deserialize<List<TranscriptTurn>>(json) ?? new List<TranscriptTurn>();
    }

    private async Task PersistCallSummaryAsync(
        Guid tenantId, string callSid, string callerPhone,
        int durationSeconds, PostCallSummary summary, string transcript)
    {
        try
        {
            var log = new AIDecisionLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AgentName = "VoiceAgent",
                DecisionType = "PostCallSummary",
                InputData = transcript[..Math.Min(2000, transcript.Length)],
                OutputDecision = summary.Summary,
                ConfidenceScore = summary.SentimentScore,
                RequiresHumanReview = summary.NoShowRiskScore >= 0.7m || summary.BookingOutcome == "follow_up_needed",
                CreatedAt = DateTime.UtcNow
            };
            _context.AIDecisionLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VoiceAgent] Failed to persist call summary for {CallSid}", callSid);
        }
    }
}

public class PostCallSummary
{
    public string Intent { get; set; } = "unknown";
    public string? ServiceMentioned { get; set; }
    public string? PreferredDate { get; set; }
    public string BookingOutcome { get; set; } = "not_booked";
    public decimal SentimentScore { get; set; } = 0.5m;
    public decimal NoShowRiskScore { get; set; } = 0.5m;
    public List<string> ActionItems { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class TranscriptTurn
{
    public Guid TenantId { get; set; }
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime At { get; set; }
}
