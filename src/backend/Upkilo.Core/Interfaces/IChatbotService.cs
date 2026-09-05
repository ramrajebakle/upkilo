using System;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IChatbotService
{
    Task<ChatResponseDto> ProcessMessageAsync(ChatRequestDto request);
    /// <summary>
    /// Adds a knowledge base entry and returns the persisted row.
    ///
    /// Returns the entity rather than a bool because the caller needs the server-assigned id: the
    /// admin page appends the result straight into its list, and a bare "true" left it rendering a
    /// blank card with an undefined React key that vanished on the next refresh.
    /// </summary>
    Task<AIKnowledgeBase> TrainKnowledgeBaseAsync(Guid tenantId, string category, string question, string answer);
    Task<List<AIConversation>> GetConversationsAsync(Guid tenantId);
    Task<List<AIMessage>> GetHistoryAsync(Guid tenantId, Guid conversationId);
    Task<List<AIKnowledgeBase>> GetTrainingDataAsync(Guid tenantId);
}

public class ChatRequestDto
{
    public Guid TenantId { get; set; }
    public string ExternalId { get; set; } = string.Empty; // Channel ID
    public ConversationChannel Channel { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Who is asking. Decides whether Upkilo platform knowledge is in scope for this turn, so it
    /// defaults to the closed option: a caller that forgets to set it gets the public visitor's
    /// restricted view rather than staff-level knowledge.
    /// </summary>
    public ChatAudience Audience { get; set; } = ChatAudience.PublicVisitor;
}

public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public bool HandoffRequested { get; set; }
    public decimal Confidence { get; set; }
}
