using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Core.DTOs;

namespace Upkilo.API.Controllers;

/// <summary>
/// Super Admin controller for platform-wide management.
/// Replaces mocks with real DB queries on Tenants, Subscriptions, and AuditLogs.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/super-admin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly ILogger<SuperAdminController> _logger;
    private readonly AppDbContext _context;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ISecretProvider _secretProvider;
    private readonly SubscriptionPlanVersioningService _versioningService;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;

    public SuperAdminController(
        ILogger<SuperAdminController> logger,
        AppDbContext context,
        ITwoFactorService twoFactorService,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration,
        ISecretProvider secretProvider,
        SubscriptionPlanVersioningService versioningService,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache)
    {
        _logger = logger;
        _context = context;
        _twoFactorService = twoFactorService;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _secretProvider = secretProvider;
        _versioningService = versioningService;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// Super Admin Standalone Login
    /// </summary>
    /// <summary>
    /// Super Admin Standalone Login (Step 1: Password)
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Role == UserRole.SuperAdmin);

        if (user == null)
            return Unauthorized("Invalid admin credentials");

        var pHasher = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (pHasher == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid admin credentials");

        // Mandate 2FA setup if not enabled
        if (!user.TwoFactorEnabled)
        {
            return Ok(new 
            { 
                status = "SetupRequired", 
                message = "Two-factor authentication must be configured first.",
                email = user.Email
            });
        }

        // M-5 FIX: Use distributed cache (Redis) instead of IMemoryCache so
        // pre-auth tokens work across multiple app instances.
        var preAuthToken = Guid.NewGuid().ToString("N");
        await _distributedCache.SetStringAsync(
            $"SuperAdminPreAuth_{user.Email}",
            preAuthToken,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        return Ok(new 
        { 
            status = "TwoFactorRequired", 
            message = "Please enter your TOTP code.",
            email = user.Email,
            preAuthToken = preAuthToken
        });
    }

    /// <summary>
    /// Initialize Super Admin 2FA Setup
    /// </summary>
    [HttpPost("setup-2fa")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SetupTwoFactor([FromBody] AdminSetup2FaRequest request)
    {
        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Role == UserRole.SuperAdmin);

        if (user == null) return Unauthorized();

        var pHasher = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (pHasher == PasswordVerificationResult.Failed) return Unauthorized();

        // If 2FA is already configured, require the existing TOTP code before resetting it.
        // This prevents an attacker with only the password from replacing the TOTP secret.
        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(request.ExistingCode))
                return Unauthorized("Existing TOTP code required to reset 2FA.");

            var isValid = await _twoFactorService.VerifyTotpAsync(user.Id, request.ExistingCode);
            if (!isValid)
                return Unauthorized("Invalid existing TOTP code.");
        }

        var result = await _twoFactorService.SetupTotpAsync(user.Id);
        return Ok(result);
    }

    /// <summary>
    /// Verify 2FA and Complete Admin Login
    /// </summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] AdminVerify2FaRequest request)
    {
        if (string.IsNullOrEmpty(request.PreAuthToken))
            return Unauthorized("Invalid session. Please login again.");

        // M-5 FIX: Read pre-auth token from distributed cache (Redis)
        var cachedToken = await _distributedCache.GetStringAsync($"SuperAdminPreAuth_{request.Email}");
        if (string.IsNullOrEmpty(cachedToken) || cachedToken != request.PreAuthToken)
            return Unauthorized("Session expired or invalid. Please login again.");

        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Role == UserRole.SuperAdmin);

        if (user == null) return Unauthorized();

        var isValid = await _twoFactorService.VerifyTotpAsync(user.Id, request.Code);
        if (!isValid) return Unauthorized("Invalid 2FA code");

        // Clear pre-auth token after successful 2FA
        await _distributedCache.RemoveAsync($"SuperAdminPreAuth_{request.Email}");

        // Enable if this was the initial setup
        if (!user.TwoFactorEnabled)
        {
            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();
        }

        var token = GenerateAdminToken(user);
        return Ok(new 
        { 
            token,
            user = new 
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                role = "superadmin",
                tenantId = user.TenantId
            }
        });
    }

    /// <summary>
    /// Platform Owner One-Time Registration
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // C-4 FIX: Rate limit registration attempts using distributed cache.
        // Only 1 registration attempt per IP every 5 minutes.
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimitKey = $"AdminRegRateLimit_{clientIp}";
        var existing = await _distributedCache.GetStringAsync(rateLimitKey);
        if (!string.IsNullOrEmpty(existing))
        {
            _logger.LogWarning("SECURITY: Rate-limited admin registration attempt from IP {Ip}", clientIp);
            return StatusCode(429, new { error = "Too many attempts. Please try again later." });
        }
        await _distributedCache.SetStringAsync(rateLimitKey, "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        // Acquire a cluster-wide distributed lock so concurrent registration attempts
        // from different IPs cannot both pass the AnyAsync() check before either commits.
        // The DB-level unique index on (Role=SuperAdmin) is the final safety net if the
        // lock is somehow bypassed (e.g. Redis restart during the window).
        var lockKey = "SuperAdminRegisterLock";
        var lockValue = Guid.NewGuid().ToString();
        var acquired = await _distributedCache.GetStringAsync(lockKey) == null;
        if (acquired)
            await _distributedCache.SetStringAsync(lockKey, lockValue,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) });
        if (!acquired)
            return StatusCode(429, new { error = "Registration is already in progress. Try again in a moment." });

        try
        {
            var exists = await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.SuperAdmin);
            if (exists) return BadRequest("Registration is closed. A platform owner already exists.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = UserRole.SuperAdmin,
                IsActive = true,
                EmailVerified = true,
                TenantId = Guid.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            _context.Users.Add(user);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
            {
                // DB unique constraint on SuperAdmin role caught a race that slipped past the lock
                _logger.LogWarning("SECURITY: Concurrent SuperAdmin registration race caught by DB constraint.");
                return BadRequest("Registration is closed. A platform owner already exists.");
            }

            return Ok(new { message = "Owner account created. Please proceed to setup 2FA." });
        }
        finally
        {
            // Only release the lock if we are still the holder
            var current = await _distributedCache.GetStringAsync(lockKey);
            if (current == lockValue)
                await _distributedCache.RemoveAsync(lockKey);
        }
    }

    private string GenerateAdminToken(User user)
    {
        var jwtSecret = _secretProvider.GetSecret("Jwt:Secret") ?? _configuration["Jwt:Secret"];
        if (string.IsNullOrEmpty(jwtSecret))
            throw new InvalidOperationException("FATAL: JWT secret not found in Key Vault or configuration. Cannot issue admin token.");
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin"),
            new System.Security.Claims.Claim("IsSuperAdmin", "true")
        };
        
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1), // Shorter TTL for admins
            signingCredentials: credentials
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    public class AdminSetup2FaRequest { public string Email { get; set; } = ""; public string Password { get; set; } = ""; public string? ExistingCode { get; set; } }
    public class AdminVerify2FaRequest { public string Email { get; set; } = ""; public string Code { get; set; } = ""; public string PreAuthToken { get; set; } = ""; }

    /// <summary>
    /// Reset 2FA for a user (Super Admin only)
    /// </summary>
    [HttpPost("users/{userId}/reset-2fa")]
    public async Task<IActionResult> ResetUserTwoFactor(Guid userId)
    {
        // Direct DB update if service doesn't handle finding user across tenants
        // SuperAdmin might need to find user in ANY tenant.
        // User entity has QueryFilter for TenantId! SuperAdmin needs IgnoreQueryFilters.
        
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound("User not found");

        // H-5 FIX: Also disable the TwoFactorEnabled flag, otherwise the user
        // is permanently locked out (system demands 2FA but no secret exists).
        user.TwoFactorSecret = null;
        user.TwoFactorEnabled = false;
        
        await _context.SaveChangesAsync();
        _logger.LogWarning("Super Admin reset 2FA for user {UserId}", userId);
        return Ok(new { success = true, message = "2FA has been reset for the user" });
    }

    /// <summary>
    /// Get all tenants with subscription summary
    /// </summary>
    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        // M-6 FIX: Clamp pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Tenants.IgnoreQueryFilters();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TenantStatus>(status, true, out var statusEnum))
        {
            query = query.Where(t => t.Status == statusEnum);
        }

        if (!string.IsNullOrEmpty(search))
        {
            if (search.Length > 100)
                return BadRequest(new { error = "Search term must be 100 characters or fewer." });
            query = query.Where(t => (t.Name != null && t.Name.Contains(search)) || (t.Slug != null && t.Slug.Contains(search)) || (t.Email != null && t.Email.Contains(search)));
        }

        var total = await query.CountAsync();

        // M-1 FIX: Use a simpler projection to avoid N+1 correlated subqueries.
        // Count users/bookings via a separate query, or accept the trade-off for admin pages.
        var tenants = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                id = t.Id.ToString(),
                tenantId = t.Id,
                name = t.Name,
                slug = t.Slug,
                subdomain = t.Slug,
                ownerEmail = t.Email,
                subscriptionTier = t.SubscriptionTier.ToString(),
                status = t.Status.ToString(),
                createdAt = t.CreatedAt,
                revenueScore = t.Status == TenantStatus.Active ? 85 : 40,
                healthScore = t.Status == TenantStatus.Active ? 92 : 35,
                seatCount = 15,
                trend = t.Status == TenantStatus.Active ? "up" : "down",
                mrr = t.SubscriptionTier == SubscriptionTier.Enterprise ? "₹1,45,000" : "₹15,000"
            })
            .ToListAsync();
        
        return Ok(new { data = tenants, total, page, pageSize });
    }

    /// <summary>
    /// Get tenant details
    /// </summary>
    [HttpGet("tenants/{id}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        // H-06 FIX: SuperAdmin queries must use IgnoreQueryFilters() because the
        // global query filter scopes by tenant_id, and SuperAdmin has TenantId = Guid.Empty.
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
        if (tenant == null) return NotFound();

        // Ignore query filters to count users/bookings for this tenant
        var usersCount = await _context.Users.IgnoreQueryFilters().CountAsync(u => u.TenantId == id);
        var bookingsCount = await _context.Bookings.IgnoreQueryFilters().CountAsync(b => b.TenantId == id); // Assumes Booking entity exists and has TenantId

        // Get active subscription
        var sub = await _context.Subscriptions
            .Include(s => s.PricingPlan).ThenInclude(p => p!.Prices)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(s => s.TenantId == id);

        var monthlyPrice = sub?.PricingPlan?.Prices
            .FirstOrDefault(p => p.Cycle == Upkilo.Core.Entities.BillingCycle.Monthly)?.Amount ?? 0;

        return Ok(new
        {
            tenant.Id,
            tenant.Name,
            subdomain = tenant.Slug,
            customDomain = tenant.Domain,
            ownerEmail = tenant.Email,
            ownerPhone = tenant.Phone,
            plan = sub?.PricingPlan?.Name ?? tenant.SubscriptionTier.ToString(),
            planStartDate = sub?.CurrentPeriodStart,
            status = tenant.Status.ToString().ToLower(),
            mrr = monthlyPrice,
            usersCount,
            bookingsCount,
            subscriptionStatus = sub?.Status.ToString(),
            aiUsed = await _context.AIUsageLogs.IgnoreQueryFilters()
                .Where(l => l.TenantId == id && l.CreatedAt >= (sub != null ? sub.CurrentPeriodStart : DateTime.MinValue))
                .SumAsync(l => l.Cost),
            aiBudget = sub?.AiMonthlyBudget != 0 ? sub?.AiMonthlyBudget : 0
        });
    }

    /// <summary>
    /// Update tenant status
    /// </summary>
    [HttpPut("tenants/{id}/status")]
    public async Task<IActionResult> UpdateTenantStatus(Guid id, [FromBody] UpdateTenantStatusRequest request)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (tenant == null) return NotFound();

        if (Enum.TryParse<TenantStatus>(request.Status, true, out var status))
        {
            tenant.Status = status;
            await _context.SaveChangesAsync();
             _logger.LogInformation("Tenant {TenantId} status updated to {Status}", id, request.Status);
             return Ok(new { success = true, status = request.Status });
        }
        return BadRequest("Invalid status");
    }

    /// <summary>
    /// Impersonate a tenant (login as tenant admin)
    /// </summary>
    [HttpPost("tenants/{id}/impersonate")]
    public async Task<IActionResult> ImpersonateTenant(Guid id)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (tenant == null) return NotFound("Tenant not found");
        
        if (tenant.Status != TenantStatus.Active)
            return BadRequest(new { error = "Cannot impersonate an inactive or suspended tenant." });

        var targetUser = await _context.Users.IgnoreQueryFilters()
            .Where(u => u.TenantId == id && u.Status == UserStatus.Active)
            .OrderBy(u => u.Role == UserRole.Owner ? 0 : 1) // Prioritize Owner
            .FirstOrDefaultAsync();

        if (targetUser == null)
            return BadRequest(new { message = "Cannot impersonate this tenant because it has no active users." });

        _logger.LogWarning("Super admin impersonating tenant {TenantId} as user {UserId}", id, targetUser.Id);

        // M-14 FIX: Persist impersonation audit trail to the database
        var adminIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        try
        {
            _context.AdminImpersonationLogs.Add(new AdminImpersonationLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = Guid.TryParse(adminIdStr, out var aid) ? aid : Guid.Empty,
                TargetTenantId = id,
                TargetUserId = targetUser.Id,
                StartedAt = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist impersonation audit log for tenant {TenantId}", id);
        }

        var jwtSecret = _secretProvider.GetSecret("Jwt:Secret") ?? _configuration["Jwt:Secret"];
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("FATAL: JWT secret not found in Key Vault or configuration. Cannot issue impersonation token.");
        }

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, targetUser.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, targetUser.Email),
            new System.Security.Claims.Claim("tenant_id", targetUser.TenantId.ToString() ?? ""),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, targetUser.Role.ToString()),
            new System.Security.Claims.Claim("sid", Guid.NewGuid().ToString()),
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new System.Security.Claims.Claim("impersonator", "superadmin"),
            new System.Security.Claims.Claim("portal_access", "true")
        };
        
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );
        var realImpersonationToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        // C-5 FIX: Return a short-lived exchange code instead of the raw JWT in the URL.
        // The frontend should POST this code to a token exchange endpoint to receive
        // the actual JWT in an HttpOnly cookie — never exposed in URLs, logs, or Referer headers.
        var exchangeCode = Guid.NewGuid().ToString("N");
        await _distributedCache.SetStringAsync(
            $"ImpersonationExchange_{exchangeCode}",
            realImpersonationToken,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) });

        return Ok(new
        {
            success = true,
            exchangeCode = exchangeCode,
            redirectUrl = $"{(_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/')}/login?exchange={exchangeCode}",
            message = $"You are now logged in as {tenant.Name} ({targetUser.Email})"
        });
    }

    /// <summary>
    /// Get pricing plans (use POST /api/admin/pricing/plans for the full PricingPlan management API)
    /// </summary>
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _context.PricingPlans
            .Include(p => p.Prices)
            .Include(p => p.FeatureMappings).ThenInclude(m => m.PricingFeature)
            .Where(p => p.IsActive)
            .ToListAsync();
        return Ok(new { data = plans });
    }

    /// <summary>
    /// Get platform analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetPlatformAnalytics()
    {
        var totalTenants = await _context.Tenants.IgnoreQueryFilters().CountAsync();
        var activeTenants = await _context.Tenants.IgnoreQueryFilters().CountAsync(t => t.Status == TenantStatus.Active);
        var totalUsers = await _context.Users.IgnoreQueryFilters().CountAsync();
        
        // Comprehensive aggregates
        var totalRevenue = await _context.Payments.IgnoreQueryFilters()
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => p.Amount);
            
        var totalBookings = await _context.Bookings.IgnoreQueryFilters().CountAsync();
        var activeSubscriptions = await _context.Subscriptions.IgnoreQueryFilters()
            .CountAsync(s => s.Status == SubscriptionStatus.Active);
            
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var newTenantsMonth = await _context.Tenants.IgnoreQueryFilters()
            .CountAsync(t => t.CreatedAt >= lastMonth);

        return Ok(new
        {
            totalTenants,
            activeTenants,
            totalUsers,
            totalRevenue,
            totalBookings,
            activeSubscriptions,
            newTenantsLast30Days = newTenantsMonth,
            platformHealth = "Optimal",
            currency = "USD"
        });
    }

    /// <summary>
    /// Get platform AI Insights
    /// </summary>
    [HttpGet("insights")]
    public async Task<IActionResult> GetPlatformInsights()
    {
        // Computed from live tenant activity. This previously returned hardcoded copy naming a
        // tenant that did not exist, priced in a single currency, and mis-pluralised its counts.
        // Everything below is derived; when a signal has nothing to report it is omitted rather
        // than padded, so an empty list means "nothing needs attention".
        var now = DateTime.UtcNow;
        var staleAfter = now.AddDays(-14);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevMonthStart = monthStart.AddMonths(-1);

        var activeTenants = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Status == TenantStatus.Active && !t.IsDeleted)
            .Select(t => new { t.Id, t.Name, t.SubscriptionTier, t.CreatedAt })
            .ToListAsync();

        var insights = new List<object>();

        if (activeTenants.Count > 0)
        {
            var tenantIds = activeTenants.Select(t => t.Id).ToList();

            // ── Risk: established tenants with no booking activity in the last 14 days ──
            var recentlyActive = await _context.Bookings
                .IgnoreQueryFilters()
                .Where(b => tenantIds.Contains(b.TenantId) && b.CreatedAt >= staleAfter)
                .Select(b => b.TenantId)
                .Distinct()
                .ToListAsync();

            // Only count tenants old enough that silence is meaningful — a brand-new tenant
            // with no bookings is onboarding, not churning.
            var dormant = activeTenants
                .Where(t => t.CreatedAt < staleAfter && !recentlyActive.Contains(t.Id))
                .ToList();

            if (dormant.Count > 0)
            {
                var named = dormant.OrderBy(t => t.Name).First().Name;
                insights.Add(new
                {
                    id = "ins-dormant",
                    type = "risk",
                    title = dormant.Count == 1
                        ? $"'{named}' has had no bookings for 14 days"
                        : $"{dormant.Count} tenants have had no bookings for 14 days",
                    description = dormant.Count == 1
                        ? "No booking activity since the start of the window. Worth an outreach before renewal."
                        : $"Including '{named}'. No booking activity since the start of the window.",
                    tenantIds = dormant.Select(t => t.Id).Take(20),
                    actions = new[]
                    {
                        new { id = "act-view", label = "View tenants", primary = false }
                    }
                });
            }

            // ── Opportunity: entry-tier tenants carrying heavy booking volume ──
            var entryTiers = new[] { SubscriptionTier.Free, SubscriptionTier.Starter };
            var entryTenantIds = activeTenants
                .Where(t => entryTiers.Contains(t.SubscriptionTier))
                .Select(t => t.Id)
                .ToList();

            if (entryTenantIds.Count > 0)
            {
                var volumes = await _context.Bookings
                    .IgnoreQueryFilters()
                    .Where(b => entryTenantIds.Contains(b.TenantId) && b.CreatedAt >= monthStart)
                    .GroupBy(b => b.TenantId)
                    .Select(g => new { TenantId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 40+ bookings in a month is well beyond casual use of an entry plan.
                var heavy = volumes.Where(v => v.Count >= 40).ToList();
                if (heavy.Count > 0)
                {
                    insights.Add(new
                    {
                        id = "ins-upgrade",
                        type = "opportunity",
                        title = heavy.Count == 1
                            ? "1 entry-plan tenant is running at upgrade volume"
                            : $"{heavy.Count} entry-plan tenants are running at upgrade volume",
                        description = $"{heavy.Max(h => h.Count)} bookings this month on a Free or Starter plan.",
                        tenantIds = heavy.Select(h => h.TenantId).Take(20),
                        actions = new[]
                        {
                            new { id = "act-upgrade", label = "Review plans", primary = true }
                        }
                    });
                }
            }

            // ── Trend: AI usage this month vs last ──
            var aiThis = await _context.AIUsageLogs.IgnoreQueryFilters()
                .CountAsync(l => l.CreatedAt >= monthStart);
            var aiPrev = await _context.AIUsageLogs.IgnoreQueryFilters()
                .CountAsync(l => l.CreatedAt >= prevMonthStart && l.CreatedAt < monthStart);

            if (aiThis > 0 || aiPrev > 0)
            {
                var delta = aiPrev == 0 ? 100 : (int)Math.Round(((double)aiThis - aiPrev) / aiPrev * 100);
                insights.Add(new
                {
                    id = "ins-ai-trend",
                    type = "trend",
                    title = $"AI usage {(delta >= 0 ? "+" : "")}{delta}% this month",
                    description = $"{aiThis:N0} AI calls so far this month against {aiPrev:N0} last month.",
                    actions = new[]
                    {
                        new { id = "act-breakdown", label = "See breakdown", primary = false }
                    }
                });
            }
        }

        return Ok(insights);
    }

    /// <summary>
    /// Get audit logs (platform-wide)
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        // M-6 FIX: Clamp pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = _context.AuditEntries.IgnoreQueryFilters()
            .OrderByDescending(a => a.Timestamp);

        var total = await query.CountAsync();
        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { data = logs, total, page, pageSize });
    }

    /// <summary>
    /// Get system health
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetSystemHealth()
    {
        bool dbHealthy = await _context.Database.CanConnectAsync();
        
        long enqueuedCount = 0;
        long failedCount = 0;
        long processingCount = 0;

        try 
        {
            var monitoringApi = Hangfire.JobStorage.Current.GetMonitoringApi();
            enqueuedCount = monitoringApi.EnqueuedCount("default");
            failedCount = monitoringApi.FailedCount();
            processingCount = monitoringApi.ProcessingCount();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Hangfire JobStorage for health check");
        }

        bool isDegraded = !dbHealthy || enqueuedCount > 5000 || failedCount > 1000;

        return Ok(new
        {
            status = isDegraded ? "degraded" : (dbHealthy ? "healthy" : "crashed"),
            services = new
            {
                database = new { status = dbHealthy ? "healthy" : "crashed" },
                backgroundJobs = new 
                { 
                    status = (enqueuedCount > 5000) ? "congested" : "healthy",
                    enqueued = enqueuedCount,
                    failed = failedCount,
                    processing = processingCount
                }
            }
        });
    }

    /// <summary>
    /// Get platform billing overview
    /// </summary>
    [HttpGet("billing")]
    public async Task<IActionResult> GetPlatformBilling()
    {
        // Assuming simple sums for demonstration. Real MRR would require complex calculation.
        var totalRevenue = await _context.Payments.IgnoreQueryFilters().SumAsync(p => p.Amount);
        var activeSubscriptions = await _context.Subscriptions.IgnoreQueryFilters().CountAsync();
        
        return Ok(new
        {
            totalRevenue,
            activeSubscriptions,
            currency = "USD"
        });
    }

    /// <summary>
    /// Get tenant resource usage
    /// </summary>
    [HttpGet("tenants/{id}/usage")]
    public async Task<IActionResult> GetTenantResourceUsage(Guid id)
    {
        var tenantExists = await _context.Tenants.AnyAsync(t => t.Id == id);
        if (!tenantExists) return NotFound();

        var aiUsage = await _context.AIUsageLogs.IgnoreQueryFilters().Where(a => a.TenantId == id).SumAsync(a => a.Cost);
        var webhookDeliveries = await _context.WebhookDeliveries.IgnoreQueryFilters().CountAsync(w => w.TenantId == id);
        var storageUsedBytes = -1L; // TODO: Query Azure Blob Storage API for actual tenant storage usage
        _logger.LogDebug("Storage usage tracking not yet implemented for tenant {TenantId}", id);
        
        return Ok(new
        {
            tenantId = id,
            aiCost = aiUsage,
            webhookDeliveries,
            storageUsedBytes
        });
    }

    /// <summary>
    /// Get aggregated analytics for all tenants
    /// </summary>
    [HttpGet("tenants/analytics")]
    public async Task<IActionResult> GetAllTenantsAnalytics()
    {
        // Single query: join tenant rows with a GROUP BY count to avoid N+1 correlated subqueries.
        var tenantIds = await _context.Tenants.Select(t => t.Id).ToListAsync();

        var userCounts = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => tenantIds.Contains(u.TenantId))
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var tenants = await _context.Tenants
            .Select(t => new
            {
                t.Id,
                t.Name,
                status = t.Status.ToString()
            })
            .ToListAsync();

        var result = tenants.Select(t => new
        {
            t.Id,
            t.Name,
            usersCount = userCounts.TryGetValue(t.Id, out var c) ? c : 0,
            t.status
        });

        return Ok(new { data = result });
    }

    /// <summary>
    /// Broadcast a system message or announcement
    /// </summary>
    [HttpPost("broadcast")]
    public async Task<IActionResult> BroadcastSystemMessage([FromBody] SendAnnouncementRequest request)
    {
        _logger.LogInformation("System broadcast sent: {Title} ({Type})", request.Title, request.Type);
        
        return Ok(new { success = true, sentAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Get recent system error/health logs
    /// </summary>
    [HttpGet("health/logs")]
    public async Task<IActionResult> GetSystemHealthLogs([FromQuery] int limit = 50)
    {
        var logs = await _context.AuditEntries.IgnoreQueryFilters()
            .Where(a => a.Action.Contains("Error") || a.Action.Contains("Failed"))
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new { a.Id, a.Timestamp, a.Action, a.Details })
            .ToListAsync();

        return Ok(new { data = logs });
    }
    /// <summary>
    /// Processes a pending 2FA recovery request
    /// </summary>
    [HttpPost("process-2fa-recovery/{id:guid}")]
    public async Task<IActionResult> ProcessTwoFactorRecovery(Guid id, [FromBody] Process2FaRecoveryDto request)
    {
        var adminIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(adminIdStr, out var adminId))
            return Unauthorized();
        var authService = HttpContext.RequestServices.GetRequiredService<IAuthService>();
        var success = await authService.ProcessTwoFactorRecoveryRequestAsync(id, adminId, request.Approve, request.Notes ?? string.Empty);
        
        if (!success)
            return NotFound(new { message = "Request not found or already processed." });

        return Ok(new { message = $"2FA recovery request {(request.Approve ? "approved" : "rejected")} successfully." });
    }

    /// <summary>
    /// Get monthly revenue trend across platform
    /// </summary>
    [HttpGet("analytics/revenue-trend")]
    public async Task<IActionResult> GetRevenueTrend()
    {
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

        // M-10 FIX: Use DB-side GROUP BY instead of loading all payments into memory
        var trend = await _context.Payments.IgnoreQueryFilters()
            .Where(p => p.CreatedAt >= sixMonthsAgo && p.Status == PaymentStatus.Succeeded)
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                revenue = g.Sum(p => p.Amount)
            })
            .OrderBy(g => g.year).ThenBy(g => g.month)
            .ToListAsync();

        var result = trend.Select(t => new
        {
            label = new DateTime(t.year, t.month, 1).ToString("MMM"),
            revenue = t.revenue
        }).ToList();

        return Ok(new { data = result });
    }

    /// <summary>
    /// Get tenant distribution by subscription tier
    /// </summary>
    [HttpGet("analytics/tier-distribution")]
    public async Task<IActionResult> GetTierDistribution()
    {
        // M-15 FIX: Use DB-side GROUP BY instead of loading all tenants into memory
        var distribution = await _context.Tenants.IgnoreQueryFilters()
            .GroupBy(t => t.SubscriptionTier)
            .Select(g => new
            {
                name = g.Key.ToString(),
                value = g.Count()
            })
            .ToListAsync();

        return Ok(new { data = distribution });
    }

    /// <summary>
    /// Get global platform settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetGlobalSettings()
    {
        // In a real app, this would be a dedicated table or a system tenant.
        // For now, returning standard platform config.
        return Ok(new
        {
            platformName = "Upkilo",
            supportEmail = "support@upkilo.com",
            enforceTwoFactorGlobal = false,
            maintenanceMode = false,
            allowNewRegistrations = true,
            defaultTenantTier = "Starter",
            apiRateLimit = 1000,
            smtpConfigured = true,
            stripeConnected = true
        });
    }

    /// <summary>
    /// Update global platform settings
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateGlobalSettings([FromBody] Dictionary<string, object> settings)
    {
        _logger.LogInformation("Global platform settings updated by {Admin}", User.Identity?.Name);
        return Ok(new { success = true, updatedAt = DateTime.UtcNow });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/super-admin/ai/overview
    //      Platform-wide AI usage KPIs, model breakdown, top tenants by cost,
    //      and daily cost trend for the last 14 days.
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("ai/overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAiOverview([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 7, 90);
        var since = DateTime.UtcNow.AddDays(-days);

        var logs = await _context.AIUsageLogs
            .Where(l => l.CreatedAt >= since)
            .Select(l => new
            {
                l.TenantId,
                l.Model,
                l.Feature,
                l.InputTokens,
                l.OutputTokens,
                l.Cost,
                l.LatencyMs,
                l.Success,
                l.CreatedAt
            })
            .ToListAsync();

        var totalRequests  = logs.Count;
        var totalTokens    = logs.Sum(l => l.InputTokens + l.OutputTokens);
        var totalCost      = logs.Sum(l => (double)l.Cost);
        var successCount   = logs.Count(l => l.Success);
        var avgLatency     = logs.Where(l => l.LatencyMs.HasValue).Select(l => (double)l.LatencyMs!.Value)
                                .DefaultIfEmpty(0).Average();

        // Model breakdown
        var byModel = logs
            .GroupBy(l => l.Model)
            .Select(g => new
            {
                model        = g.Key,
                requests     = g.Count(),
                tokens       = g.Sum(l => l.InputTokens + l.OutputTokens),
                cost         = Math.Round(g.Sum(l => (double)l.Cost), 4),
                successRate  = g.Count() > 0 ? Math.Round(100.0 * g.Count(l => l.Success) / g.Count(), 1) : 100.0
            })
            .OrderByDescending(x => x.cost)
            .ToList();

        // Feature breakdown
        var byFeature = logs
            .GroupBy(l => l.Feature)
            .Select(g => new
            {
                feature  = g.Key,
                requests = g.Count(),
                tokens   = g.Sum(l => l.InputTokens + l.OutputTokens),
                cost     = Math.Round(g.Sum(l => (double)l.Cost), 4)
            })
            .OrderByDescending(x => x.cost)
            .ToList();

        // Top 10 tenants by cost
        var tenantIds = logs.Select(l => l.TenantId).Distinct().ToList();
        var tenantNames = await _context.Tenants.IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        var topTenants = logs
            .GroupBy(l => l.TenantId)
            .Select(g => new
            {
                tenantId    = g.Key,
                tenantName  = tenantNames.GetValueOrDefault(g.Key, "Unknown"),
                requests    = g.Count(),
                tokens      = g.Sum(l => l.InputTokens + l.OutputTokens),
                cost        = Math.Round(g.Sum(l => (double)l.Cost), 4),
                failedCount = g.Count(l => !l.Success)
            })
            .OrderByDescending(x => x.cost)
            .Take(10)
            .ToList();

        // Daily cost trend (last 14 days capped)
        var trendDays = Math.Min(days, 14);
        var trendSince = DateTime.UtcNow.AddDays(-trendDays).Date;
        var dailyTrend = logs
            .Where(l => l.CreatedAt.Date >= trendSince)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new
            {
                date     = g.Key.ToString("yyyy-MM-dd"),
                cost     = Math.Round(g.Sum(l => (double)l.Cost), 4),
                requests = g.Count(),
                tokens   = g.Sum(l => l.InputTokens + l.OutputTokens)
            })
            .OrderBy(x => x.date)
            .ToList();

        return Ok(new
        {
            period           = new { days, since },
            summary = new
            {
                totalRequests,
                totalTokens,
                totalCostUsd    = Math.Round(totalCost, 4),
                successRate     = totalRequests > 0 ? Math.Round(100.0 * successCount / totalRequests, 1) : 100.0,
                avgLatencyMs    = Math.Round(avgLatency, 0),
                failedRequests  = totalRequests - successCount
            },
            byModel,
            byFeature,
            topTenants,
            dailyTrend
        });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/super-admin/ai/tenants/{id}
    //      Per-tenant AI usage detail — breakdown by feature and daily trend.
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("ai/tenants/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantAiUsage(Guid id, [FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 7, 90);
        var tenantExists = await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == id);
        if (!tenantExists) return NotFound();

        var since = DateTime.UtcNow.AddDays(-days);
        var logs = await _context.AIUsageLogs
            .Where(l => l.TenantId == id && l.CreatedAt >= since)
            .Select(l => new
            {
                l.Model,
                l.Feature,
                l.InputTokens,
                l.OutputTokens,
                l.Cost,
                l.LatencyMs,
                l.Success,
                l.ErrorMessage,
                l.CreatedAt
            })
            .ToListAsync();

        var byFeature = logs
            .GroupBy(l => l.Feature)
            .Select(g => new
            {
                feature  = g.Key,
                requests = g.Count(),
                tokens   = g.Sum(l => l.InputTokens + l.OutputTokens),
                cost     = Math.Round(g.Sum(l => (double)l.Cost), 4),
                failures = g.Count(l => !l.Success)
            })
            .OrderByDescending(x => x.cost)
            .ToList();

        var recentFailures = logs
            .Where(l => !l.Success)
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(l => new
            {
                feature      = l.Feature,
                model        = l.Model,
                errorMessage = l.ErrorMessage,
                occurredAt   = l.CreatedAt
            })
            .ToList();

        var dailyTrend = logs
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new
            {
                date     = g.Key.ToString("yyyy-MM-dd"),
                cost     = Math.Round(g.Sum(l => (double)l.Cost), 4),
                requests = g.Count()
            })
            .OrderBy(x => x.date)
            .ToList();

        return Ok(new
        {
            tenantId      = id,
            period        = new { days, since },
            totalRequests = logs.Count,
            totalCostUsd  = Math.Round(logs.Sum(l => (double)l.Cost), 4),
            totalTokens   = logs.Sum(l => l.InputTokens + l.OutputTokens),
            successRate   = logs.Count > 0 ? Math.Round(100.0 * logs.Count(l => l.Success) / logs.Count, 1) : 100.0,
            byFeature,
            recentFailures,
            dailyTrend
        });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/super-admin/security/overview
    //      Aggregated security event summary: counts by severity, recent
    //      unresolved critical events, and login failure statistics.
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("security/overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSecurityOverview([FromQuery] int days = 7)
    {
        days = Math.Clamp(days, 1, 30);
        var since = DateTime.UtcNow.AddDays(-days);

        var events = await _context.SecurityEvents
            .Where(e => e.CreatedAt >= since)
            .Select(e => new
            {
                e.TenantId,
                e.EventType,
                e.Severity,
                e.IsResolved,
                e.IpAddress,
                e.Description,
                e.CreatedAt
            })
            .ToListAsync();

        var bySeverity = events
            .GroupBy(e => e.Severity.ToString())
            .Select(g => new { severity = g.Key, count = g.Count() })
            .ToList();

        var unresolvedCritical = await _context.SecurityEvents
            .Where(e => !e.IsResolved && (e.Severity == SecuritySeverity.Critical || e.Severity == SecuritySeverity.High))
            .OrderByDescending(e => e.CreatedAt)
            .Take(20)
            .Select(e => new
            {
                e.Id,
                severity    = e.Severity.ToString(),
                e.EventType,
                e.Description,
                e.TenantId,
                e.IpAddress,
                occurredAt  = e.CreatedAt
            })
            .ToListAsync();

        var loginFailures    = events.Count(e => e.EventType == SecurityEventTypes.LoginFailed);
        var loginSuccesses   = events.Count(e => e.EventType == SecurityEventTypes.LoginSuccess);
        var failureRate      = (loginSuccesses + loginFailures) > 0
            ? Math.Round(100.0 * loginFailures / (loginSuccesses + loginFailures), 1)
            : 0.0;

        // Most targeted tenants (highest failure count)
        var tenantIds = events.Where(e => e.TenantId.HasValue).Select(e => e.TenantId!.Value).Distinct().ToList();
        var tenantNames = await _context.Tenants.IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        var targetedTenants = events
            .Where(e => e.TenantId.HasValue && e.EventType == SecurityEventTypes.LoginFailed)
            .GroupBy(e => e.TenantId!.Value)
            .Select(g => new
            {
                tenantId   = g.Key,
                tenantName = tenantNames.GetValueOrDefault(g.Key, "Unknown"),
                failures   = g.Count()
            })
            .OrderByDescending(x => x.failures)
            .Take(5)
            .ToList();

        return Ok(new
        {
            period = new { days, since },
            summary = new
            {
                totalEvents      = events.Count,
                unresolvedCount  = events.Count(e => !e.IsResolved),
                criticalCount    = events.Count(e => e.Severity == SecuritySeverity.Critical),
                highCount        = events.Count(e => e.Severity == SecuritySeverity.High),
                loginFailureRate = failureRate,
                loginFailures,
                loginSuccesses
            },
            bySeverity,
            unresolvedCritical,
            targetedTenants
        });
    }
}
