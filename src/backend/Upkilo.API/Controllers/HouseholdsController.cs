using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Households controller for managing family units
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class HouseholdsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<HouseholdsController> _logger;

    public HouseholdsController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<HouseholdsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all households
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHouseholds([FromQuery] bool? isActive = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Set<Household>()
            .Include(h => h.PrimaryClient)
            .Include(h => h.Members)
            .Where(h => h.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(h => h.IsActive == isActive.Value);

        var households = await query
            .OrderBy(h => h.Name)
            .Select(h => new
            {
                h.Id,
                h.Name,
                PrimaryClient = new
                {
                    h.PrimaryClient!.Id,
                    h.PrimaryClient.FirstName,
                    h.PrimaryClient.LastName,
                    h.PrimaryClient.Email,
                    h.PrimaryClient.Phone
                },
                MemberCount = h.Members.Count,
                h.SharedBilling,
                h.IsActive,
                h.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = households });
    }

    /// <summary>
    /// Get household details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetHousehold(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var household = await _context.Set<Household>()
            .Include(h => h.PrimaryClient)
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId);

        if (household == null) return NotFound();

        return Ok(new
        {
            household.Id,
            household.Name,
            household.PrimaryClientId,
            PrimaryClient = new
            {
                household.PrimaryClient!.Id,
                household.PrimaryClient.FirstName,
                household.PrimaryClient.LastName,
                household.PrimaryClient.Email,
                household.PrimaryClient.Phone
            },
            Members = household.Members.Select(m => new
            {
                m.Id,
                m.FirstName,
                m.LastName,
                m.Email,
                m.Phone,
                m.DateOfBirth
            }).ToList(),
            household.BillingAddress,
            household.Notes,
            household.SharedBilling,
            household.IsActive,
            household.CreatedAt,
            household.UpdatedAt
        });
    }

    /// <summary>
    /// Create household
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateHousehold([FromBody] CreateHouseholdRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify primary client exists
        var primaryClient = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == request.PrimaryClientId && c.TenantId == tenantId);

        if (primaryClient == null)
            return NotFound("Primary client not found");

        // Check if primary client already in a household
        if (primaryClient.HouseholdId.HasValue)
            return BadRequest("Primary client is already a member of a household");

        var household = new Household
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            PrimaryClientId = request.PrimaryClientId,
            BillingAddress = request.BillingAddress,
            Notes = request.Notes,
            SharedBilling = request.SharedBilling ?? true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<Household>().Add(household);

        // Link primary client
        primaryClient.HouseholdId = household.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created household {HouseholdId} with primary client {ClientId}",
            household.Id, request.PrimaryClientId);

        return CreatedAtAction(nameof(GetHousehold), new { id = household.Id }, household);
    }

    /// <summary>
    /// Update household
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHousehold(Guid id, [FromBody] UpdateHouseholdRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var household = await _context.Set<Household>()
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId);

        if (household == null) return NotFound();

        if (request.Name != null) household.Name = request.Name;
        if (request.BillingAddress != null) household.BillingAddress = request.BillingAddress;
        if (request.Notes != null) household.Notes = request.Notes;
        if (request.SharedBilling.HasValue) household.SharedBilling = request.SharedBilling.Value;
        if (request.IsActive.HasValue) household.IsActive = request.IsActive.Value;

        household.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(household);
    }

    /// <summary>
    /// Add member to household
    /// </summary>
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var household = await _context.Set<Household>()
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId);

        if (household == null) return NotFound("Household not found");

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == request.ClientId && c.TenantId == tenantId);

        if (client == null) return NotFound("Client not found");

        if (client.HouseholdId.HasValue)
            return BadRequest("Client is already a member of a household");

        client.HouseholdId = id;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added client {ClientId} to household {HouseholdId}", request.ClientId, id);

        return Ok(new { message = "Member added successfully", clientId = request.ClientId });
    }

    /// <summary>
    /// Remove member from household
    /// </summary>
    [HttpDelete("{id}/members/{clientId}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var household = await _context.Set<Household>()
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId);

        if (household == null) return NotFound("Household not found");

        // Can't remove primary client
        if (household.PrimaryClientId == clientId)
            return BadRequest("Cannot remove primary client from household");

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.HouseholdId == id);

        if (client == null) return NotFound("Client not found in this household");

        client.HouseholdId = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed client {ClientId} from household {HouseholdId}", clientId, id);

        return NoContent();
    }

    /// <summary>
    /// Delete household
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHousehold(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var household = await _context.Set<Household>()
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId);

        if (household == null) return NotFound();

        // Unlink all members
        foreach (var member in household.Members)
        {
            member.HouseholdId = null;
        }

        _context.Set<Household>().Remove(household);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted household {HouseholdId}", id);

        return NoContent();
    }

    /// <summary>
    /// Get household bookings
    /// </summary>
    [HttpGet("{id}/bookings")]
    public async Task<IActionResult> GetHouseholdBookings(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var household = await _context.Set<Household>()
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId);

        if (household == null) return NotFound();

        var memberIds = household.Members.Select(m => m.Id).ToList();

        var bookings = await _context.Bookings
            .Where(b => b.ClientId.HasValue && memberIds.Contains(b.ClientId.Value) && b.TenantId == tenantId)
            .OrderByDescending(b => b.StartTime)
            .Take(50)
            .Select(b => new
            {
                b.Id,
                b.ClientId,
                ClientName = b.Client != null ? $"{b.Client.FirstName} {b.Client.LastName}" : "Unknown",
                b.Status,
                b.StartTime,
                b.EndTime,
                b.Price
            })
            .ToListAsync();

        return Ok(new { data = bookings });
    }
}

// DTOs
public record CreateHouseholdRequest(
    string Name,
    Guid PrimaryClientId,
    string? BillingAddress = null,
    string? Notes = null,
    bool? SharedBilling = true
);

public record UpdateHouseholdRequest(
    string? Name = null,
    string? BillingAddress = null,
    string? Notes = null,
    bool? SharedBilling = null,
    bool? IsActive = null
);

public record AddMemberRequest(
    Guid ClientId
);

