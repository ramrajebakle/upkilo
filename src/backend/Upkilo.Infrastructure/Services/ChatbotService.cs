using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class ChatbotService : IChatbotService
{
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;
    private readonly IAIDashboardService _dashboardService;
    private readonly IBookingService _bookingService;
    private readonly ISchedulingService _schedulingService;

    public ChatbotService(
        AppDbContext context,
        IAIService aiService,
        IAIDashboardService dashboardService,
        IBookingService bookingService,
        ISchedulingService schedulingService)
    {
        _context = context;
        _aiService = aiService;
        _dashboardService = dashboardService;
        _bookingService = bookingService;
        _schedulingService = schedulingService;
    }

    public async Task<ChatResponseDto> ProcessMessageAsync(ChatRequestDto request)
    {
        // 1. Find or create conversation
        var conversation = await _context.AIConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId && c.ExternalId == request.ExternalId && c.Channel == request.Channel);

        if (conversation == null)
        {
            conversation = new AIConversation
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ExternalId = request.ExternalId,
                Channel = request.Channel,
                Status = ConversationStatus.Active
            };
            _context.AIConversations.Add(conversation);
        }

        // 2. Persist user message
        var userMessage = new AIMessage
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow
        };
        _context.AIMessages.Add(userMessage);

        // 3. Prepare AI context (Active RAG + Real-time Booking Data)
        var kbEntries = await _context.AIKnowledgeBases
            .Where(k => k.TenantId == request.TenantId && k.IsActive)
            .ToListAsync();

        var kbContext = string.Join("\n", kbEntries.Select(k => $"Q: {k.Question}\nA: {k.Answer}"));

        // Add real-time service and slot context if the message looks like a booking request
        string bookingContext = "";
        if (request.Message.Contains("book", StringComparison.OrdinalIgnoreCase) ||
            request.Message.Contains("appointment", StringComparison.OrdinalIgnoreCase) ||
            request.Message.Contains("available", StringComparison.OrdinalIgnoreCase))
        {
            var services = await _context.Services
                .Where(s => s.TenantId == request.TenantId && s.IsActive)
                .Select(s => $"{s.Name} (${s.Price}, {s.DurationMinutes}min)")
                .ToListAsync();

            bookingContext = "\nOur available services include:\n" + string.Join("\n", services) +
                             "\n\nTo check specific availability, ask the user for their preferred date and time.";
        }

        var systemContext = "Act as a helpful booking assistant for a service business. " +
                            "Use the following Information to answer the client's questions accurately:\n" +
                            $"{kbContext}\n" +
                            $"{bookingContext}\n\n" +
                            "If the answer is not in the information above, tell them you'll connect them with a human staff member. " +
                            "If they want to book, encourage them to provide a date and time so you can check availability.";

        // 4. Generate AI response
        var prompt = $"{systemContext}\n\nClient: {request.Message}\nAssistant:";
        var aiResult = await _aiService.GenerateTextAsync(request.TenantId, null, prompt);

        if (!aiResult.Success)
        {
            return new ChatResponseDto { Response = "I'm having trouble connecting right now. Please try again later.", Confidence = 0 };
        }

        var responseContent = aiResult.Content ?? "I'm sorry, I couldn't generate a response.";

        // 5. AI-driven Intent Detection & Handoff
        var intentRequest = $"Analyze the following message and categorize the user's intent: \"{request.Message}\". Categories: Booking, Pricing, Cancellation, Support, General. Return only the category name.";
        var intentResult = await _aiService.GenerateTextAsync(request.TenantId, null, intentRequest);
        var intent = intentResult.Success ? intentResult.Content?.Trim() ?? "General" : "General";

        var lowercaseResponse = responseContent.ToLowerInvariant();
        var clientMessageBase = request.Message.ToLowerInvariant();

        var handoffRequested = lowercaseResponse.Contains("human") ||
                               lowercaseResponse.Contains("staff") ||
                               clientMessageBase.Contains("speak to a person") ||
                               clientMessageBase.Contains("human") ||
                               clientMessageBase.Contains("real person");

        // 6. Persist assistant message
        var assistantMessage = new AIMessage
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = responseContent,
            TokenUsage = aiResult.InputTokens + aiResult.OutputTokens,
            Cost = aiResult.Cost,
            CreatedAt = DateTime.UtcNow
        };
        _context.AIMessages.Add(assistantMessage);

        // 7. Update conversation state & Auto-Summary
        conversation.LastMessage = responseContent;
        conversation.LastActivityAt = DateTime.UtcNow;
        conversation.Intent = intent;
        if (handoffRequested)
        {
            conversation.Status = ConversationStatus.HandoffRequested;
            conversation.HumanInteractionRequired = true;
        }

        // Generate auto-summary if enough messages
        var messageCount = await _context.AIMessages.CountAsync(m => m.ConversationId == conversation.Id);
        if (messageCount > 0 && messageCount % 5 == 0)
        {
            var history = await _context.AIMessages
                .Where(m => m.ConversationId == conversation.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .Reverse()
                .Select(m => $"{m.Role}: {m.Content}")
                .ToListAsync();

            var summaryPrompt = $"Summarize this conversation in one short sentence: \n{string.Join("\n", history)}";
            var summaryResult = await _aiService.GenerateTextAsync(request.TenantId, null, summaryPrompt);
            if (summaryResult.Success)
            {
                conversation.Summary = summaryResult.Content;
            }
        }

        await _context.SaveChangesAsync();

        // 8. Log tactical decision
        await _dashboardService.LogDecisionAsync(request.TenantId, "Chatbot", "ChatInteraction", request.Message, responseContent, 0.9m);

        return new ChatResponseDto
        {
            Response = responseContent,
            Intent = intent,
            HandoffRequested = handoffRequested,
            Confidence = 0.9m
        };
    }

    public async Task<bool> TrainKnowledgeBaseAsync(Guid tenantId, string category, string question, string answer)
    {
        var entry = new AIKnowledgeBase
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Category = category,
            Question = question,
            Answer = answer,
            IsActive = true
        };

        _context.AIKnowledgeBases.Add(entry);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AIConversation>> GetConversationsAsync(Guid tenantId)
    {
        return await _context.AIConversations
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.LastActivityAt)
            .ToListAsync();
    }

    public async Task<List<AIMessage>> GetHistoryAsync(Guid tenantId, Guid conversationId)
    {
        return await _context.AIMessages
            .Where(m => m.TenantId == tenantId && m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AIKnowledgeBase>> GetTrainingDataAsync(Guid tenantId)
    {
        return await _context.AIKnowledgeBases
            .Where(k => k.TenantId == tenantId)
            .OrderBy(k => k.Category)
            .ToListAsync();
    }
}
