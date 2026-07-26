using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/auth/2fa")]
[Authorize]
public class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorService _twoFactorService;
    private readonly AppDbContext _context;
    private readonly ILogger<TwoFactorController> _logger;

    public TwoFactorController(ITwoFactorService twoFactorService, AppDbContext context, ILogger<TwoFactorController> logger)
    {
        _twoFactorService = twoFactorService;
        _context = context;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Start 2FA setup - generates TOTP secret and QR code
    /// </summary>
    [HttpPost("setup")]
    public async Task<IActionResult> Setup()
    {
        var result = await _twoFactorService.SetupTotpAsync(GetUserId());
        return Ok(new
        {
            qrCodeUri = result.QrCodeUri,
            manualEntryKey = result.ManualEntryKey,
            message = "Scan the QR code with your authenticator app, then verify with a code"
        });
    }

    /// <summary>
    /// Verify TOTP code and enable 2FA
    /// </summary>
    [HttpPost("enable")]
    public async Task<IActionResult> Enable([FromBody] TwoFactorVerifyRequest request)
    {
        var userId = GetUserId();
        var success = await _twoFactorService.EnableTwoFactorAsync(userId, request.Code);
        if (!success)
            return BadRequest(new { error = "Invalid verification code" });

        // Generate and return backup codes immediately upon enabling
        var backupCodes = await _twoFactorService.GenerateBackupCodesAsync(userId);

        return Ok(new 
        { 
            message = "Two-factor authentication has been enabled",
            backupCodes = backupCodes,
            backupCodesMessage = "Save these backup codes securely. Each code can only be used once."
        });
    }

    /// <summary>
    /// Disable 2FA
    /// </summary>
    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] TwoFactorVerifyRequest request)
    {
        var verified = await _twoFactorService.VerifyTotpAsync(GetUserId(), request.Code);
        if (!verified)
            return BadRequest(new { error = "Invalid verification code" });

        await _twoFactorService.DisableTwoFactorAsync(GetUserId());
        return Ok(new { message = "Two-factor authentication has been disabled" });
    }

    /// <summary>
    /// Generate new backup codes
    /// </summary>
    [HttpPost("backup-codes")]
    public async Task<IActionResult> GenerateBackupCodes([FromBody] TwoFactorVerifyRequest request)
    {
        var verified = await _twoFactorService.VerifyTotpAsync(GetUserId(), request.Code);
        if (!verified)
            return BadRequest(new { error = "Invalid verification code" });

        var codes = await _twoFactorService.GenerateBackupCodesAsync(GetUserId());
        return Ok(new
        {
            backupCodes = codes,
            message = "Save these codes securely. Each code can only be used once."
        });
    }

    /// <summary>
    /// Check if 2FA is enabled
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var isEnabled = await _twoFactorService.IsTwoFactorEnabledAsync(GetUserId());
        return Ok(new { enabled = isEnabled });
    }

    /// <summary>
    /// Use a backup code to recover account access
    /// </summary>
    [HttpPost("recover")]
    [AllowAnonymous]
    public async Task<IActionResult> RecoverAccount([FromBody] TwoFactorVerifyRequest request, [FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { error = "Email parameter is required." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
        if (user == null)
            return BadRequest(new { error = "User not found or inactive." });

        var success = await _twoFactorService.VerifyBackupCodeAsync(user.Id, request.Code);
        if (!success) return BadRequest(new { error = "Invalid or expired recovery code." });

        // Disable 2FA so they can login and re-configure it
        await _twoFactorService.DisableTwoFactorAsync(user.Id);
        _logger.LogWarning("Two-factor authentication disabled via recovery code for user: {UserId}", user.Id);

        return Ok(new { message = "Recovery successful. Two-factor authentication has been disabled. You can now reset your password or login." });
    }
    /// <summary>
    /// Reset 2FA setting for the current user (requires verification of existing 2FA or backup code)
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetTwoFactor([FromBody] TwoFactorVerifyRequest request)
    {
        var userId = GetUserId();
        var verified = await _twoFactorService.VerifyTotpAsync(userId, request.Code) || 
                       await _twoFactorService.VerifyBackupCodeAsync(userId, request.Code);
        
        if (!verified)
            return BadRequest(new { error = "Invalid code. Please provide a valid TOTP or backup code to reset 2FA." });

        await _twoFactorService.DisableTwoFactorAsync(userId);
        _logger.LogWarning("Two-factor authentication reset for user: {UserId}", userId);
        
        return Ok(new { message = "Two-factor authentication has been reset. You can now set it up again." });
    }
}

public record TwoFactorVerifyRequest(string Code);
