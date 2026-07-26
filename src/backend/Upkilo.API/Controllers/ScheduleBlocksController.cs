using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Schedule Blocks controller for managing staff unavailability and time-off blocks.
/// Uses real database queries against ScheduleBlock entity.
/// </summary>
[ApiController]
[Route("api/schedule-blocks")]
[Authorize]
public class ScheduleBlocksController : ControllerBase
{
    private readonly ILogger<ScheduleBlocksController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ScheduleBlocksController(
        ILogger<ScheduleBlocksController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all schedule blocks
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBlocks(
        [FromQuery] Guid? staffId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.ScheduleBlocks
            .Include(sb => sb.Staff)
            .Where(sb => sb.TenantId == tenantId.Value && !sb.IsDeleted);

        if (staffId.HasValue)
            query = query.Where(sb => sb.StaffId == staffId.Value);
        if (startDate.HasValue)
            query = query.Where(sb => sb.EndDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(sb => sb.StartDate <= endDate.Value);

        var blocks = await query
            .OrderByDescending(sb => sb.StartDate)
            .Select(sb => new
            {
                sb.Id,
                sb.StaffId,
                staffName = sb.Staff != null ? $"{sb.Staff.FirstName} {sb.Staff.LastName}" : "Unknown",
                sb.Type,
                sb.Title,
                startDate = sb.StartDate.ToString("yyyy-MM-dd"),
                endDate = sb.EndDate.ToString("yyyy-MM-dd"),
                sb.AllDay,
                sb.StartTime,
                sb.EndTime,
                sb.Reason,
                sb.Status,
                sb.CreatedAt
            })
            .ToListAsync();

        var total = blocks.Count;
        return Ok(new { data = blocks, total });
    }

    /// <summary>
    /// Create schedule block
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBlock([FromBody] CreateScheduleBlockRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (!DateTime.TryParse(request.StartDate, out var start) || !DateTime.TryParse(request.EndDate, out var end))
            return BadRequest(new { error = "Invalid date format." });

        var block = new ScheduleBlock
        {
            TenantId = tenantId.Value,
            StaffId = request.StaffId,
            Type = request.Type,
            Title = request.Title,
            StartDate = start,
            EndDate = end,
            AllDay = request.AllDay,
            StartTime = !string.IsNullOrEmpty(request.StartTime) ? TimeSpan.Parse(request.StartTime) : null,
            EndTime = !string.IsNullOrEmpty(request.EndTime) ? TimeSpan.Parse(request.EndTime) : null,
            Reason = request.Reason,
            Status = request.RequiresApproval ? "pending" : "approved"
        };

        _context.ScheduleBlocks.Add(block);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule block created for staff: {StaffId}", request.StaffId);

        return Created($"/api/schedule-blocks/{block.Id}", new
        {
            block.Id,
            block.Status,
            block.CreatedAt
        });
    }

    /// <summary>
    /// Update schedule block
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBlock(Guid id, [FromBody] UpdateScheduleBlockRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var block = await _context.ScheduleBlocks
            .FirstOrDefaultAsync(sb => sb.Id == id && sb.TenantId == tenantId.Value && !sb.IsDeleted);

        if (block == null) return NotFound();

        if (request.Title != null) block.Title = request.Title;
        if (request.Reason != null) block.Reason = request.Reason;
        if (request.StartDate != null && DateTime.TryParse(request.StartDate, out var s)) block.StartDate = s;
        if (request.EndDate != null && DateTime.TryParse(request.EndDate, out var e)) block.EndDate = e;
        if (request.StartTime != null) block.StartTime = TimeSpan.Parse(request.StartTime);
        if (request.EndTime != null) block.EndTime = TimeSpan.Parse(request.EndTime);
        block.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule block updated: {BlockId}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Delete schedule block (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBlock(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var block = await _context.ScheduleBlocks
            .FirstOrDefaultAsync(sb => sb.Id == id && sb.TenantId == tenantId.Value && !sb.IsDeleted);

        if (block == null) return NotFound();

        block.IsDeleted = true;
        block.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule block deleted: {BlockId}", id);
        return NoContent();
    }

    /// <summary>
    /// Approve time-off request
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveBlock(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var block = await _context.ScheduleBlocks
            .FirstOrDefaultAsync(sb => sb.Id == id && sb.TenantId == tenantId.Value && !sb.IsDeleted);

        if (block == null) return NotFound();

        block.Status = "approved";
        block.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule block approved: {BlockId}", id);
        return Ok(new { success = true, status = "approved" });
    }

    /// <summary>
    /// Reject time-off request
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectBlock(Guid id, [FromBody] RejectBlockRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var block = await _context.ScheduleBlocks
            .FirstOrDefaultAsync(sb => sb.Id == id && sb.TenantId == tenantId.Value && !sb.IsDeleted);

        if (block == null) return NotFound();

        block.Status = "rejected";
        block.RejectionReason = request.Reason;
        block.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule block rejected: {BlockId}", id);
        return Ok(new { success = true, status = "rejected" });
    }

    /// <summary>
    /// Get pending approvals
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingApprovals()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var pending = await _context.ScheduleBlocks
            .Include(sb => sb.Staff)
            .Where(sb => sb.TenantId == tenantId.Value && sb.Status == "pending" && !sb.IsDeleted)
            .OrderBy(sb => sb.StartDate)
            .Select(sb => new
            {
                sb.Id,
                sb.StaffId,
                staffName = sb.Staff != null ? $"{sb.Staff.FirstName} {sb.Staff.LastName}" : "Unknown",
                sb.Type,
                sb.Title,
                startDate = sb.StartDate.ToString("yyyy-MM-dd"),
                endDate = sb.EndDate.ToString("yyyy-MM-dd"),
                sb.Reason,
                requestedAt = sb.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = pending, total = pending.Count });
    }

    /// <summary>
    /// Get staff availability summary for a date
    /// </summary>
    [HttpGet("availability-summary")]
    public async Task<IActionResult> GetAvailabilitySummary([FromQuery] string date)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (!DateTime.TryParse(date, out var targetDate))
            return BadRequest(new { error = "Invalid date." });

        // Get all staff
        var allStaff = await _context.StaffMembers
            .Where(s => s.TenantId == tenantId.Value && !s.IsDeleted && s.IsActive)
            .Select(s => new { s.Id, Name = $"{s.FirstName} {s.LastName}" })
            .ToListAsync();

        // Get blocks on that date
        var blockedStaffIds = await _context.ScheduleBlocks
            .Where(sb => sb.TenantId == tenantId.Value && sb.Status == "approved" && !sb.IsDeleted &&
                sb.StartDate <= targetDate && sb.EndDate >= targetDate)
            .Select(sb => sb.StaffId)
            .Distinct()
            .ToListAsync();

        var summary = allStaff.Select(s => new
        {
            staffName = s.Name,
            available = !blockedStaffIds.Contains(s.Id),
            hoursAvailable = blockedStaffIds.Contains(s.Id) ? 0 : 8
        }).ToList();

        return Ok(new { date, summary });
    }
}

// Request DTOs
public class CreateScheduleBlockRequest
{
    public Guid StaffId { get; set; }
    public string Type { get; set; } = "time_off"; // time_off, break, personal
    public string Title { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public bool AllDay { get; set; } = true;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Reason { get; set; }
    public bool RequiresApproval { get; set; } = true;
}

public class UpdateScheduleBlockRequest
{
    public string? Title { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Reason { get; set; }
}

public class RejectBlockRequest
{
    public string Reason { get; set; } = string.Empty;
}
