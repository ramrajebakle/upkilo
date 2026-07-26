using System;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IChatbotService
{
    Task<ChatResponseDto> ProcessMessageAsync(ChatRequestDto request);
    Task<bool> TrainKnowledgeBaseAsync(Guid tenantId, string category, string question, string answer);
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
}

public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public bool HandoffRequested { get; set; }
    public decimal Confidence { get; set; }
}
