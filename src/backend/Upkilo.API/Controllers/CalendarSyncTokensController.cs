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
[Authorize(Roles = "Staff,Admin,Owner")]
public class CalendarSyncTokensController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CalendarSyncTokensController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetTokens()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        var staffMember = await _context.StaffMembers.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);
        if (staffMember == null) return Unauthorized("Linked staff member not found.");

        var tokens = await _context.CalendarSyncTokens
            .Where(t => t.TenantId == tenantId && t.StaffId == staffMember.Id)
            .ToListAsync();
            
        // Never return access/refresh tokens to the client directly, mask them
        var masked = tokens.Select(t => new {
            t.Id,
            t.Provider,
            t.ExternalAccountId,
            t.SyncDirection,
            t.LastSyncAt,
            t.IsActive,
            Status = t.IsActive ? "Connected" : "Disconnected"
        });

        return Ok(masked);
    }

    [HttpPost]
    public async Task<IActionResult> SaveToken([FromBody] SaveTokenRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var staffMember = await _context.StaffMembers.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);
        if (staffMember == null) return Unauthorized();

        var token = new CalendarSyncToken
        {
            TenantId = tenantId.Value,
            StaffId = staffMember.Id,
            Provider = request.Provider, // e.g., "Google", "Outlook"
            ExternalAccountId = request.ExternalAccountId,
            AccessToken = request.AccessToken,
            RefreshToken = request.RefreshToken,
            ExpiresAt = request.ExpiresAt,
            SyncDirection = request.SyncDirection,
            IsActive = true
        };

        _context.CalendarSyncTokens.Add(token);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Disconnect(Guid id)
    {
        var token = await _context.CalendarSyncTokens.FindAsync(id);
        if (token == null) return NotFound();

        _context.CalendarSyncTokens.Remove(token);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }
}

public class SaveTokenRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ExternalAccountId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string SyncDirection { get; set; } = "TwoWay"; // OneWayUp, OneWayDown
}
