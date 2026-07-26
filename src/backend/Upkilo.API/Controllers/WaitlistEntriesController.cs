using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class WaitlistEntriesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public WaitlistEntriesController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetWaitlist([FromQuery] Guid? serviceId, [FromQuery] Guid? staffId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.WaitlistEntries
            .Include(w => w.Client)
            .Include(w => w.Service)
            .Where(w => w.TenantId == tenantId && w.Status == WaitlistStatus.Pending) // Show only pending
            .AsQueryable();

        if (serviceId.HasValue) query = query.Where(w => w.ServiceId == serviceId.Value);
        if (staffId.HasValue) query = query.Where(w => w.StaffId == staffId.Value);

        var entries = await query.OrderBy(w => w.Priority).ThenBy(w => w.CreatedAt).ToListAsync();
        return Ok(entries);
    }

    [HttpGet("summary")]
    [Authorize]
    public async Task<IActionResult> GetWaitlistSummary()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var entries = await _context.WaitlistEntries
            .Where(w => w.TenantId == tenantId)
            .ToListAsync();

        return Ok(new
        {
            total = entries.Count,
            pending = entries.Count(e => e.Status == WaitlistStatus.Pending || e.Status == WaitlistStatus.Waiting),
            notified = entries.Count(e => e.Status == WaitlistStatus.Notified),
            converted = entries.Count(e => e.Status == WaitlistStatus.Converted || e.Status == WaitlistStatus.Booked),
            expired = entries.Count(e => e.Status == WaitlistStatus.Expired || e.Status == WaitlistStatus.Cancelled),
        });
    }

    [HttpPost("{id}/notify")]
    [Authorize]
    public async Task<IActionResult> NotifyEntry(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var entry = await _context.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);
        if (entry == null) return NotFound();

        entry.Status = WaitlistStatus.Notified;
        entry.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, status = WaitlistStatus.Notified.ToString() });
    }

    [HttpPost("{id}/convert")]
    [Authorize]
    public async Task<IActionResult> ConvertToBooking(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var entry = await _context.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);
        if (entry == null) return NotFound();

        entry.Status = WaitlistStatus.Converted;
        entry.IsConverted = true;
        entry.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, status = WaitlistStatus.Converted.ToString() });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> RemoveEntry(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var entry = await _context.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);
        if (entry == null) return NotFound();

        entry.Status = WaitlistStatus.Cancelled;
        entry.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    [AllowAnonymous] // Public clients can join waitlist from booking widgets
    public async Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return BadRequest("Missing Tenant context");

        var maxPriority = await _context.WaitlistEntries
            .Where(w => w.TenantId == tenantId && w.ServiceId == request.ServiceId)
            .MaxAsync(w => (int?)w.Priority) ?? 0;

        var entry = new WaitlistEntry
        {
            TenantId = tenantId.Value,
            ServiceId = request.ServiceId,
            StaffId = request.StaffId,
            ClientId = request.ClientId, // Might be null if guest, need robust guest handling but keeping simple here
            Email = request.Email ?? string.Empty,
            FirstName = request.FirstName ?? string.Empty,
            LastName = request.LastName ?? string.Empty,
            Phone = request.Phone,
            RequestedDate = request.RequestedDate ?? DateTime.UtcNow,
            PreferredDate = request.RequestedDate ?? DateTime.UtcNow,
            Status = WaitlistStatus.Pending,
            Notes = request.Notes
        };

        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWaitlist), new { id = entry.Id }, entry);
    }
}

public class JoinWaitlistRequest
{
    public Guid ServiceId { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? ClientId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public DateTime? RequestedDate { get; set; }
    public string? Notes { get; set; }
}
