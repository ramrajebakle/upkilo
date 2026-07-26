using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ISupportTicketService
{
    Task<SupportTicket> CreateTicketAsync(SupportTicket ticket);
    Task<SupportTicket?> GetTicketAsync(Guid id, Guid tenantId);
    Task<IEnumerable<SupportTicket>> GetTenantTicketsAsync(Guid tenantId);
    Task AddCommentAsync(Guid ticketId, SupportTicketComment comment);
    Task UpdateStatusAsync(Guid ticketId, TicketStatus status);
}
