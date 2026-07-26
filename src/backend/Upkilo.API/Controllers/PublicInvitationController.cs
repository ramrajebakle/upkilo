using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

/// <summary>
/// Public invitation controller for anonymous access
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/public/publicinvitation")]
public class PublicInvitationController : ControllerBase
{
    private readonly ILogger<PublicInvitationController> _logger;
    private readonly AppDbContext _context;

    public PublicInvitationController(ILogger<PublicInvitationController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Get invitation details by token
    /// </summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> GetInvitation(string token)
    {
        _logger.LogInformation("Public query for invitation token: {Token}", token);

        var invitation = await _context.Invitations
            .IgnoreQueryFilters() // Bypass tenant filter for anonymous lookup
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsAccepted);

        if (invitation == null || invitation.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Invitation not found or expired for token: {Token}", token);
            return NotFound(new { message = "Invitation not found or expired." });
        }

        return Ok(new
        {
            email = invitation.Email,
            role = invitation.Role.ToString(),
            businessName = invitation.Tenant?.Name ?? "Upkilo Business",
            expiresAt = invitation.ExpiresAt
        });
    }

    /// <summary>
    /// Accept invitation and create user
    /// </summary>
    [HttpPost("accept")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        _logger.LogInformation("Attempting to accept invitation with token: {Token}", request.Token);

        var invitation = await _context.Invitations
            .IgnoreQueryFilters()
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Token == request.Token && !i.IsAccepted);

        if (invitation == null || invitation.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Acceptance failed: Invitation not found or expired for token: {Token}", request.Token);
            return BadRequest(new { message = "Invitation not found or expired." });
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters long." });
        }

        // Create User with BCrypt-hashed password
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            Email = invitation.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = invitation.Role,
            Status = UserStatus.Active,
            EmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // Create StaffMember entry if role is Staff/Manager
        var staff = new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = invitation.Email,
            Role = invitation.Role.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        invitation.IsAccepted = true;
        invitation.AcceptedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        _context.StaffMembers.Add(staff);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invitation accepted for {Email} in tenant {TenantId}", invitation.Email, invitation.TenantId);

        return Ok(new { message = "Invitation accepted successfully. You can now login." });
    }
}

public class AcceptInvitationRequest
{
    public string Token { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
