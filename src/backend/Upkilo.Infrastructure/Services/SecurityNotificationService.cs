using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for sending security notifications related to 2FA changes, 
/// session changes, and other security-sensitive actions.
/// </summary>
public class SecurityNotificationService
{
    private readonly IEmailService _emailService;
    private readonly AppDbContext _db;
    private readonly ILogger<SecurityNotificationService> _logger;

    public SecurityNotificationService(
        IEmailService emailService,
        AppDbContext db,
        ILogger<SecurityNotificationService> logger)
    {
        _emailService = emailService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Send notification when 2FA is enabled
    /// </summary>
    public async Task Notify2FAEnabledAsync(Guid userId, string method)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email == null) return;

        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Two-Factor Authentication Enabled — Upkilo",
                BuildSecurityEmail(
                    user.FirstName ?? "User",
                    "Two-Factor Authentication Enabled",
                    $"Two-factor authentication ({method}) has been enabled on your account.",
                    "If you did not make this change, please contact support immediately and change your password."));

            _logger.LogInformation("2FA enabled notification sent to {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA enabled notification to {UserId}", userId);
        }
    }

    /// <summary>
    /// Send notification when 2FA is disabled
    /// </summary>
    public async Task Notify2FADisabledAsync(Guid userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email == null) return;

        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "⚠️ Two-Factor Authentication Disabled — Upkilo",
                BuildSecurityEmail(
                    user.FirstName ?? "User",
                    "Two-Factor Authentication Disabled",
                    "Two-factor authentication has been <strong>disabled</strong> on your account. " +
                    "Your account is now less secure.",
                    "If you did not make this change, your account may be compromised. " +
                    "Please change your password immediately and re-enable 2FA."));

            _logger.LogInformation("2FA disabled notification sent to {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA disabled notification to {UserId}", userId);
        }
    }

    /// <summary>
    /// Send notification when 2FA method is changed
    /// </summary>
    public async Task Notify2FAMethodChangedAsync(Guid userId, string oldMethod, string newMethod)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email == null) return;

        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "2FA Method Changed — Upkilo",
                BuildSecurityEmail(
                    user.FirstName ?? "User",
                    "Two-Factor Authentication Method Changed",
                    $"Your 2FA method has been changed from <strong>{oldMethod}</strong> to <strong>{newMethod}</strong>.",
                    "If you did not make this change, please contact support immediately."));

            _logger.LogInformation("2FA method changed notification sent to {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA method changed notification to {UserId}", userId);
        }
    }

    /// <summary>
    /// Send notification when backup codes are regenerated
    /// </summary>
    public async Task NotifyBackupCodesRegeneratedAsync(Guid userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email == null) return;

        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Backup Codes Regenerated — Upkilo",
                BuildSecurityEmail(
                    user.FirstName ?? "User",
                    "Backup Codes Regenerated",
                    "Your two-factor authentication backup codes have been regenerated. " +
                    "Previous backup codes are no longer valid.",
                    "Make sure to store your new backup codes in a safe place. " +
                    "If you did not make this change, please contact support immediately."));

            _logger.LogInformation("Backup codes regenerated notification sent to {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send backup codes notification to {UserId}", userId);
        }
    }

    /// <summary>
    /// Send notification on password change
    /// </summary>
    public async Task NotifyPasswordChangedAsync(Guid userId, string? ipAddress = null)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email == null) return;

        var ipInfo = !string.IsNullOrEmpty(ipAddress) ? $" from IP address <code>{ipAddress}</code>" : "";

        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Password Changed — Upkilo",
                BuildSecurityEmail(
                    user.FirstName ?? "User",
                    "Password Changed",
                    $"Your password was changed successfully{ipInfo}.",
                    "If you did not change your password, please reset it immediately and enable 2FA."));

            _logger.LogInformation("Password changed notification sent to {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed notification to {UserId}", userId);
        }
    }

    /// <summary>
    /// Send notification on new device login
    /// </summary>
    public async Task NotifyNewDeviceLoginAsync(Guid userId, string? device, string? ipAddress, string? location)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email == null) return;

        var details = new List<string>();
        if (!string.IsNullOrEmpty(device)) details.Add($"<strong>Device:</strong> {device}");
        if (!string.IsNullOrEmpty(ipAddress)) details.Add($"<strong>IP Address:</strong> {ipAddress}");
        if (!string.IsNullOrEmpty(location)) details.Add($"<strong>Location:</strong> {location}");
        details.Add($"<strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "New Device Login — Upkilo",
                BuildSecurityEmail(
                    user.FirstName ?? "User",
                    "New Device Login Detected",
                    "A new login was detected on your account:<br/><br/>" +
                    string.Join("<br/>", details),
                    "If this wasn't you, please change your password immediately and review your active sessions."));

            _logger.LogInformation("New device login notification sent to {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new device login notification to {UserId}", userId);
        }
    }

    private static string BuildSecurityEmail(string firstName, string title, string message, string warning)
    {
        return $@"
<!DOCTYPE html>
<html>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; margin: 0; padding: 0; background: #f4f4f4;'>
  <div style='max-width: 600px; margin: 40px auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.08);'>
    <div style='background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); padding: 32px; text-align: center;'>
      <h1 style='color: #fff; margin: 0; font-size: 24px;'>🔐 {title}</h1>
    </div>
    <div style='padding: 32px;'>
      <p style='color: #333; font-size: 16px; line-height: 1.6;'>Hi {firstName},</p>
      <p style='color: #333; font-size: 16px; line-height: 1.6;'>{message}</p>
      <div style='background: #fff3cd; border-left: 4px solid #ffc107; padding: 16px; margin: 24px 0; border-radius: 4px;'>
        <p style='color: #856404; margin: 0; font-size: 14px;'><strong>⚠️ Security Notice:</strong> {warning}</p>
      </div>
      <hr style='border: none; border-top: 1px solid #eee; margin: 24px 0;' />
      <p style='color: #888; font-size: 12px; line-height: 1.5;'>
        This is an automated security notification from Upkilo. If you have questions, contact support at support@upkilo.com.
      </p>
    </div>
  </div>
</body>
</html>";
    }
}
