using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using OtpNet;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Upkilo.Infrastructure.Services;

public class TwoFactorService : ITwoFactorService
{
    private readonly AppDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TwoFactorService> _logger;

    // TOTP verification allows ±1 time step (30 seconds) for clock drift
    private const int VerificationWindow = 1;

    public TwoFactorService(AppDbContext context, ISmsService smsService, IEmailService emailService, IServiceProvider serviceProvider, ILogger<TwoFactorService> logger)
    {
        _context = context;
        _smsService = smsService;
        _emailService = emailService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TwoFactorSetupResult> SetupTotpAsync(Guid userId)
    {
        // Generate a random 20-byte secret
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        // Trim padding for better app compatibility
        var secret = Base32Encoding.ToString(secretBytes).TrimEnd('=');

        // Store in database (not enabled yet)
        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa == null)
        {
            twoFa = new User2FA { Id = Guid.NewGuid(), UserId = userId };
            _context.Set<User2FA>().Add(twoFa);
        }
        twoFa.TotpSecret = secret;
        twoFa.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Get user email for QR code label
        var user = await _context.Users.FindAsync(userId);
        var label = user?.Email ?? userId.ToString();
        var issuer = "Upkilo";

        // Generate QR code URI (otpauth format) - Highly compatible format
        var qrUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

        return new TwoFactorSetupResult
        {
            Secret = secret,
            QrCodeUri = qrUri,
            ManualEntryKey = secret
        };
    }

    public async Task<bool> VerifyTotpAsync(Guid userId, string code)
    {
        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa?.TotpSecret == null) return false;

        try
        {
            var secretBytes = Base32Encoding.ToBytes(twoFa.TotpSecret);
            var totp = new Totp(secretBytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);

            // Verify with time window tolerance (±1 step = ±30 seconds)
            var isValid = totp.VerifyTotp(code, out _, new VerificationWindow(VerificationWindow, VerificationWindow));

            _logger.LogInformation("TOTP verification for user {UserId}: {Result}", userId, isValid);
            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying TOTP for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> EnableTwoFactorAsync(Guid userId, string verificationCode)
    {
        if (!await VerifyTotpAsync(userId, verificationCode))
            return false;

        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa == null) return false;

        twoFa.IsEnabled = true;
        twoFa.EnabledAt = DateTime.UtcNow;
        twoFa.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        using var scope = _serviceProvider.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        await authService.ProcessTwoFactorStateChangeAsync(userId, true);

        _logger.LogInformation("2FA enabled for user {UserId}", userId);
        return true;
    }

    public async Task DisableTwoFactorAsync(Guid userId)
    {
        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa != null)
        {
            twoFa.IsEnabled = false;
            twoFa.TotpSecret = null;
            twoFa.BackupCodes = null;
            twoFa.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            await authService.ProcessTwoFactorStateChangeAsync(userId, false);
        }
        _logger.LogInformation("2FA disabled for user {UserId}", userId);
    }

    public async Task<string[]> GenerateBackupCodesAsync(Guid userId)
    {
        var codes = new string[10];
        using (var rng = RandomNumberGenerator.Create())
        {
            for (int i = 0; i < 10; i++)
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                codes[i] = $"{BitConverter.ToUInt32(bytes, 0) % 100000000:D8}";
            }
        }

        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa != null)
        {
            // Store hashed codes
            twoFa.BackupCodes = System.Text.Json.JsonSerializer.Serialize(codes.Select(c => HashCode(c)));
            twoFa.BackupCodesRemaining = 10;
            twoFa.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return codes;
    }

    public async Task<bool> VerifyBackupCodeAsync(Guid userId, string code)
    {
        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa?.BackupCodes == null) return false;

        var hashedCodes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(twoFa.BackupCodes);
        var hashedInput = HashCode(code);

        if (hashedCodes?.Contains(hashedInput) == true)
        {
            hashedCodes.Remove(hashedInput);
            twoFa.BackupCodes = System.Text.Json.JsonSerializer.Serialize(hashedCodes);
            twoFa.BackupCodesRemaining = hashedCodes.Count;
            twoFa.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> IsTwoFactorEnabledAsync(Guid userId)
    {
        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        return await Task.FromResult(twoFa?.IsEnabled ?? false);
    }

    public async Task ResetTwoFactorAsync(Guid userId)
    {
        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa != null)
        {
            _context.Set<User2FA>().Remove(twoFa);
            await _context.SaveChangesAsync();
            _logger.LogWarning("2FA has been RESET (hard delete) for user {UserId} by an administrator", userId);
        }
    }

    private static string HashCode(string code)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(hash);
    }

    public async Task<bool> InitiateSmsCodeAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa == null || string.IsNullOrEmpty(twoFa.PhoneNumber))
        {
            _logger.LogWarning("Cannot initiate SMS 2FA for user {UserId}: No phone number configured.", userId);
            return false;
        }

        // Generate a 6-digit random code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        // Store hashed code
        twoFa.SmsCode = HashCode(code);
        twoFa.SmsCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);
        twoFa.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Send via SMS
        var result = await _smsService.SendVerificationCodeAsync(user.TenantId, twoFa.PhoneNumber, code);

        _logger.LogInformation("SMS 2FA code sent to user {UserId}. Success: {Result}", userId, result.Success);
        return result.Success;
    }

    public async Task<bool> VerifySmsCodeAsync(Guid userId, string code)
    {
        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa == null || string.IsNullOrEmpty(twoFa.SmsCode) || !twoFa.SmsCodeExpiresAt.HasValue)
        {
            return false;
        }

        if (twoFa.SmsCodeExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("SMS 2FA code expired for user {UserId}", userId);
            return false;
        }

        var hashedInput = HashCode(code);
        if (twoFa.SmsCode == hashedInput)
        {
            // Clear code after successful verification
            twoFa.SmsCode = null;
            twoFa.SmsCodeExpiresAt = null;
            twoFa.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> InitiateEmailCodeAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            _logger.LogWarning("Cannot initiate email 2FA for user {UserId}: User or email not found.", userId);
            return false;
        }

        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa == null)
        {
            twoFa = new User2FA { Id = Guid.NewGuid(), UserId = userId };
            _context.Set<User2FA>().Add(twoFa);
        }

        // Generate a 6-digit random code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        // Store hashed code
        twoFa.EmailCode = HashCode(code);
        twoFa.EmailCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);
        twoFa.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Send via Email
        try
        {
            await _emailService.SendTwoFactorCodeAsync(user.Email, code);
            _logger.LogInformation("Email 2FA code sent to user {UserId}.", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA email to {Email}", user.Email);
            return false;
        }
    }

    public async Task<bool> VerifyEmailCodeAsync(Guid userId, string code)
    {
        var twoFa = _context.Set<User2FA>().FirstOrDefault(t => t.UserId == userId);
        if (twoFa == null || string.IsNullOrEmpty(twoFa.EmailCode) || !twoFa.EmailCodeExpiresAt.HasValue)
        {
            return false;
        }

        if (twoFa.EmailCodeExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Email 2FA code expired for user {UserId}", userId);
            return false;
        }

        var hashedInput = HashCode(code);
        if (twoFa.EmailCode == hashedInput)
        {
            // Clear code after successful verification
            twoFa.EmailCode = null;
            twoFa.EmailCodeExpiresAt = null;
            twoFa.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new System.Text.StringBuilder((data.Length + 4) / 5 * 8);
        int buffer = 0, bitsLeft = 0;
        foreach (byte b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                result.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }
        if (bitsLeft > 0)
            result.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);
        return result.ToString();
    }

    // ---- Task 18: Remember Device (30-day Trusted Device) ----

    public async Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceToken)
    {
        if (string.IsNullOrEmpty(deviceToken)) return false;

        var config = await _context.Set<TwoFactorConfig>()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (config?.TrustedDevices == null) return false;

        try
        {
            var devices = System.Text.Json.JsonSerializer.Deserialize<List<TrustedDeviceEntry>>(config.TrustedDevices);
            if (devices == null) return false;

            var hashedToken = HashCode(deviceToken);
            var match = devices.FirstOrDefault(d => d.TokenHash == hashedToken);

            if (match == null) return false;

            // Check expiry (30 days)
            if (match.ExpiresAt < DateTime.UtcNow)
            {
                // Remove expired device entry
                devices.Remove(match);
                config.TrustedDevices = System.Text.Json.JsonSerializer.Serialize(devices);
                await _context.SaveChangesAsync();
                return false;
            }

            _logger.LogInformation("Trusted device recognized for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking trusted device for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string> TrustDeviceAsync(Guid userId, string userAgent)
    {
        // Generate a secure random device token
        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var deviceToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        // Get or create TwoFactorConfig
        var config = await _context.Set<TwoFactorConfig>()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (config == null)
        {
            config = new TwoFactorConfig
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TrustedDevices = "[]"
            };
            _context.Set<TwoFactorConfig>().Add(config);
        }

        // Parse existing devices or initialize empty list
        var devices = new List<TrustedDeviceEntry>();
        if (!string.IsNullOrEmpty(config.TrustedDevices))
        {
            try
            {
                devices = System.Text.Json.JsonSerializer.Deserialize<List<TrustedDeviceEntry>>(config.TrustedDevices) ?? new();
            }
            catch { devices = new(); }
        }

        // Remove expired entries (cleanup)
        devices.RemoveAll(d => d.ExpiresAt < DateTime.UtcNow);

        // Cap trusted devices at 10 per user
        if (devices.Count >= 10)
        {
            devices = devices.OrderByDescending(d => d.ExpiresAt).Take(9).ToList();
        }

        // Add new trusted device
        devices.Add(new TrustedDeviceEntry
        {
            TokenHash = HashCode(deviceToken),
            UserAgent = userAgent,
            TrustedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        config.TrustedDevices = System.Text.Json.JsonSerializer.Serialize(devices);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Device trusted for user {UserId} for 30 days", userId);
        return deviceToken;
    }

    // ---- Task 19: 2FA Enforcement per Role/Tenant ----

    public async Task<bool> IsTwoFactorEnforcedAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        // Check 1: TwoFactorConfig.EnforcedByRole flag
        var config = await _context.Set<TwoFactorConfig>()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (config?.EnforcedByRole == true) return true;

        // Check 2: Tenant-level enforcement
        var tenant = await _context.Tenants.FindAsync(user.TenantId);
        if (tenant != null)
        {
            if (tenant.EnforceTwoFactor) return true;

            if (tenant.Settings != null && tenant.Settings.TryGetValue("Enforce2FA", out var enforce2fa))
            {
                if (enforce2fa is bool b && b) return true;
                if (enforce2fa is System.Text.Json.JsonElement je && je.GetBoolean()) return true;
            }

            // Role-based enforcement: e.g., "Enforce2FA_Admin" = true
            var roleKey = $"Enforce2FA_{user.Role}";
            if (tenant.Settings.TryGetValue(roleKey, out var enforceRole))
            {
                if (enforceRole is bool rb && rb) return true;
                if (enforceRole is System.Text.Json.JsonElement rje && rje.GetBoolean()) return true;
            }
        }

        return false;
    }

    // ---- Internal DTOs ----

    private class TrustedDeviceEntry
    {
        public string TokenHash { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public DateTime TrustedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
