using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using System.Security.Claims;

namespace Upkilo.API.Controllers;

/// <summary>
/// Team invitation management controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class InvitationController : ControllerBase
{
    private readonly ILogger<InvitationController> _logger;
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ITenantProvider _tenantProvider;

    public InvitationController(
        ILogger<InvitationController> logger, 
        AppDbContext context, 
        IEmailService emailService,
        IConfiguration configuration,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all pending invitations for the current tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetInvitations()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var invitations = await _context.Invitations
            .Where(i => i.TenantId == tenantId && !i.IsAccepted)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return Ok(new { data = invitations });
    }

    /// <summary>
    /// Create and send a team invitation
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest request)
    {
        _logger.LogInformation("Attempting to create invitation for {Email} with role {Role}", request.Email, request.Role);
        
        // Parse role safely
        if (!Enum.TryParse<UserRole>(request.Role, true, out var userRole))
        {
            userRole = UserRole.Staff; // Default or return error
            _logger.LogWarning("Invalid role {Role} provided, defaulting to Staff", request.Role);
        }

        // 1. Check if user already exists
        var existingUser = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (existingUser)
        {
            return BadRequest(new { message = "User with this email already exists in the system." });
        }

        // 2. Check for existing pending invitation
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var existingInvite = await _context.Invitations.FirstOrDefaultAsync(i => i.Email == request.Email && i.TenantId == tenantId && !i.IsAccepted);
        if (existingInvite != null)
        {
            return BadRequest(new { message = "An active invitation already exists for this email in your tenant." });
        }

        // 3. Get tenant info for branding
        var tenantInfo = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value);
        if (tenantInfo == null) return Unauthorized();

        // 4. Create Invitation
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var token = Guid.NewGuid().ToString("N");
        
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Email = request.Email,
            Role = userRole,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            InvitedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();

        // 5. Send Email
        var invitationLink = $"{_configuration["App:FrontendUrl"]}/invite/{token}";
        await _emailService.SendTeamInvitationAsync(request.Email, tenantInfo.Name, invitationLink);

        _logger.LogInformation("Invitation created for {Email} by {UserId}", request.Email, currentUserId);

        return Ok(new { message = "Invitation sent successfully", data = invitation });
    }

    /// <summary>
    /// Cancel/Remove a pending invitation
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelInvitation(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var invitation = await _context.Invitations.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (invitation == null) return NotFound();
        if (invitation.IsAccepted) return BadRequest(new { message = "Cannot cancel an accepted invitation." });

        _context.Invitations.Remove(invitation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invitation {InvitationId} cancelled", id);
        return NoContent();
    }
}

public class CreateInvitationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
}

