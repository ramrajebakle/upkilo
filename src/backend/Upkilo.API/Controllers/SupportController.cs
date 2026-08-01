using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SupportController> _logger;

    public SupportController(AppDbContext context, ITenantProvider tenantProvider, ILogger<SupportController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() ?? throw new UnauthorizedAccessException();

    private Guid GetUserId()
    {
        var idClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return idClaim != null ? Guid.Parse(idClaim) : Guid.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        var ticket = new SupportTicket
        {
            TenantId = tenantId,
            SubmittedByUserId = userId,
            Subject = request.Subject,
            Description = request.Description,
            Priority = Enum.TryParse<TicketPriority>(request.Priority, true, out var p) ? p : TicketPriority.Normal,
            Status = TicketStatus.Open
        };

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Support ticket {TicketId} created by user {UserId}", ticket.Id, userId);

        return Ok(new { success = true, ticketId = ticket.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets()
    {
        var tenantId = GetTenantId();
        var tickets = await _context.SupportTickets
            .Include(t => t.SubmittedByUser)
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Subject,
                t.Status,
                t.Priority,
                t.CreatedAt,
                submittedBy = t.SubmittedByUser != null ? t.SubmittedByUser.FirstName + " " + t.SubmittedByUser.LastName : "Unknown"
            })
            .ToListAsync();

        return Ok(new { data = tickets });
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Owner,Admin")] // Only tenant admins/owners can update status or SuperAdmin
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTicketStatusRequest request)
    {
        var tenantId = GetTenantId();
        var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);

        if (ticket == null) return NotFound();

        if (Enum.TryParse<TicketStatus>(request.Status, true, out var status))
        {
            ticket.Status = status;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, status = ticket.Status.ToString() });
        }

        return BadRequest("Invalid status");
    }
}

public class CreateTicketRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
}

public class UpdateTicketStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
