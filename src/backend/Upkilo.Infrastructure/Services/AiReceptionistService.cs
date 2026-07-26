using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// AI Receptionist — handles inbound SMS via Twilio webhook.
/// Conversation history is stored in Redis (TTL 24h) so it survives restarts and scales horizontally.
/// Escalates to human after 2 consecutive failed intent resolutions (A1).
/// </summary>
public class AiReceptionistService
{
    private readonly AppDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IAIService _aiService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AiReceptionistService> _logger;

    private static readonly DistributedCacheEntryOptions _sessionTtl = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
        SlidingExpiration = TimeSpan.FromHours(4)
    };

    public AiReceptionistService(
        AppDbContext context,
        ISmsService smsService,
        IAIService aiService,
        IDistributedCache cache,
        ILogger<AiReceptionistService> logger)
    {
        _context = context;
        _smsService = smsService;
        _aiService = aiService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Process an inbound SMS from a client and produce a reply.
    /// Returns the reply text (already sent via SMS) and whether escalation was triggered.
    /// </summary>
    public async Task<ReceptionistResponse> HandleInboundSmsAsync(
        Guid tenantId,
        string fromPhone,
        string message)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Services)
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null)
            return new ReceptionistResponse { Sent = false, Reply = "", Escalated = false };

        if (tenant.Settings.TryGetValue("ai_receptionist_enabled", out var enabledObj) &&
            bool.TryParse(enabledObj?.ToString(), out var receptionistEnabled) && !receptionistEnabled)
            return new ReceptionistResponse { Sent = false, Reply = "", Escalated = false };

        var sessionKey = $"receptionist:sms:{tenantId}:{fromPhone}";
        var history = await LoadHistoryAsync(sessionKey);

        // A1: escalate after 2 failed intent resolutions (not 3)
        var failedAttempts = history.Count(t => t.Role == "assistant" && t.IsFailedAttempt);
        if (failedAttempts >= 2)
        {
            await EscalateToHumanAsync(tenantId, fromPhone, tenant);
            await _cache.RemoveAsync(sessionKey);
            var escalationMsg = $"I'll connect you with a team member shortly. Someone from {tenant.Name} will text you back soon!";
            await _smsService.SendSmsAsync(tenantId, fromPhone, escalationMsg);
            return new ReceptionistResponse { Sent = true, Reply = escalationMsg, Escalated = true };
        }

        history.Add(new ConversationTurn { Role = "user", Content = message });

        var servicesSummary = string.Join(", ", tenant.Services.Take(10).Select(s => $"{s.Name} (${s.Price})"));
        var availability = await GetAvailabilitySummaryAsync(tenantId);

        var prompt = $"""
            You are the AI receptionist for {tenant.Name}. Reply via SMS (under 160 chars).
            Services: {servicesSummary}.
            Availability: {availability}
            Recent conversation: {string.Join(" | ", history.TakeLast(6).Select(t => $"{t.Role}: {t.Content}"))}
            Client message: "{message}"

            Rules:
            - Reply must be under 160 characters (one SMS).
            - If client wants to book, confirm service + preferred time + their name.
            - If you cannot help after trying, say you'll escalate.
            - Be warm and professional.

            Reply ONLY with the SMS text, no quotes.
            """;

        string reply;
        bool isFailedAttempt = false;

        try
        {
            var result = await _aiService.GenerateTextAsync(tenantId, Guid.Empty, prompt);
            reply = result.Content?.Trim() ?? "";

            if (string.IsNullOrEmpty(reply))
            {
                reply = "I didn't quite catch that. Could you rephrase? Or call us directly for immediate help.";
                isFailedAttempt = true;
            }

            if (reply.Length > 160)
                reply = reply[..157] + "...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiReceptionist] AI call failed for tenant {TenantId}", tenantId);
            reply = "I'm having a moment. Please call us or try again shortly!";
            isFailedAttempt = true;
        }

        history.Add(new ConversationTurn { Role = "assistant", Content = reply, IsFailedAttempt = isFailedAttempt });

        // Keep last 20 turns, persist to Redis
        if (history.Count > 20)
            history = history.TakeLast(20).ToList();

        await SaveHistoryAsync(sessionKey, history);
        await _smsService.SendSmsAsync(tenantId, fromPhone, reply);

        _logger.LogInformation("[AiReceptionist] Tenant={TenantId} From={Phone} Reply={Reply}",
            tenantId, fromPhone, reply[..Math.Min(80, reply.Length)]);

        return new ReceptionistResponse { Sent = true, Reply = reply, Escalated = false };
    }

    private async Task<List<ConversationTurn>> LoadHistoryAsync(string key)
    {
        var json = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(json)) return new List<ConversationTurn>();
        return JsonSerializer.Deserialize<List<ConversationTurn>>(json) ?? new List<ConversationTurn>();
    }

    private async Task SaveHistoryAsync(string key, List<ConversationTurn> history)
    {
        var json = JsonSerializer.Serialize(history);
        await _cache.SetStringAsync(key, json, _sessionTtl);
    }

    private async Task<string> GetAvailabilitySummaryAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var upcoming = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.StartTime >= now &&
                        b.StartTime <= now.AddDays(7) &&
                        b.Status == BookingStatus.Confirmed)
            .OrderBy(b => b.StartTime)
            .Take(20)
            .Select(b => b.StartTime.ToString("ddd MMM d ha"))
            .ToListAsync();

        if (!upcoming.Any()) return "Open all week — plenty of slots available.";
        return $"Busy: {string.Join(", ", upcoming.Take(5))}. Other times available.";
    }

    private async Task EscalateToHumanAsync(Guid tenantId, string fromPhone, Tenant tenant)
    {
        _logger.LogWarning("[AiReceptionist] Escalating {Phone} to human. Tenant={TenantId}", fromPhone, tenantId);

        if (tenant.Settings.TryGetValue("staff_notification_phone", out var staffPhoneObj) &&
            !string.IsNullOrEmpty(staffPhoneObj?.ToString()))
        {
            await _smsService.SendSmsAsync(
                tenantId,
                staffPhoneObj.ToString()!,
                $"AI Receptionist escalation: Client at {fromPhone} needs human help. Please reply ASAP.");
        }
    }
}

public class ReceptionistResponse
{
    public bool Sent { get; set; }
    public string Reply { get; set; } = string.Empty;
    public bool Escalated { get; set; }
}

public class ConversationTurn
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsFailedAttempt { get; set; }
}
