using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Upkilo.Core.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Http;


namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IConfiguration _configuration;
    private readonly ISecretProvider _secretProvider;
    private readonly SiemLoggingService _siemLoggingService;
    private readonly ILogger<AuthService> _logger;
    private readonly IBusinessMetrics _metrics;
    private readonly IDbConnectionSelector _connectionSelector;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Configuration
    private const int PasswordResetTokenExpiryMinutes = 15;
    private const int EmailVerificationTokenExpiryHours = 48;
    private const int MaxFailedLoginAttempts = 5;
    private const int LockoutDurationMinutes = 15;
    private const int PasswordHistoryCount = 5;
    private const int MinPasswordLength = 8;

    private readonly IDistributedCache _cache;

    // Configuration
    private const string SessionCachePrefix = "session:";
    private readonly IValidator<RegisterRequest> _registerValidator;

    public AuthService(
        AppDbContext context,
        IEmailService emailService,
        ITwoFactorService twoFactorService,
        IConfiguration configuration,
        ISecretProvider secretProvider,
        ISubscriptionService subscriptionService,
        SiemLoggingService siemLoggingService,
        IDistributedCache cache,
        IBusinessMetrics metrics,
        ILogger<AuthService> logger,
        IValidator<RegisterRequest> registerValidator,
        IDbConnectionSelector connectionSelector,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _emailService = emailService;
        _twoFactorService = twoFactorService;
        _configuration = configuration;
        _secretProvider = secretProvider;
        _subscriptionService = subscriptionService;
        _siemLoggingService = siemLoggingService;
        _cache = cache;
        _metrics = metrics;
        _logger = logger;
        _registerValidator = registerValidator;
        _connectionSelector = connectionSelector;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> InitiatePasswordResetAsync(string email)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        if (user == null)
        {
            // Return true to prevent email enumeration
            _logger.LogInformation("Password reset requested for non-existent email: {Email}", email);
            return true;
        }

        // Invalidate existing tokens
        var existingTokens = await _context.PasswordResetTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync();
        foreach (var t in existingTokens)
            t.UsedAt = DateTime.UtcNow;

        // Generate new token
        var token = GenerateSecureToken();
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetTokenExpiryMinutes)
        };
        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        // Send email
        var appUrl = _configuration["APP_URL"] ?? "https://app.upkilo.com";
        var resetUrl = $"{appUrl.TrimEnd('/')}/reset-password?token={token}";
        await _emailService.SendSystemEmailAsync(
            user.Email,
            "Reset Your Password - Upkilo",
            $@"<h2>Password Reset Request</h2>
               <p>Hi {user.FirstName},</p>
               <p>We received a request to reset your password. Click the link below to create a new password:</p>
               <p><a href=""{resetUrl}"" style=""background-color:#4F46E5;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;"">Reset Password</a></p>
               <p>This link expires in {PasswordResetTokenExpiryMinutes} minutes.</p>
               <p>If you didn't request this, please ignore this email.</p>");

        _logger.LogInformation("Password reset email sent to: {Email}", email);
        return true;
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword, Guid? tenantId = null)
    {
        var (isValid, errors) = ValidatePasswordStrength(newPassword);
        if (!isValid)
            return (false, string.Join(", ", errors));

        // Set tenant context for connection selector if provided
        if (tenantId.HasValue && tenantId.Value != Guid.Empty && _httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items["TenantId"] = tenantId.ToString();
        }

        var hashedToken = HashToken(token);
        var resetToken = await _context.PasswordResetTokens
            .IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == hashedToken && t.UsedAt == null);

        if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
            return (false, "Invalid or expired reset token");

        var user = resetToken.User;
        if (user == null)
            return (false, "User not found");

        // Check password history
        if (await IsPasswordPreviouslyUsedAsync(user.Id, newPassword))
            return (false, "Cannot reuse a recent password");

        // Save old password to history
        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash
        });

        // Update password
        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        resetToken.UsedAt = DateTime.UtcNow;

        // Clean up old password history
        var oldHistories = await _context.PasswordHistories
            .Where(h => h.UserId == user.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Skip(PasswordHistoryCount)
            .ToListAsync();
        _context.PasswordHistories.RemoveRange(oldHistories);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Password reset completed for user: {UserId}", user.Id);

        return (true, "Password has been reset successfully");
    }

    public async Task<bool> SendEmailVerificationAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        if (user.EmailVerified)
            return true;

        // Invalidate existing tokens
        var existingTokens = await _context.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync();
        foreach (var t in existingTokens)
            t.UsedAt = DateTime.UtcNow;

        // Generate new token
        var token = GenerateSecureToken();
        var verificationToken = new EmailVerificationToken
        {
            UserId = userId,
            Token = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddHours(EmailVerificationTokenExpiryHours)
        };
        _context.EmailVerificationTokens.Add(verificationToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email verification token created for User: {UserId}", userId);

        // Send email
        var appUrl = _configuration["APP_URL"] ?? _configuration["App:FrontendUrl"] ?? "https://app.upkilo.com";
        // Include tid for robust multi-db lookup
        var verifyUrl = $"{appUrl.TrimEnd('/')}/verify-email?token={token}&tid={user.TenantId}";

        await _emailService.SendSystemEmailAsync(
            user.Email,
            "Verify Your Email - Upkilo",
            $@"<h2>Welcome to Upkilo!</h2>
               <p>Hi {user.FirstName},</p>
               <p>Please verify your email address by clicking the button below:</p>
               <p><a href=""{verifyUrl}"" style=""background-color:#4F46E5;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;"">Verify Email</a></p>
               <p>This link expires in {EmailVerificationTokenExpiryHours} hours.</p>");

        _logger.LogInformation("Email verification sent to user: {UserId} for Tenant: {TenantId}", userId, user.TenantId);
        return true;
    }

    public async Task<(bool Success, string Message)> VerifyEmailAsync(string token, Guid? tenantId = null)
    {
        if (string.IsNullOrEmpty(token))
            return (false, "Token is required");

        token = token.Trim();

        // If tenantId is provided, attempt to set context to resolve the correct database
        if (tenantId.HasValue && tenantId.Value != Guid.Empty && _httpContextAccessor.HttpContext != null)
        {
            _logger.LogInformation("Setting tenant context for verification: {TenantId}", tenantId);
            _httpContextAccessor.HttpContext.Items["TenantId"] = tenantId.ToString();
        }

        var hashedToken = HashToken(token);

        // Cross-tenant lookup for unauthenticated email verification
        var verificationToken = await _context.EmailVerificationTokens
            .IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == hashedToken && t.UsedAt == null);

        if (verificationToken == null)
        {
            _logger.LogWarning("Verification token not found in database or already used for hashed prefix: {HashPrefix}",
                hashedToken.Substring(0, Math.Min(10, hashedToken.Length)));
            return (false, "Verification token not found or already used.");
        }

        var now = DateTime.UtcNow;
        if (verificationToken.ExpiresAt < now)
        {
            _logger.LogWarning("Verification token expired. ExpiresAt (UTC): {ExpiresAt}, Now (UTC): {Now}",
                verificationToken.ExpiresAt, now);
            return (false, "Verification link has expired.");
        }

        var user = verificationToken.User;
        if (user == null)
        {
            _logger.LogWarning("User not found for verification token ID: {TokenId}", verificationToken.Id);
            return (false, "User not found");
        }

        user.EmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        verificationToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Email verified for user: {UserId}", user.Id);

        return (true, "Email has been verified successfully");
    }

    public (bool IsValid, string[] Errors) ValidatePasswordStrength(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
            errors.Add("Password is required");
        else
        {
            if (password.Length < MinPasswordLength)
                errors.Add($"Password must be at least {MinPasswordLength} characters");
            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("Password must contain at least one uppercase letter");
            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("Password must contain at least one lowercase letter");
            if (!Regex.IsMatch(password, @"[0-9]"))
                errors.Add("Password must contain at least one number");
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
                errors.Add("Password must contain at least one special character");
        }

        return (errors.Count == 0, errors.ToArray());
    }

    public async Task<bool> IsPasswordPreviouslyUsedAsync(Guid userId, string password)
    {
        var histories = await _context.PasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(PasswordHistoryCount)
            .ToListAsync();

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
            histories.Insert(0, new PasswordHistory { PasswordHash = user.PasswordHash });

        return histories.Any(h => VerifyPassword(password, h.PasswordHash));
    }

    public async Task RecordFailedLoginAsync(string email, string ipAddress)
    {
        _context.LoginAttempts.Add(new LoginAttempt
        {
            Email = email.ToLower(),
            IpAddress = ipAddress,
            Succeeded = false
        });
        await _context.SaveChangesAsync();
        _logger.LogWarning("Failed login attempt for: {Email} from IP: {IpAddress}", email, ipAddress);

        await _siemLoggingService.ForwardEventAsync("LoginFailure", new { Email = email, IP = ipAddress });
    }

    public async Task<(bool IsLocked, DateTime? UnlockTime)> IsAccountLockedAsync(string email)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-LockoutDurationMinutes);
        // Single query: fetch timestamps of recent failures — avoids a second round-trip for
        // the last-attempt time when the threshold is hit.
        var recentFailTimes = await _context.LoginAttempts
            .IgnoreQueryFilters()
            .Where(a => a.Email == email.ToLowerInvariant() && a.AttemptedAt > cutoff && !a.Succeeded)
            .OrderByDescending(a => a.AttemptedAt)
            .Select(a => a.AttemptedAt)
            .ToListAsync();

        if (recentFailTimes.Count >= MaxFailedLoginAttempts)
        {
            var unlockTime = recentFailTimes[0].AddMinutes(LockoutDurationMinutes);
            return (true, unlockTime);
        }

        return (false, null);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, string ipAddress, string userAgent, string? deviceToken = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new AuthResult { Success = false, Message = "Email is required" };

        email = email.Trim().ToLower();
        var (isLocked, _) = await IsAccountLockedAsync(email);
        if (isLocked)
        {
            _logger.LogWarning("Login blocked: Account locked for {Email}", email);
            // Generic message — do NOT reveal that the account is locked or when it unlocks
            return new AuthResult { Success = false, Message = "Incorrect email or password" };
        }

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for {Email}", email);
            return await RecordAndReturnFailureAsync(email, ipAddress);
        }

        // Upgrade-on-login: if stored hash is not BCrypt (legacy MD5/SHA1/SHA256),
        // verify with the old algorithm and silently rehash to BCrypt/12 on success.
        if (IsLegacyHash(user.PasswordHash))
        {
            if (!VerifyLegacyHash(password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Legacy hash mismatch for {Email}", email);
                return await RecordAndReturnFailureAsync(email, ipAddress);
            }
            user.PasswordHash = HashPassword(password);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Upgraded legacy password hash to BCrypt/12 for user {UserId}", user.Id);
        }
        else if (!await Task.Run(() => VerifyPassword(password, user.PasswordHash)))
        {
            // BCrypt.Verify is ~100-300ms of CPU at cost 12. Task.Run offloads it from the
            // ASP.NET thread pool so the thread is not blocked during the hash computation.
            _logger.LogWarning("Login failed: Password mismatch for {Email}", email);
            return await RecordAndReturnFailureAsync(email, ipAddress);
        }
        else if (NeedsBCryptUpgrade(user.PasswordHash))
        {
            // BCrypt cost factor below 12 — rehash transparently on successful login
            user.PasswordHash = HashPassword(password);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Upgraded BCrypt cost factor to 12 for user {UserId}", user.Id);
        }

        if (user.Status != UserStatus.Active)
        {
            // Generic message — do NOT reveal that the account exists but is suspended/banned
            _logger.LogWarning("Login failed: Account status is {Status} for {Email}", user.Status, email);
            return new AuthResult { Success = false, Message = "Incorrect email or password" };
        }

        _logger.LogInformation("Login successful for {Email}", email);

        // Check 2FA: user-enabled OR tenant/role-enforced
        var is2faEnabled = await _twoFactorService.IsTwoFactorEnabledAsync(user.Id);
        var is2faEnforced = await _twoFactorService.IsTwoFactorEnforcedAsync(user.Id);

        if (is2faEnabled || is2faEnforced)
        {
            // Check if this is a trusted device (skip 2FA)
            if (!string.IsNullOrEmpty(deviceToken) &&
                await _twoFactorService.IsDeviceTrustedAsync(user.Id, deviceToken))
            {
                _logger.LogInformation("2FA skipped for user {Email} — trusted device", email);
                // Fall through to token generation
            }
            else
            {
                return new AuthResult
                {
                    Success = true,
                    TwoFactorRequired = true,
                    TwoFactorEnforced = is2faEnforced && !is2faEnabled,
                    Message = is2faEnforced && !is2faEnabled
                        ? "2FA is required by your organization. Please set up 2FA."
                        : "2FA Required"
                };
            }
        }

        // --- NEW DEVICE / UNKNOWN IP NOTIFICATION LOGIC ---
        var knownIps = await _context.UserSessions
            .Where(s => s.UserId == user.Id)
            .Select(s => s.IpAddress)
            .Distinct()
            .ToListAsync();

        if (!knownIps.Contains(ipAddress) && knownIps.Any())
        {
            _logger.LogInformation("New IP {IpAddress} detected for user {Email}", ipAddress, email);
            _ = _emailService.SendSystemEmailAsync(
                user.Email,
                "New Login Alert - Upkilo",
                $@"<h2>New Login Detected</h2>
                   <p>Hi {user.FirstName},</p>
                   <p>We noticed a new login to your Upkilo account from a new IP Address or Device.</p>
                   <ul>
                     <li><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                     <li><strong>IP Address:</strong> {ipAddress}</li>
                     <li><strong>Device/Browser:</strong> {userAgent}</li>
                   </ul>
                   <p>If this was you, you can safely ignore this email.</p>
                   <p>If you don't recognize this activity, please change your password immediately and review your active sessions.</p>"
            );
        }

        // Generate Token Pair
        var sessionId = Guid.NewGuid();
        var accessToken = GenerateJwtToken(user, sessionId);
        var refreshToken = GenerateSecureToken();

        // Invalidate older identical sessions to perform proper regeneration
        var oldSessions = await _context.UserSessions
            .Where(s => s.UserId == user.Id &&
                        s.IpAddress == ipAddress &&
                        s.Browser == userAgent &&
                        !s.IsRevoked)
            .ToListAsync();

        foreach (var oldSession in oldSessions)
        {
            oldSession.IsRevoked = true;
            oldSession.ExpiresAt = DateTime.UtcNow;
        }

        // Record session
        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            TenantId = user.TenantId,
            RefreshToken = HashToken(refreshToken),
            IpAddress = ipAddress,
            Browser = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // Refresh token expiry
            LastActiveAt = DateTime.UtcNow
        };
        _context.UserSessions.Add(session);

        // Record success
        _context.LoginAttempts.Add(new LoginAttempt { Email = email.ToLower(), IpAddress = ipAddress, Succeeded = true });
        await _context.SaveChangesAsync();

        await _siemLoggingService.ForwardEventAsync("LoginSuccess", new { IP = ipAddress, UserAgent = userAgent }, user.Id, user.TenantId);

        return new AuthResult
        {
            Success = true,
            Token = accessToken,
            RefreshToken = refreshToken,
            User = new { user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.TenantId }
        };
    }
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string ipAddress, string userAgent)
    {
        // 1. IP-based Rate Limiting (Prevent bot floods)
        var rateLimitKey = $"reg_limit:{ipAddress}";
        var regCountStr = await _cache.GetStringAsync(rateLimitKey);
        // Read from config so the limit can differ per environment without code changes.
        // Production default: 5 attempts/hour. Development default: 50.
        var maxAttempts = _configuration.GetValue<int>("Auth:RegistrationMaxAttemptsPerHour", 10);
        if (!string.IsNullOrEmpty(regCountStr) && int.TryParse(regCountStr, out var count) && count >= maxAttempts)
        {
            _logger.LogWarning("Registration rate limit exceeded for IP: {Ip}", ipAddress);
            _siemLoggingService.LogSecurityEvent("RegistrationRateLimitExceeded", $"IP: {ipAddress}", SecurityEventSeverity.Medium);
            _metrics.RecordRegistrationAttempt("rate_limited");
            return new AuthResult { Success = false, Message = "Too many registration attempts. Please try again later." };
        }

        // 2. FluentValidation
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return new AuthResult
            {
                Success = false,
                Message = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage))
            };
        }

        var email = request.Email;
        var password = request.Password;
        var firstName = request.FirstName;
        var lastName = request.LastName;
        var companyName = request.CompanyName;
        var planId = request.PlanId;

        // 3. Domain Blacklist Check
        var domain = email.Split('@').Last().ToLower();
        var isBlacklisted = await _cache.GetAsync($"blacklist:{domain}") != null;
        if (isBlacklisted)
        {
            _logger.LogWarning("Registration attempted from blacklisted domain: {Domain}", domain);
            _siemLoggingService.LogSecurityEvent("BlacklistedDomainRegistrationAttempt", $"Email: {email}", SecurityEventSeverity.Low);
            return new AuthResult { Success = false, Message = "This email domain is not permitted." };
        }

        // 4. Duplicate Check — indistinguishable from successful registration path
        if (await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant()))
        {
            // Jitter prevents distinguishing this fast-path from the real registration path,
            // which takes 150-300 ms for BCrypt/12.  Use a random delay in the same range.
            await Task.Delay(Random.Shared.Next(150, 350));
            return new AuthResult { Success = true, Message = "Registration successful. A verification email has been sent to your inbox." };
        }

        // Ensure we handle the transaction inside the configured execution strategy (for retries)
        // and force the connection to the Primary database for the write operation.
        _connectionSelector.UseReplica(false);

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 5. Create Tenant & User
                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = companyName ?? $"{firstName}'s Organization",
                    Slug = (companyName ?? firstName).ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString("N")[..4],
                    Status = TenantStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Tenants.Add(tenant);

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Email = email.ToLowerInvariant(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    FirstName = firstName,
                    LastName = lastName,
                    Role = UserRole.Admin,
                    Status = UserStatus.Active,
                    EmailVerified = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);

                // 6. Default Subscription (Trial)
                var pricingPlan = planId.HasValue
                    ? await _context.PricingPlans.FindAsync(planId.Value)
                    : await _context.PricingPlans.FirstOrDefaultAsync(p => p.Name == "Free");

                if (pricingPlan == null)
                {
                    _logger.LogCritical("No Free pricing plan found in PricingPlans table. Run the seeder before registering users.");
                    throw new InvalidOperationException("Platform is not correctly configured: Free plan is missing. Please contact support.");
                }

                {
                    var subscription = new Subscription
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant.Id,
                        PricingPlanId = pricingPlan.Id,
                        Status = SubscriptionStatus.Active,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(14), // Default 14-day trial
                        // R4 fix: set a $5 default AI budget so new tenants aren't silently blocked
                        // from AI on first login. SyncWithStripeAsync will override this with the
                        // plan-derived value once Stripe is connected.
                        AiMonthlyBudget = 5.00m,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Subscriptions.Add(subscription);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("New user registered: {Email} for Tenant: {TenantId}", email, tenant.Id);
                _siemLoggingService.LogSecurityEvent("UserRegistered", $"Email: {email}, Tenant: {tenant.Id}", SecurityEventSeverity.Low);

                // 7. Background: Stripe Customer Creation
                try
                {
                    await _subscriptionService.SyncWithStripeAsync(tenant.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stripe customer creation failed for {Email}", email);
                }



                // 9. Initial Session
                var sessionId = Guid.NewGuid();
                var accessToken = GenerateJwtToken(user, sessionId);
                var refreshToken = GenerateSecureToken();

                var message = "Registration successful. A verification email has been sent to your inbox.";

                // 8. Email Verification - Wrap in try-catch to ensure registration doesn't 
                // fail just because an email couldn't be sent (account is already committed).
                try
                {
                    await SendEmailVerificationAsync(user.Id);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "User {Email} created but verification email failed to send.", email);
                    message = "Registration successful, but we couldn't send the verification email. You can resend it from your user settings.";
                }

                // Record session
                var session = new UserSession
                {
                    Id = sessionId,
                    UserId = user.Id,
                    TenantId = tenant.Id,
                    RefreshToken = HashToken(refreshToken),
                    IpAddress = ipAddress,
                    Browser = userAgent,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    LastActiveAt = DateTime.UtcNow
                };
                _context.UserSessions.Add(session);
                await _context.SaveChangesAsync();

                // Record success metric
                _metrics.RecordRegistrationAttempt("success");

                // Update rate limit count
                var currentCount = regCountStr == null ? 0 : int.Parse(regCountStr);
                await _cache.SetStringAsync(rateLimitKey, (currentCount + 1).ToString(),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

                return new AuthResult
                {
                    Success = true,
                    Message = message,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    User = new { user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.TenantId }
                };
            }
            catch (Exception ex)
            {
                // Serilog + Application Insights captures the full exception (including inner)
                // automatically — no need to write to a local file.
                _logger.LogError(ex, "Registration failed for {Email}", email);
                try { if (transaction != null) await transaction.RollbackAsync(); } catch (Exception rollbackEx) { _logger.LogWarning(rollbackEx, "Rollback failed (transaction may have already completed)"); }
                _metrics.RecordRegistrationAttempt("failed");
                return new AuthResult { Success = false, Message = "An unexpected error occurred during registration." };
            }
        });
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent)
    {
        var hashedToken = HashToken(refreshToken);
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == hashedToken);

        if (session == null)
            return new AuthResult { Success = false, Message = "Invalid refresh token" };

        if (session.IsRevoked)
        {
            // TOKEN REUSE DETECTION - If a revoked token is used, someone might have stolen it.
            // Revoke all other active sessions for this user as a precaution.
            _logger.LogWarning("Revoked refresh token reuse detected for User: {UserId}. Revoking all sessions.", session.UserId);
            var allSessions = await _context.UserSessions.Where(s => s.UserId == session.UserId && !s.IsRevoked).ToListAsync();
            foreach (var s in allSessions) s.IsRevoked = true;
            await _context.SaveChangesAsync();
            return new AuthResult { Success = false, Message = "Security alert: Session compromised" };
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            session.IsRevoked = true;
            await _context.SaveChangesAsync();
            return new AuthResult { Success = false, Message = "Refresh token expired" };
        }

        // Generate new pair (ROTATION)
        var user = session.User;
        if (user == null) return new AuthResult { Success = false, Message = "User not found" };

        var newSessionId = Guid.NewGuid();
        var newAccessToken = GenerateJwtToken(user, newSessionId);
        var newRefreshToken = GenerateSecureToken();

        // Update session with new token and revoke current one
        session.IsRevoked = true;
        _context.UserSessions.Add(new UserSession
        {
            Id = newSessionId,
            UserId = user.Id,
            TenantId = user.TenantId,
            RefreshToken = HashToken(newRefreshToken),
            IpAddress = ipAddress,
            Browser = userAgent, // Simplified parsing or just store raw
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            User = new { user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.TenantId }
        };
    }

    public async Task<AuthResult> VerifyTwoFactorAsync(string email, string code, bool isBackupCode, bool rememberDevice = false, string? ipAddress = null, string? userAgent = null)
    {
        // H-09 FIX: Rate limit 2FA verification to prevent brute-forcing
        var cacheKey = $"2fa_attempts_{email.ToLower()}";
        var attemptsStr = await _cache.GetStringAsync(cacheKey);
        int attempts = string.IsNullOrEmpty(attemptsStr) ? 0 : int.Parse(attemptsStr);

        if (attempts >= 5)
        {
            _logger.LogWarning("2FA verification locked out for {Email} due to rate limiting.", email);
            return new AuthResult { Success = false, Message = "Too many attempts. Please try again after 15 minutes." };
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

        if (user == null)
        {
            // Increment even if user not found to prevent user enumeration
            await _cache.SetStringAsync(cacheKey, (attempts + 1).ToString(), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
            return new AuthResult { Success = false, Message = "Invalid credentials" };
        }

        var twoFa = await _context.Set<User2FA>().FirstOrDefaultAsync(t => t.UserId == user.Id);
        if (twoFa != null && twoFa.LockedUntil.HasValue && twoFa.LockedUntil > DateTime.UtcNow)
        {
            return new AuthResult
            {
                Success = false,
                Message = $"2FA is locked due to too many failed attempts. Try again at {twoFa.LockedUntil.Value:HH:mm} UTC."
            };
        }

        bool isValid;
        if (isBackupCode)
        {
            isValid = await _twoFactorService.VerifyBackupCodeAsync(user.Id, code);
        }
        else
        {
            // Try TOTP first, then SMS, then Email as fallback
            isValid = await _twoFactorService.VerifyTotpAsync(user.Id, code);
            if (!isValid)
            {
                isValid = await _twoFactorService.VerifySmsCodeAsync(user.Id, code);
            }
            if (!isValid)
            {
                isValid = await _twoFactorService.VerifyEmailCodeAsync(user.Id, code);
            }
        }

        if (!isValid)
        {
            // Increment cache rate limit
            await _cache.SetStringAsync(cacheKey, (attempts + 1).ToString(), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });

            if (twoFa != null)
            {
                twoFa.FailedAttempts++;
                if (twoFa.FailedAttempts >= 5)
                {
                    twoFa.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                    _logger.LogWarning("User {Email} 2FA locked out for 15 mins after 5 failures.", email);
                }
                twoFa.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return new AuthResult { Success = false, Message = "Invalid 2FA code" };
        }

        // Clear rate limit on success
        await _cache.RemoveAsync(cacheKey);

        // Reset lockout on success
        if (twoFa != null)
        {
            twoFa.FailedAttempts = 0;
            twoFa.LockedUntil = null;
            twoFa.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Generate tokens
        var sessionId = Guid.NewGuid();
        var accessToken = GenerateJwtToken(user, sessionId);
        var refreshToken = GenerateSecureToken();

        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            TenantId = user.TenantId,
            RefreshToken = HashToken(refreshToken),
            IpAddress = ipAddress,
            Browser = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            LastActiveAt = DateTime.UtcNow
        };
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("2FA verification successful for user: {Email}", email);

        // Remember device if requested
        string? newDeviceToken = null;
        if (rememberDevice && !string.IsNullOrEmpty(userAgent))
        {
            newDeviceToken = await _twoFactorService.TrustDeviceAsync(user.Id, userAgent);
        }

        return new AuthResult
        {
            Success = true,
            Token = accessToken,
            RefreshToken = refreshToken,
            DeviceToken = newDeviceToken,
            User = new { user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.TenantId }
        };
    }

    public async Task ProcessTwoFactorStateChangeAsync(Guid userId, bool enabled)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        var status = enabled ? "enabled" : "disabled";

        // Fire-and-forget email notification
        _ = _emailService.SendSystemEmailAsync(
            user.Email,
            $"Two-Factor Authentication {char.ToUpper(status[0]) + status.Substring(1)}",
            $@"<h2>Security Setting Updated</h2>
               <p>Hi {user.FirstName},</p>
               <p>Two-Factor Authentication (2FA) has been <strong>{status}</strong> on your Upkilo account.</p>
               <p>If you did not make this change, please contact support immediately.</p>"
        );

        return;
    }

    public async Task<AuthResponse> SendTwoFactorSmsAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        // Generic response — do NOT reveal whether the email is registered
        if (user == null) return new AuthResponse(true, "If that email is registered, an SMS code will be sent.");

        var success = await _twoFactorService.InitiateSmsCodeAsync(user.Id);
        return new AuthResponse(success, success ? "SMS code sent" : "If that email is registered, an SMS code will be sent.");
    }

    public async Task<AuthResponse> SendTwoFactorEmailAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        // Generic response — do NOT reveal whether the email is registered
        if (user == null) return new AuthResponse(true, "If that email is registered, an email code will be sent.");

        var success = await _twoFactorService.InitiateEmailCodeAsync(user.Id);
        return new AuthResponse(success, success ? "Email code sent" : "If that email is registered, an email code will be sent.");
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var hashedToken = HashToken(refreshToken);
        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.RefreshToken == hashedToken);
        if (session == null) return false;

        session.IsRevoked = true;
        await _context.SaveChangesAsync();

        // Blacklist in Redis for immediate global revocation
        await _cache.SetStringAsync($"blacklist:sid:{session.Id}", "revoked", new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(7)
        });

        return true;
    }

    private string GenerateSlug(string name)
    {
        string slug = name.ToLower();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim();
        slug += "-" + Guid.NewGuid().ToString().Substring(0, 4);
        return slug;
    }

    public async Task<dynamic?> GetCurrentUserAsync(Guid userId)
    {
        // IgnoreQueryFilters: SuperAdmin has TenantId=Guid.Empty, which the
        // global tenant filter would exclude. This endpoint must work for all roles.
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.TenantId,
            tenantName = user.Tenant?.Name,
            user.EmailVerified,
            user.TwoFactorEnabled,
            user.LastLoginAt
        };
    }

    public async Task<bool> RevokeAllSessionsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync();

        if (!sessions.Any()) return true;

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.ExpiresAt = DateTime.UtcNow;

            // Blacklist in Redis for immediate global revocation
            await _cache.SetStringAsync($"blacklist:sid:{session.Id}", "revoked", new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(7) // Typical max token life
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("All sessions revoked and blacklisted for user: {UserId}", userId);
        return true;
    }

    private string GenerateJwtToken(User user, Guid sessionId, bool rememberMe = false)
    {
        var jwtSecret = _secretProvider.GetSecret("Jwt:Secret");
        if (string.IsNullOrEmpty(jwtSecret)
            || jwtSecret.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase)
            || jwtSecret.StartsWith("dev-only-", StringComparison.OrdinalIgnoreCase)
            || jwtSecret.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || jwtSecret.Length < 32)
        {
            throw new InvalidOperationException("JWT Secret is not configured or is too weak. Set 'Jwt:Secret' in Key Vault or environment (min 32 chars; generate with: openssl rand -hex 32).");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("tenant_id", user.TenantId.ToString() ?? ""),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("sid", sessionId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // CRITICAL-SEC-01: Access Token ALWAYS expires in 15 minutes.
        // 'rememberMe' dictates Refresh Token lifespan, not JWT lifespan.
        var expiry = DateTime.UtcNow.AddMinutes(15);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponse> SubmitTwoFactorRecoveryRequestAsync(string email, string identityData)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        if (user == null)
        {
            return new AuthResponse(true, "Recovery request submitted successfully. Our team will review it.");
        }

        var activeRequest = await _context.TwoFaRecoveryRequests
            .FirstOrDefaultAsync(r => r.UserId == user.Id && r.Status == "Pending");

        if (activeRequest != null)
        {
            return new AuthResponse(false, "A pending recovery request already exists.");
        }

        var request = new TwoFaRecoveryRequest
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IdentityVerificationData = identityData,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.TwoFaRecoveryRequests.Add(request);
        await _context.SaveChangesAsync();

        // Notify admins (optional)
        _logger.LogInformation("New 2FA recovery request submitted by User: {UserId}", user.Id);

        return new AuthResponse(true, "Recovery request submitted successfully. Our team will review it.");
    }

    public async Task<bool> ProcessTwoFactorRecoveryRequestAsync(Guid requestId, Guid adminId, bool approve, string notes)
    {
        var request = await _context.TwoFaRecoveryRequests.FindAsync(requestId);
        if (request == null || request.Status != "Pending") return false;

        request.Status = approve ? "Approved" : "Rejected";
        request.ResolvedByAdminId = adminId;
        request.ResolvedAt = DateTime.UtcNow;
        request.AdminNotes = notes;
        request.UpdatedAt = DateTime.UtcNow;

        if (approve)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user != null)
            {
                var twoFa = await _context.Set<User2FA>().FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (twoFa != null)
                {
                    _context.Set<User2FA>().Remove(twoFa);
                }

                await _siemLoggingService.ForwardEventAsync("TwoFaDisabledByAdmin", new { RequestId = requestId, AdminId = adminId }, user.Id, user.TenantId);

                _ = _emailService.SendSystemEmailAsync(
                    user.Email,
                    "2FA Recovery Request Approved",
                    $@"<h2>2FA Recovery Approved</h2>
                       <p>Hi {user.FirstName},</p>
                       <p>Your 2FA recovery request has been approved and 2FA has been disabled on your account.</p>
                       <p>Please log in and set up 2FA again immediately.</p>"
                );
            }
        }
        else
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user != null)
            {
                _ = _emailService.SendSystemEmailAsync(
                    user.Email,
                    "2FA Recovery Request Rejected",
                    $@"<h2>2FA Recovery Rejected</h2>
                       <p>Hi {user.FirstName},</p>
                       <p>We could not verify your identity for the 2FA recovery request. Notes: {notes}</p>
                       <p>Please submit a new request with valid identity verification data.</p>"
                );
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("2FA Recovery Request {RequestId} was {Status} by Admin: {AdminId}", requestId, request.Status, adminId);
        return true;
    }


    // Helper methods
    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        // Use Hex encoding instead of Base64 to avoid confusing characters (0/O, 1/l/I, 7/Z, +/-/_)
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    // ── Legacy hash detection & migration helpers ─────────────────────────────

    // BCrypt hashes always start with $2 ($2b$, $2a$, $2y$)
    private static bool IsLegacyHash(string hash)
        => string.IsNullOrEmpty(hash) || !hash.StartsWith("$2", StringComparison.Ordinal);

    // Verify against old weak algorithms (SHA-256, SHA-1, MD5) for migration path only.
    // These are intentionally the *reading* side of migration — no new hashes are ever
    // created with these algorithms.
    private static bool VerifyLegacyHash(string password, string storedHash)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var stored = storedHash.ToLowerInvariant();

        var sha256Hex = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (sha256Hex == stored) return true;

        var sha1Hex = Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant();
        if (sha1Hex == stored) return true;

        var md5Hex = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();
        return md5Hex == stored;
    }

    // Returns true if the stored BCrypt hash was created with a cost factor below 12.
    private static bool NeedsBCryptUpgrade(string hash)
    {
        if (!hash.StartsWith("$2")) return false;
        var parts = hash.Split('$');
        return parts.Length >= 3 && int.TryParse(parts[2], out var cost) && cost < 12;
    }

    // ── Failed-attempt handling with progressive delay ────────────────────────

    /// <summary>
    /// Records a failed login attempt, applies progressive delay (to slow brute-force),
    /// sends a lockout email when the threshold is first hit, then returns a generic error.
    /// </summary>
    private async Task<AuthResult> RecordAndReturnFailureAsync(string email, string ipAddress)
    {
        await RecordFailedLoginAsync(email, ipAddress);

        // Count failures in the lockout window (same window used by IsAccountLockedAsync)
        var cutoff = DateTime.UtcNow.AddMinutes(-LockoutDurationMinutes);
        var failCount = await _context.LoginAttempts
            .IgnoreQueryFilters()
            .CountAsync(a => a.Email == email && !a.Succeeded && a.AttemptedAt > cutoff);

        // Progressive delay: 0s → 1s → 2s → 4s → 8s (capped)
        // failCount=1: no delay; 2: 1s; 3: 2s; 4: 4s; ≥5: 8s
        if (failCount >= 2)
        {
            var delayMs = Math.Min((int)Math.Pow(2, failCount - 2) * 1000, 8000);
            await Task.Delay(delayMs);
        }

        // Send lockout notification exactly when the threshold is first hit
        if (failCount == MaxFailedLoginAttempts)
            _ = SendLockoutNotificationAsync(email);

        return new AuthResult { Success = false, Message = "Incorrect email or password" };
    }

    private async Task SendLockoutNotificationAsync(string email)
    {
        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        if (user == null) return;

        var appUrl = _configuration["APP_URL"] ?? "https://app.upkilo.com";
        var resetUrl = $"{appUrl.TrimEnd('/')}/forgot-password";

        await _emailService.SendSystemEmailAsync(
            user.Email,
            "Suspicious Login Activity – Upkilo",
            $@"<h2>Suspicious Login Activity Detected</h2>
               <p>Hi {user.FirstName},</p>
               <p>We detected multiple failed login attempts on your Upkilo account. For your protection, access to your account has been temporarily suspended for {LockoutDurationMinutes} minutes.</p>
               <p>If <strong>you</strong> made these attempts, please wait {LockoutDurationMinutes} minutes and try again.</p>
               <p>If <strong>you did not</strong> make these attempts, reset your password immediately:</p>
               <p><a href=""{resetUrl}"" style=""background-color:#DC2626;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;"">Reset Password Now</a></p>
               <p>For further assistance contact our support team.</p>");
    }

    public async Task<AuthResult> SocialLoginAsync(string email, string firstName, string lastName, string provider, string? avatarUrl, string ipAddress, string userAgent)
    {
        _logger.LogInformation("Social login attempt: {Provider} for {Email}", provider, email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        bool isNewUser = false;

        if (user == null)
        {
            // Create new user + tenant for social signup
            isNewUser = true;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = $"{firstName}'s Organization",
                    Slug = GenerateSlug(firstName),
                    Status = TenantStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Tenants.Add(tenant);

                user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password for social users
                    FirstName = firstName,
                    LastName = lastName,
                    AvatarUrl = avatarUrl,
                    Role = UserRole.Admin,
                    Status = UserStatus.Active,
                    EmailVerified = true, // Social logins have verified emails
                    EmailVerifiedAt = DateTime.UtcNow,
                    SocialProvider = provider,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);

                // Default Free subscription
                var freePricingPlan = await _context.PricingPlans.FirstOrDefaultAsync(p => p.Name == "Free");
                _context.Subscriptions.Add(new Subscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    PricingPlanId = freePricingPlan?.Id,
                    Status = SubscriptionStatus.Active,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(14),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Social signup complete: {Provider} {Email} => Tenant {TenantId}", provider, email, tenant.Id);

                try { await _subscriptionService.SyncWithStripeAsync(tenant.Id); }
                catch (Exception ex) { _logger.LogError(ex, "Stripe sync failed for social signup {Email}", email); }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Social registration failed for {Email}", email);
                return new AuthResult { Success = false, Message = "An error occurred creating your account." };
            }
        }

        // Generate session + tokens
        var sessionId = Guid.NewGuid();
        var accessToken = GenerateJwtToken(user, sessionId);
        var refreshToken = GenerateSecureToken();

        _context.UserSessions.Add(new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            TenantId = user.TenantId,
            RefreshToken = HashToken(refreshToken),
            IpAddress = ipAddress,
            Browser = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            LastActiveAt = DateTime.UtcNow
        });

        _context.LoginAttempts.Add(new LoginAttempt { Email = email.ToLower(), IpAddress = ipAddress, Succeeded = true });
        await _context.SaveChangesAsync();

        await _siemLoggingService.ForwardEventAsync($"SocialLogin:{provider}", new { IP = ipAddress }, user.Id, user.TenantId);

        return new AuthResult
        {
            Success = true,
            Token = accessToken,
            RefreshToken = refreshToken,
            IsNewUser = isNewUser,
            User = new { user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.TenantId }
        };
    }


    /// <summary>
    /// Issues a session for the given user. MUST only be called after
    /// <c>_fido2.MakeAssertionAsync()</c> has returned status="ok" in BiometricAuthController.
    /// The method trusts the caller to have completed cryptographic FIDO2 assertion verification
    /// and MUST NOT be called from any other code path.
    /// </summary>
    public async Task<AuthResult> LoginWithBiometricAsync(Guid userId, string ipAddress, string userAgent)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return new AuthResult { Success = false, Message = "User not found" };

        if (user.Status != UserStatus.Active)
            return new AuthResult { Success = false, Message = "Account is not active" };

        var sessionId = Guid.NewGuid();
        var accessToken = GenerateJwtToken(user, sessionId);
        var refreshToken = GenerateSecureToken();

        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            TenantId = user.TenantId,
            RefreshToken = HashToken(refreshToken),
            IpAddress = ipAddress,
            Browser = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            LastActiveAt = DateTime.UtcNow
        };

        _context.UserSessions.Add(session);
        _context.LoginAttempts.Add(new LoginAttempt { Email = user.Email.ToLower(), IpAddress = ipAddress, Succeeded = true });

        await _context.SaveChangesAsync();
        await _siemLoggingService.ForwardEventAsync("BiometricLoginSuccess", new { IP = ipAddress, UserAgent = userAgent }, user.Id, user.TenantId);

        return new AuthResult
        {
            Success = true,
            Token = accessToken,
            RefreshToken = refreshToken,
            User = new { user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.TenantId }
        };
    }

    public async Task<AuthResult> SsoLoginAsync(string email, string firstName, string lastName, string provider, Guid tenantId, string ipAddress, string userAgent)
    {
        _logger.LogInformation("SSO login attempt: provider={Provider}, tenant={TenantId}, email={Email}", provider, tenantId, email);

        // Query user case-insensitively, ignoring standard tenant-isolation filters to look up across the database explicitly by tenantId.
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email.ToLowerInvariant() && !u.IsDeleted);

        bool isNewUser = false;

        if (user == null)
        {
            // Load the tenant's SAML configuration
            var samlConfig = await _context.SamlConfigurations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);

            if (samlConfig == null || !samlConfig.IsEnabled || !samlConfig.AutoCreateUsers)
            {
                _logger.LogWarning("SSO login failed: User does not exist and auto-provisioning is disabled or SAML is not configured/enabled for tenant {TenantId}.", tenantId);
                return new AuthResult { Success = false, Message = "User account does not exist and auto-provisioning is disabled." };
            }

            // Provision a new user
            isNewUser = true;

            var role = UserRole.Staff;
            Guid? customRoleId = null;

            if (!string.IsNullOrEmpty(samlConfig.DefaultRoleId))
            {
                if (Guid.TryParse(samlConfig.DefaultRoleId, out var customGuid))
                {
                    customRoleId = customGuid;
                }
                else if (Enum.TryParse<UserRole>(samlConfig.DefaultRoleId, true, out var parsedRole))
                {
                    role = parsedRole;
                }
            }

            user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password since login is external
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                CustomRoleId = customRoleId,
                Status = UserStatus.Active,
                IsActive = true,
                EmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                SocialProvider = provider,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Auto-provisioned new user via SSO: {Email} for Tenant {TenantId}", email, tenantId);
        }

        if (user.Status != UserStatus.Active || !user.IsActive)
        {
            _logger.LogWarning("SSO login failed: User account {Email} is inactive for Tenant {TenantId}.", email, tenantId);
            return new AuthResult { Success = false, Message = "User account is inactive." };
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var sessionId = Guid.NewGuid();
        var accessToken = GenerateJwtToken(user, sessionId);
        var refreshToken = GenerateSecureToken();

        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            TenantId = user.TenantId,
            RefreshToken = HashToken(refreshToken),
            IpAddress = ipAddress,
            Browser = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            LastActiveAt = DateTime.UtcNow
        };

        _context.UserSessions.Add(session);
        _context.LoginAttempts.Add(new LoginAttempt { Email = email.ToLower(), IpAddress = ipAddress, Succeeded = true });

        await _context.SaveChangesAsync();
        await _siemLoggingService.ForwardEventAsync($"SsoLogin:{provider}", new { IP = ipAddress }, user.Id, user.TenantId);

        return new AuthResult
        {
            Success = true,
            Token = accessToken,
            RefreshToken = refreshToken,
            IsNewUser = isNewUser,
            User = new { user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.TenantId }
        };
    }
}
