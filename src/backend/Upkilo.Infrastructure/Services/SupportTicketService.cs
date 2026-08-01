using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for managing support tickets with tenant isolation and SLA tracking.
/// </summary>
public class SupportTicketService : ISupportTicketService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SupportTicketService> _logger;
    private readonly IEmailService _emailService;

    public SupportTicketService(
        AppDbContext context,
        IEmailService emailService,
        ILogger<SupportTicketService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<SupportTicket> CreateTicketAsync(SupportTicket ticket)
    {
        ticket.Id = Guid.NewGuid();
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.Status = TicketStatus.Open;

        // Default SLR: 24 hours for normal, 4 hours for high priority
        ticket.SlaExpiresAt = ticket.Priority == TicketPriority.High || ticket.Priority == TicketPriority.Urgent
            ? DateTime.UtcNow.AddHours(4)
            : DateTime.UtcNow.AddHours(24);

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created support ticket {TicketId} for tenant {TenantId}. Priority: {Priority}",
            ticket.Id, ticket.TenantId, ticket.Priority);

        // Notify staff (In a real app, this would use a notification service or SignalR)
        await _emailService.SendSystemEmailAsync(
            "support@upkilo.com",
            $"New {ticket.Priority} Ticket: {ticket.Subject}",
            $"A new support ticket has been opened by {ticket.ContactEmail}.<br/>Subject: {ticket.Subject}");

        return ticket;
    }

    public async Task<SupportTicket?> GetTicketAsync(Guid id, Guid tenantId)
    {
        return await _context.SupportTickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
    }

    public async Task<IEnumerable<SupportTicket>> GetTenantTicketsAsync(Guid tenantId)
    {
        return await _context.SupportTickets
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task AddCommentAsync(Guid ticketId, SupportTicketComment comment)
    {
        var ticket = await _context.SupportTickets.FindAsync(ticketId);
        if (ticket == null) throw new KeyNotFoundException("Ticket not found");

        comment.Id = Guid.NewGuid();
        comment.CreatedAt = DateTime.UtcNow;
        comment.TicketId = ticketId;

        _context.SupportTicketComments.Add(comment);

        // Update ticket last activity
        ticket.UpdatedAt = DateTime.UtcNow;

        if (comment.AuthorUserId == Guid.Empty) // Staff comment (pseudo-logic for now)
        {
            ticket.Status = TicketStatus.InProgress;

            // Notify user
            await _emailService.SendSystemEmailAsync(
                ticket.ContactEmail,
                $"Re: {ticket.Subject} - Support Update",
                $"An agent has replied to your ticket: <br/><br/>{comment.Content}");
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid ticketId, TicketStatus status)
    {
        var ticket = await _context.SupportTickets.FindAsync(ticketId);
        if (ticket == null) return;

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (status == TicketStatus.Resolved || status == TicketStatus.Closed)
        {
            ticket.ResolvedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
