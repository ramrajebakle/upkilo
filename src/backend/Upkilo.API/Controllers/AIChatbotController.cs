using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Attributes;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[FeatureGuard("ai_copilot")]
public class AIChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AIChatbotController> _logger;

    public AIChatbotController(
        IChatbotService chatbotService,
        ITenantProvider tenantProvider,
        ILogger<AIChatbotController> logger)
    {
        _chatbotService = chatbotService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Process an incoming message from the chatbot
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> ProcessMessage([FromBody] ChatRequestDto request)
    {
        request.TenantId = GetTenantId();
        var response = await _chatbotService.ProcessMessageAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Train the chatbot knowledge base with a new FAQ
    /// </summary>
    [HttpPost("train")]
    public async Task<IActionResult> Train([FromBody] TrainRequest request)
    {
        var success = await _chatbotService.TrainKnowledgeBaseAsync(GetTenantId(), request.Category, request.Question, request.Answer);
        return Ok(new { success });
    }

    /// <summary>
    /// Gets all chatbot conversations for the tenant
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var conversations = await _chatbotService.GetConversationsAsync(GetTenantId());
        return Ok(new { data = conversations });
    }

    /// <summary>
    /// Gets the chatbot message history for a specific session
    /// </summary>
    [HttpGet("history/{sessionId}")]
    public async Task<IActionResult> GetChatHistory(Guid sessionId)
    {
        var history = await _chatbotService.GetHistoryAsync(GetTenantId(), sessionId);
        return Ok(new { data = history });
    }

    /// <summary>
    /// Gets all training data currently in the knowledge base
    /// </summary>
    [HttpGet("training-data")]
    public async Task<IActionResult> GetTrainingData()
    {
        var trainingData = await _chatbotService.GetTrainingDataAsync(GetTenantId());
        return Ok(new { data = trainingData });
    }
}

public class TrainRequest
{
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// Public AI Receptionist — customer-facing chatbot on the booking widget.
/// No auth required; tenant identified by slug. Rate-limited by IP via "receptionist" policy.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/receptionist")]
[AllowAnonymous]
public class PublicReceptionistController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PublicReceptionistController> _logger;

    // A1: session TTL — conversation memory kept for 30 min of inactivity
    private static readonly DistributedCacheEntryOptions _sessionOpts = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    public PublicReceptionistController(
        IChatbotService chatbotService,
        AppDbContext context,
        IDistributedCache cache,
        ILogger<PublicReceptionistController> logger)
    {
        _chatbotService = chatbotService;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/v1/receptionist/{tenantSlug}/chat
    /// A1: Redis-backed conversation memory per session.
    ///     After 2 consecutive low-confidence/unknown-intent turns, escalates to human handoff.
    ///     Rate-limited to 10 req/min per IP via the "receptionist" policy in Program.cs.
    /// </summary>
    [HttpPost("{tenantSlug}/chat")]
    [EnableRateLimiting("receptionist")]
    public async Task<IActionResult> Chat(string tenantSlug, [FromBody] PublicChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 1000)
            return BadRequest(new { error = "Message must be 1-1000 characters." });

        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive && !t.IsDeleted);

        if (tenant == null)
            return NotFound(new { error = "Business not found." });

        // A1: Load session state from Redis
        var sessionKey = $"receptionist:session:{request.SessionId}";
        SessionState session;
        var cached = await _cache.GetStringAsync(sessionKey);
        session = cached != null
            ? JsonSerializer.Deserialize<SessionState>(cached) ?? new SessionState()
            : new SessionState();

        // A1: Prevent handoff-already-requested from looping
        if (session.HumanHandoffTriggered)
        {
            return Ok(new
            {
                reply = "You've been connected to a staff member who will respond shortly. Thank you for your patience!",
                sessionId = request.SessionId,
                intent = "human_handoff",
                confidence = 1.0m,
                handoffRequested = true
            });
        }

        var chatRequest = new ChatRequestDto
        {
            TenantId = tenant.Id,
            ExternalId = request.SessionId.ToString(),
            Channel = ConversationChannel.WebChat,
            Message = request.Message
        };

        var response = await _chatbotService.ProcessMessageAsync(chatRequest);

        // A1: Track consecutive failed intent resolutions
        var isFailedIntent = response == null
            || string.IsNullOrEmpty(response.Intent)
            || response.Intent == "unknown"
            || response.Confidence < 0.4m;

        if (isFailedIntent)
            session.ConsecutiveFailedIntents++;
        else
            session.ConsecutiveFailedIntents = 0;

        session.TurnCount++;

        // A1: Human fallback trigger — 2 consecutive failed intents OR explicit handoff request
        var triggerHandoff = session.ConsecutiveFailedIntents >= 2 || (response?.HandoffRequested ?? false);
        if (triggerHandoff)
            session.HumanHandoffTriggered = true;

        // Persist updated session to Redis
        await _cache.SetStringAsync(sessionKey, JsonSerializer.Serialize(session), _sessionOpts);

        _logger.LogInformation(
            "[A1] Receptionist slug={Slug} session={Session} intent={Intent} confidence={Confidence:F2} failed={Failed} handoff={Handoff}",
            tenantSlug, request.SessionId, response?.Intent ?? "unknown",
            response?.Confidence ?? 0m, session.ConsecutiveFailedIntents, triggerHandoff);

        var reply = triggerHandoff && !response!.HandoffRequested
            ? "I want to make sure you get the right help. Let me connect you with a staff member who can assist you directly."
            : response?.Response ?? "I'm here to help! Feel free to ask about our services or availability.";

        return Ok(new
        {
            reply,
            sessionId = request.SessionId,
            intent = response?.Intent,
            confidence = response?.Confidence,
            handoffRequested = triggerHandoff,
            turnCount = session.TurnCount
        });
    }

    private class SessionState
    {
        public int ConsecutiveFailedIntents { get; set; }
        public int TurnCount { get; set; }
        public bool HumanHandoffTriggered { get; set; }
    }
}

public record PublicChatRequest(string Message, Guid SessionId);

