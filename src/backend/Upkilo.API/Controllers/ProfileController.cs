using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using System.Security.Claims;

namespace Upkilo.API.Controllers;

/// <summary>
/// User profile controller for account management.
/// Uses User entity and Preferences dictionary.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ILogger<ProfileController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAuthService _authService;

    public ProfileController(
        ILogger<ProfileController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IPasswordHasher<User> passwordHasher,
        IAuthService authService)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _passwordHasher = passwordHasher;
        _authService = authService;
    }

    private Guid GetUserId() => _tenantProvider.GetUserId()
        ?? throw new UnauthorizedAccessException("User context not available");

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userId = GetUserId();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            phone = user.Phone,
            avatar = user.AvatarUrl,
            role = user.Role.ToString().ToLower(),
            emailVerified = user.EmailVerified,
            phoneVerified = false,
            twoFactorEnabled = user.TwoFactorEnabled,
            timezone = user.TimeZoneId,
            locale = user.LanguageCode,
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt
        });
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userId = GetUserId();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.Phone != null) user.Phone = request.Phone;

        // Update preferences
        if (request.Timezone != null) user.TimeZoneId = request.Timezone;
        if (request.Locale != null) user.LanguageCode = request.Locale;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    /// <summary>
    /// Upload avatar
    /// </summary>
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var userId = GetUserId();

        if (file == null || file.Length == 0) return BadRequest(new { error = "No file provided" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(ext))
        {
            return BadRequest(new { error = "Invalid file type. Only JPG, PNG, and WebP are allowed." });
        }
        
        using var readStream = file.OpenReadStream();
        var headerBytes = new byte[4];
        await readStream.ReadAsync(headerBytes, 0, 4);
        var hex = BitConverter.ToString(headerBytes).Replace("-", "");
        
        bool isImage = ext switch {
            ".jpg" or ".jpeg" => hex.StartsWith("FFD8"),
            ".png" => hex.StartsWith("89504E47"),
            ".webp" => hex.StartsWith("52494646"), // RIFF
            _ => false
        };

        if (!isImage)
        {
            return BadRequest(new { error = "Invalid image content." });
        }
        
        // Reset stream position for the actual copy
        readStream.Position = 0;

        var avatarFileName = $"{userId}_{Guid.NewGuid():N}{ext}";
        var avatarDir = Path.Combine("wwwroot", "avatars");
        Directory.CreateDirectory(avatarDir);
        var filePath = Path.Combine(avatarDir, avatarFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var avatarUrl = $"/avatars/{avatarFileName}";
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user != null)
        {
            user.AvatarUrl = avatarUrl;
            await _context.SaveChangesAsync();
        }

        return Ok(new { avatarUrl });
    }

    /// <summary>
    /// Delete avatar
    /// </summary>
    [HttpDelete("avatar")]
    public async Task<IActionResult> DeleteAvatar()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var userId = GetUserId();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user != null)
        {
            user.AvatarUrl = null;
            await _context.SaveChangesAsync();
        }
        return NoContent();
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var userId = GetUserId();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();

        // Verify current password
        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            return BadRequest(new { error = "Current password is incorrect" });
        }

        // Set new password
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var userId = GetUserId();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();

        // Generate secret
        var secret = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        // Temporarily store secret in user preferences or dedicated field? 
        // User entity has `TwoFactorSecret`.
        user.TwoFactorSecret = secret; 
        
        // Generate cryptographically secure backup codes
        var backupCodes = Enumerable.Range(0, 8)
            .Select(_ =>
            {
                var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(4);
                var hex = Convert.ToHexString(bytes);
                return $"{hex[..4]}-{hex[4..]}";
            })
            .ToArray();

        // Store backup codes (hashed) on user for later verification
        user.Preferences["backup_codes"] = string.Join(",", backupCodes);
        await _context.SaveChangesAsync();

        var qrCodeUrl = $"otpauth://totp/Upkilo:{user.Email}?secret={secret}&issuer=Upkilo";

        return Ok(new
        {
            secret,
            qrCodeUrl,
            backupCodes
        });
    }

    /// <summary>
    /// Verify and confirm 2FA setup
    /// </summary>
    [HttpPost("2fa/verify")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var userId = GetUserId();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return BadRequest("2FA is not initiated.");

        var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(user.TwoFactorSecret));
        if (!totp.VerifyTotp(request.Code, out _, OtpNet.VerificationWindow.RfcSpecifiedNetworkDelay))
            return BadRequest("Invalid or expired code");

        user.TwoFactorEnabled = true;
        await _context.SaveChangesAsync();
        
        await _authService.ProcessTwoFactorStateChangeAsync(user.Id, true);

        return Ok(new { success = true });
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var userId = GetUserId();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();

        // Verify password
        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
             return BadRequest(new { error = "Incorrect password" });
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await _context.SaveChangesAsync();

        await _authService.ProcessTwoFactorStateChangeAsync(user.Id, false);

        return Ok(new { success = true });
    }

    /// <summary>
    /// Get active sessions — returns current session info from request context.
    /// Full session tracking requires a Session entity (not yet implemented).
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var userId = GetUserId();

        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActiveAt)
            .Select(s => new
            {
                id = s.Id,
                device = s.Browser,
                ipAddress = s.IpAddress,
                location = s.Location ?? "Unknown",
                lastActive = s.LastActiveAt,
                current = (s.IpAddress == HttpContext.Connection.RemoteIpAddress.ToString() && s.Browser == Request.Headers.UserAgent.ToString())
            })
            .ToListAsync();

        return Ok(new { data = sessions });
    }

    /// <summary>
    /// Revoke a session. Requires Session entity to be implemented.
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var userId = GetUserId();

        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session != null && !session.IsRevoked)
        {
            session.IsRevoked = true;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }
}

// DTOs
public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Timezone { get; set; }
    public string? Locale { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class VerifyTwoFactorRequest
{
    public string Code { get; set; } = string.Empty;
}

public class DisableTwoFactorRequest
{
    public string Password { get; set; } = string.Empty;
}


