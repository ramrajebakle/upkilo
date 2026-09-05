using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using System.Net.Mail;
using System.Security.Cryptography;
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
    private readonly AppDbContext _context;

    public AIChatbotController(
        IChatbotService chatbotService,
        ITenantProvider tenantProvider,
        ILogger<AIChatbotController> logger,
        AppDbContext context)
    {
        _chatbotService = chatbotService;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _context = context;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Process an incoming message from the chatbot
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> ProcessMessage([FromBody] ChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 1000)
            return BadRequest(new { error = "Message must be 1-1000 characters." });

        // All three are overwritten from the authenticated principal rather than trusted from the
        // body. TenantId already was; Audience must be too, or a caller could simply post
        // Audience = TenantStaff from the public widget and pull Upkilo platform knowledge into
        // a customer-facing conversation.
        request.TenantId = GetTenantId();
        request.Audience = ChatAudience.TenantStaff;

        // ExternalId was taken straight from the body, and conversation lookup is
        // (TenantId, ExternalId, Channel) - so one staff member could post a colleague's
        // ExternalId and resume THEIR conversation. Since history is replayed into the prompt,
        // that read the colleague's transcript back out. Binding it to the authenticated user id
        // makes each user's conversation reachable only by that user.
        var userId = _tenantProvider.GetUserId()
            ?? throw new UnauthorizedAccessException("User context not available");

        request.ExternalId = $"staff:{userId}";
        request.Channel = ConversationChannel.WebChat;

        var response = await _chatbotService.ProcessMessageAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Train the chatbot knowledge base with a new FAQ
    /// </summary>
    [HttpPost("train")]
    public async Task<IActionResult> Train([FromBody] TrainRequest request)
    {
        // Validated here because these two strings are copied verbatim into the system prompt as
        // the highest-ranked source of truth. Unbounded input is therefore a cost and a
        // context-window problem, not just a database one, and an empty pair would add a "Q:\nA:"
        // block that teaches the model nothing.
        if (string.IsNullOrWhiteSpace(request.Question) || string.IsNullOrWhiteSpace(request.Answer))
            return BadRequest(new { error = "Question and answer are both required." });

        if (request.Question.Length > 500 || request.Answer.Length > 2000)
            return BadRequest(new { error = "Question must be under 500 characters and answer under 2000." });

        var entry = await _chatbotService.TrainKnowledgeBaseAsync(
            GetTenantId(), request.Category, request.Question, request.Answer);

        // The persisted row, so the caller can render it without a refetch. Shaped explicitly
        // rather than returning the entity, which would serialise TenantId and the soft-delete
        // bookkeeping to the browser for no reason.
        return Ok(new
        {
            id = entry.Id,
            category = entry.Category,
            question = entry.Question,
            answer = entry.Answer
        });
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

    // The chatbot admin page has been calling settings, kb and stats since it shipped. None of
    // them existed, so the page threw on load and rendered nothing: getSettings and
    // getKnowledgeBase are awaited together in a Promise.all, so the first 404 rejected the
    // whole load. They are added here, on the persisted AIKnowledgeBase and ChatWidget stores,
    // rather than pointed at KnowledgeBaseController - that controller keeps its entries in a
    // private static ConcurrentDictionary, so anything written there is lost on restart and
    // invisible to other instances.

    /// <summary>
    /// GET /api/v1/aichatbot/settings — assistant configuration for this tenant.
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var tenantId = GetTenantId();
        var widget = await _context.ChatWidgets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.TenantId == tenantId);

        // A tenant that has never opened the page has no row yet. Returning defaults keeps the
        // form usable without writing a row on a read.
        return Ok(new
        {
            isEnabled = widget?.IsEnabled ?? true,
            botName = widget?.BotName ?? "Upkilo Assistant",
            handoffEmail = widget?.HandoffEmail ?? string.Empty,
            welcomeMessage = widget?.WelcomeMessage ?? "Hello! How can I help you today?"
        });
    }

    /// <summary>
    /// PUT /api/v1/aichatbot/settings — upsert assistant configuration.
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] ChatbotSettingsRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.HandoffEmail)
            && !MailAddress.TryCreate(request.HandoffEmail, out _))
            return BadRequest(new { error = "Handoff email is not a valid email address" });

        var tenantId = GetTenantId();
        var widget = await _context.ChatWidgets.FirstOrDefaultAsync(w => w.TenantId == tenantId);

        if (widget == null)
        {
            widget = new ChatWidget { TenantId = tenantId };
            _context.ChatWidgets.Add(widget);
        }

        widget.IsEnabled = request.IsEnabled;
        widget.BotName = Trim(request.BotName, 100);
        widget.HandoffEmail = Trim(request.HandoffEmail, 256);
        widget.WelcomeMessage = Trim(request.WelcomeMessage, 500);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            isEnabled = widget.IsEnabled,
            botName = widget.BotName,
            handoffEmail = widget.HandoffEmail,
            welcomeMessage = widget.WelcomeMessage
        });
    }

    /// <summary>
    /// GET /api/v1/aichatbot/kb — knowledge base entries backing the assistant.
    /// </summary>
    [HttpGet("kb")]
    public async Task<IActionResult> GetKnowledgeBase()
    {
        var entries = await _chatbotService.GetTrainingDataAsync(GetTenantId());

        // Projected to the same four fields the "train" response returns, so the admin page can
        // append a new entry to this list without the two shapes disagreeing. It also keeps
        // TenantId and the soft-delete bookkeeping out of the browser.
        return Ok(entries.Select(e => new
        {
            id = e.Id,
            category = e.Category,
            question = e.Question,
            answer = e.Answer
        }));
    }

    /// <summary>
    /// DELETE /api/v1/aichatbot/kb/{id} — remove a knowledge base entry.
    /// </summary>
    [HttpDelete("kb/{id:guid}")]
    public async Task<IActionResult> DeleteKnowledgeBaseEntry(Guid id)
    {
        var tenantId = GetTenantId();

        // Matched on both id AND tenant, so an id belonging to another tenant is a 404 rather
        // than a cross-tenant delete.
        var entry = await _context.Set<AIKnowledgeBase>()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (entry == null) return NotFound(new { error = "Knowledge base entry not found" });

        _context.Remove(entry);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// GET /api/v1/aichatbot/stats — conversation counts for the admin page header.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var tenantId = GetTenantId();
        var conversations = await _chatbotService.GetConversationsAsync(tenantId);

        var total = conversations.Count;

        // "Active handoffs" is the queue a human still has to work: a handoff asked for or
        // being handled, but not yet closed. A closed conversation is done regardless of
        // whether a human touched it, so it must not count as an outstanding handoff.
        var activeHandoffs = conversations.Count(c =>
            c.Status == ConversationStatus.HandoffRequested
            || c.Status == ConversationStatus.HumanHandled);

        // Resolved by the assistant: closed without ever needing a human.
        var resolved = conversations.Count(c =>
            c.Status == ConversationStatus.Closed && !c.HumanInteractionRequired);

        return Ok(new
        {
            totalConversations = total,
            // Percentage, and 0 rather than NaN when there is nothing to divide by.
            resolutionRate = total == 0 ? 0 : (int)Math.Round(resolved * 100.0 / total),
            activeHandoffs
        });
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

public class ChatbotSettingsRequest
{
    public bool IsEnabled { get; set; } = true;
    public string? BotName { get; set; }
    public string? HandoffEmail { get; set; }
    public string? WelcomeMessage { get; set; }
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
    private readonly IEntitlementService _entitlements;
    private readonly IConfiguration _configuration;

    // A1: session TTL — conversation memory kept for 30 min of inactivity
    private static readonly DistributedCacheEntryOptions _sessionOpts = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    public PublicReceptionistController(
        IChatbotService chatbotService,
        AppDbContext context,
        IDistributedCache cache,
        ILogger<PublicReceptionistController> logger,
        IEntitlementService entitlements,
        IConfiguration configuration)
    {
        _chatbotService = chatbotService;
        _context = context;
        _cache = cache;
        _logger = logger;
        _entitlements = entitlements;
        _configuration = configuration;
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

        // Entitlement is checked here explicitly because this route is [AllowAnonymous] and so
        // carries no [FeatureGuard] - those resolve the tenant from the authenticated principal,
        // which does not exist on a public widget request. Without this the endpoint was a
        // subscription bypass: any business, on any plan or none, including one whose
        // subscription had lapsed, got an unlimited public AI receptionist billed to Upkilo.
        if (!await _entitlements.HasFeatureAsync(tenant.Id, FeatureKeys.AiCopilot))
        {
            _logger.LogInformation(
                "[Receptionist] Refused for tenant {TenantId}: ai_copilot not entitled.", tenant.Id);
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "This business does not have the AI assistant enabled." });
        }

        // Resolve the session from a token this server issued, never from a caller-chosen id.
        //
        // The old contract took a raw Guid from the request body and used it directly as the
        // conversation's ExternalId. Conversation lookup is (TenantId, ExternalId, Channel), so
        // anyone who learned or guessed another visitor's session id resumed THEIR conversation
        // on that business - and now that history is replayed into the prompt, that is a direct
        // read of a stranger's messages. A signed token cannot be forged, so a session can only
        // be continued by whoever was given it.
        var sessionId = ResolveSession(request.SessionToken, tenant.Id) ?? Guid.NewGuid();
        var sessionToken = IssueSessionToken(sessionId, tenant.Id);

        // Scoped by tenant as well. The key was "receptionist:session:{SessionId}", shared across
        // every business on the platform, so a visitor's handoff flag and turn counter carried
        // from one business's widget into another's.
        var sessionKey = $"receptionist:session:{tenant.Id}:{sessionId}";
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
                sessionToken,
                intent = "human_handoff",
                confidence = 1.0m,
                handoffRequested = true
            });
        }

        var chatRequest = new ChatRequestDto
        {
            TenantId = tenant.Id,
            ExternalId = sessionId.ToString(),
            Channel = ConversationChannel.WebChat,
            Message = request.Message,
            // A member of the public, so Upkilo platform knowledge is out of scope for this turn.
            Audience = ChatAudience.PublicVisitor
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
            tenantSlug, sessionId, response?.Intent ?? "unknown",
            response?.Confidence ?? 0m, session.ConsecutiveFailedIntents, triggerHandoff);

        var reply = triggerHandoff && !response!.HandoffRequested
            ? "I want to make sure you get the right help. Let me connect you with a staff member who can assist you directly."
            : response?.Response ?? "I'm here to help! Feel free to ask about our services or availability.";

        return Ok(new
        {
            reply,
            sessionToken,
            intent = response?.Intent,
            confidence = response?.Confidence,
            handoffRequested = triggerHandoff,
            turnCount = session.TurnCount
        });
    }

    /// <summary>
    /// Verifies a session token and returns its session id, or null when the token is missing,
    /// malformed, or not one this server issued for THIS tenant.
    /// </summary>
    private Guid? ResolveSession(string? token, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var parts = token.Split('.', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var sessionId)) return null;

        var expected = Sign(sessionId, tenantId);

        // Fixed-time comparison: a length-or-prefix comparison here would leak the signature a
        // byte at a time to anyone willing to make enough requests.
        var provided = System.Text.Encoding.UTF8.GetBytes(parts[1]);
        var computed = System.Text.Encoding.UTF8.GetBytes(expected);
        if (provided.Length != computed.Length) return null;
        if (!CryptographicOperations.FixedTimeEquals(provided, computed)) return null;

        return sessionId;
    }

    private string IssueSessionToken(Guid sessionId, Guid tenantId) =>
        $"{sessionId}.{Sign(sessionId, tenantId)}";

    /// <summary>
    /// The tenant id is inside the signature, so a token minted on one business's widget does not
    /// verify on another's.
    /// </summary>
    private string Sign(Guid sessionId, Guid tenantId)
    {
        var secret = _configuration["Receptionist:SessionSecret"]
                     ?? _configuration["Jwt:Secret"]
                     ?? throw new InvalidOperationException(
                         "No signing secret configured for receptionist sessions (Jwt:Secret).");

        using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var payload = System.Text.Encoding.UTF8.GetBytes($"receptionist-session|{tenantId}|{sessionId}");
        return Convert.ToBase64String(hmac.ComputeHash(payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private class SessionState
    {
        public int ConsecutiveFailedIntents { get; set; }
        public int TurnCount { get; set; }
        public bool HumanHandoffTriggered { get; set; }
    }
}

/// <summary>
/// SessionToken is opaque and server-issued: the caller echoes back whatever the previous
/// response returned, and omits it on the first turn. It replaced a caller-supplied Guid, which
/// let anyone resume a session that was not theirs.
/// </summary>
public record PublicChatRequest(string Message, string? SessionToken);

