using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Implements pre-chat form collection and canned response management for live chat.
/// </summary>
public class LiveChatService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LiveChatService> _logger;

    public LiveChatService(AppDbContext context, ILogger<LiveChatService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PreChatFormResult> SubmitPreChatFormAsync(Guid tenantId, PreChatFormData form)
    {
        _logger.LogInformation("Pre-chat form submitted for tenant {TenantId} by {Name}", tenantId, form.Name);

        // Try to match existing client by email
        var existingClient = await _context.Clients
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Email == form.Email);

        Guid? clientId = existingClient?.Id;

        // Create a new chat session record linked to the client
        var conversationId = Guid.NewGuid();
        _context.AuditEntries.Add(new AuditEntry
        {
            TenantId = tenantId,
            EntityType = "ChatSession",
            EntityId = conversationId.ToString(),
            Action = "PreChatFormSubmitted",
            UserName = form.Email ?? "anonymous",
            Timestamp = DateTime.UtcNow,
            Details = System.Text.Json.JsonSerializer.Serialize(new
            {
                form.Name,
                form.Email,
                form.Phone,
                form.Subject,
                ClientId = clientId
            })
        });
        await _context.SaveChangesAsync();

        return new PreChatFormResult
        {
            ConversationId = conversationId,
            ClientId = clientId,
            IsReturningClient = existingClient != null,
            WelcomeMessage = existingClient != null
                ? $"Welcome back, {existingClient.FirstName}! How can we help you today?"
                : $"Hi {form.Name}! Thanks for reaching out. We'll be with you shortly."
        };
    }

    public async Task<List<CannedResponseDto>> GetCannedResponsesAsync(Guid tenantId, string? category = null)
    {
        var responses = await _context.AuditEntries
            .Where(a => a.TenantId == tenantId && a.EntityType == "CannedResponse")
            .Select(a => new CannedResponseDto
            {
                Id = Guid.Parse(a.EntityId),
                Title = a.Action,
                Body = a.Details ?? string.Empty,
                Category = a.UserName ?? string.Empty,
                Shortcut = a.ChangedFields ?? string.Empty
            })
            .ToListAsync();

        if (!string.IsNullOrEmpty(category))
            responses = responses.Where(r => r.Category == category).ToList();

        // If no custom responses in DB, return built-in defaults
        if (!responses.Any())
        {
            responses = GetDefaultCannedResponses();
        }

        return responses;
    }

    public async Task<CannedResponseDto> UpsertCannedResponseAsync(Guid tenantId, CannedResponseDto dto)
    {
        var id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;

        var existing = await _context.AuditEntries
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.EntityType == "CannedResponse" && a.EntityId == id.ToString());

        if (existing != null)
        {
            existing.Action = dto.Title;
            existing.Details = dto.Body;
            existing.UserName = dto.Category;
            existing.ChangedFields = dto.Shortcut;
        }
        else
        {
            _context.AuditEntries.Add(new AuditEntry
            {
                TenantId = tenantId,
                EntityType = "CannedResponse",
                EntityId = id.ToString(),
                Action = dto.Title,
                Details = dto.Body,
                UserName = dto.Category,
                ChangedFields = dto.Shortcut,
                Timestamp = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        dto.Id = id;
        return dto;
    }

    private static List<CannedResponseDto> GetDefaultCannedResponses() => new()
    {
        new() { Id = Guid.NewGuid(), Title = "Greeting", Body = "Hi there! Welcome to our support chat. How can I help you today?", Category = "Greeting", Shortcut = "/hi" },
        new() { Id = Guid.NewGuid(), Title = "Booking Help", Body = "I'd be happy to help you with your booking. Could you please share your booking reference number?", Category = "Booking", Shortcut = "/book" },
        new() { Id = Guid.NewGuid(), Title = "Hours", Body = "Our business hours are Monday–Friday 9am–6pm and Saturday 10am–4pm.", Category = "General", Shortcut = "/hours" },
        new() { Id = Guid.NewGuid(), Title = "Cancellation Policy", Body = "Cancellations made 24+ hours before your appointment are fully refunded. Within 24 hours, a 50% fee applies.", Category = "Policy", Shortcut = "/cancel" },
        new() { Id = Guid.NewGuid(), Title = "Thank You", Body = "Thank you for contacting us! Is there anything else I can help you with?", Category = "Closing", Shortcut = "/thanks" },
        new() { Id = Guid.NewGuid(), Title = "Transfer", Body = "Let me connect you with one of our specialists who can better assist you. Please hold for a moment.", Category = "Transfer", Shortcut = "/transfer" }
    };
}

public class PreChatFormData
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
}

public class PreChatFormResult
{
    public Guid ConversationId { get; set; }
    public Guid? ClientId { get; set; }
    public bool IsReturningClient { get; set; }
    public string WelcomeMessage { get; set; } = string.Empty;
}

public class CannedResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Shortcut { get; set; } = string.Empty;
}
