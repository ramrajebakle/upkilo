using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/support-tickets")]
[Authorize]
public class SupportTicketsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SupportTicketsController> _logger;

    public SupportTicketsController(AppDbContext context, ILogger<SupportTicketsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetTenantId() => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Create a support ticket
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupportTicketCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { error = "Subject and description are required" });

        var ticket = new SupportTicket
        {
            TenantId = GetTenantId(),
            SubmittedByUserId = GetUserId(),
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            Priority = Enum.TryParse<TicketPriority>(request.Priority, true, out var priority) ? priority : TicketPriority.Normal,
            Status = TicketStatus.Open
        };

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, new
        {
            ticket.Id,
            ticket.Subject,
            ticket.Status,
            ticket.Priority,
            ticket.CreatedAt
        });
    }

    /// <summary>
    /// Get all tickets for tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = GetTenantId();
        var query = _context.SupportTickets
            .Where(t => t.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, true, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        var total = await query.CountAsync();
        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Subject,
                t.Status,
                t.Priority,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items = tickets });
    }

    /// <summary>
    /// Get ticket by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ticket = await _context.SupportTickets
            .Where(t => t.Id == id && t.TenantId == GetTenantId())
            .FirstOrDefaultAsync();

        if (ticket == null) return NotFound();
        return Ok(ticket);
    }

    /// <summary>
    /// Update ticket status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] SupportTicketUpdateStatusRequest request)
    {
        var ticket = await _context.SupportTickets
            .Where(t => t.Id == id && t.TenantId == GetTenantId())
            .FirstOrDefaultAsync();

        if (ticket == null) return NotFound();

        if (!Enum.TryParse<TicketStatus>(request.Status, true, out var status))
            return BadRequest(new { error = $"Invalid status. Valid: {string.Join(", ", Enum.GetNames(typeof(TicketStatus)))}" });

        ticket.Status = status;
        // ResolvedAt property doesn't exist in entity, removed

        await _context.SaveChangesAsync();
        return Ok(new { ticket.Id, status = ticket.Status.ToString() });
    }
}

public record SupportTicketCreateRequest(string Subject, string Description, string? Category, string? Priority);
public record SupportTicketUpdateStatusRequest(string Status);
