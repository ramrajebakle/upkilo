using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using System.Text.Json;

namespace Upkilo.API.Controllers;

/// <summary>
/// Settings controller for tenant and user configuration.
/// Manages Business, Booking, Notification, Payment settings via Tenant.Settings JSON.
/// Manages Team via User entity.
/// Manages Integrations via TenantIntegration.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ILogger<SettingsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public SettingsController(
        ILogger<SettingsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IPasswordHasher<User> passwordHasher,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
    }

    /// <summary>
    /// Helper to get or create settings dictionary
    /// </summary>
    private Dictionary<string, object> GetSettingsDict(Tenant tenant)
    {
        return tenant.Settings ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Get business settings
    /// </summary>
    [HttpGet("business")]
    public async Task<IActionResult> GetBusinessSettings()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant == null) return NotFound();

        // Ensure Metadata is not null for safe access
        var metadata = tenant.Metadata ?? new Dictionary<string, object>();

        return Ok(new
        {
            name = tenant.Name ?? "",
            subdomain = tenant.Slug ?? "",
            description = tenant.Description ?? metadata.GetValueOrDefault("seo_description")?.ToString() ?? "",
            keywords = metadata.GetValueOrDefault("seo_keywords")?.ToString() ?? "",
            businessType = tenant.Industry ?? tenant.BusinessType ?? "",
            timezone = tenant.Timezone ?? "UTC",
            currency = tenant.Currency ?? "USD",
            dateFormat = metadata.GetValueOrDefault("dateFormat")?.ToString() ?? "MM/DD/YYYY",
            timeFormat = metadata.GetValueOrDefault("timeFormat")?.ToString() ?? "12h",
            logoUrl = tenant.LogoUrl,
            primaryColor = tenant.PrimaryColor ?? "#06B6D4",
            website = tenant.Domain ?? "",
            phone = tenant.Phone ?? metadata.GetValueOrDefault("phone")?.ToString() ?? "",
            email = tenant.Email ?? metadata.GetValueOrDefault("email")?.ToString() ?? $"contact@{tenant.Slug ?? "business"}.com",
            address = new
            {
                line1 = metadata.GetValueOrDefault("address_line1")?.ToString() ?? "",
                line2 = metadata.GetValueOrDefault("address_line2")?.ToString() ?? "",
                city = metadata.GetValueOrDefault("city")?.ToString() ?? "",
                state = metadata.GetValueOrDefault("state")?.ToString() ?? "",
                postalCode = metadata.GetValueOrDefault("postal_code")?.ToString() ?? "",
                country = metadata.GetValueOrDefault("country")?.ToString() ?? ""
            }
        });
    }

    /// <summary>
    /// Update business settings
    /// </summary>
    [HttpPut("business")]
    public async Task<IActionResult> UpdateBusinessSettings([FromBody] UpdateBusinessSettingsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant == null) return NotFound();

        _logger.LogInformation("UpdateBusinessSettings called for Tenant {TenantId}. Payload: {Payload}",
            tenantId, JsonSerializer.Serialize(request));

        // Currency is not settable here. It is a property of the tenant's connected Stripe
        // account — the account's country fixes the currency it settles in — so letting a tenant
        // type one only lets them state it wrongly, then charge in a currency their account has
        // to convert out of.
        //
        // Accepted silently when it already matches, so existing clients that echo the whole
        // settings object back are not broken by a field they did not intend to change.
        if (request.Currency != null &&
            !string.Equals(
                Upkilo.Core.Helpers.Currency.Normalize(request.Currency),
                Upkilo.Core.Helpers.Currency.Normalize(tenant.Currency),
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                error = "currency_not_editable",
                message = "Currency comes from your connected Stripe account and cannot be set here. "
                        + "Connect the account you want to settle in, then use "
                        + "POST /api/v1/payments/connect/sync-currency.",
                currency = Upkilo.Core.Helpers.Currency.Normalize(tenant.Currency)
            });
        }

        if (request.Name != null) tenant.Name = request.Name;
        if (request.Timezone != null) tenant.Timezone = request.Timezone;
        if (request.BusinessType != null) tenant.Industry = request.BusinessType;
        if (request.Description != null) tenant.Description = request.Description;

        // Slug / subdomain (only update if non-empty and different)
        if (!string.IsNullOrWhiteSpace(request.Subdomain) && request.Subdomain != tenant.Slug)
        {
            var slugTaken = await _context.Tenants
                .AnyAsync(t => t.Slug == request.Subdomain && t.Id != tenant.Id);
            if (slugTaken)
                return Conflict(new { error = "That booking URL is already taken. Please choose a different one." });
            tenant.Slug = request.Subdomain.ToLower().Trim();
        }

        // Basic fields
        if (request.Phone != null) tenant.Phone = request.Phone;
        if (request.Email != null) tenant.Email = request.Email;

        // Branding fields
        if (request.LogoUrl != null) tenant.LogoUrl = request.LogoUrl;
        if (request.PrimaryColor != null) tenant.PrimaryColor = request.PrimaryColor;
        if (request.Website != null) tenant.Domain = request.Website;

        if (request.Address != null || request.Keywords != null)
        {
            // Trigger EF change detection by creating a new dictionary
            var metadata = new Dictionary<string, object>(tenant.Metadata ?? new Dictionary<string, object>());

            if (request.Address != null)
            {
                metadata["address_line1"] = request.Address.Line1 ?? "";
                metadata["address_line2"] = request.Address.Line2 ?? "";
                metadata["city"] = request.Address.City ?? "";
                metadata["state"] = request.Address.State ?? "";
                metadata["country"] = request.Address.Country ?? "";
                metadata["postal_code"] = request.Address.PostalCode ?? "";
            }

            if (request.Keywords != null)
                metadata["seo_keywords"] = request.Keywords;

            tenant.Metadata = metadata;
            _logger.LogInformation("Updating address metadata for tenant {TenantId}: {Address}", tenantId, JsonSerializer.Serialize(request.Address));
        }

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully saved business settings for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save business settings for tenant {TenantId}", tenantId);
            throw;
        }
        return Ok(new { success = true });
    }

    // --- Custom Fields ---

    [HttpGet("custom-fields")]
    public async Task<IActionResult> GetCustomFields()
    {
        // Tenant filter applied automatically by QueryFilter
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var fields = await _context.CustomFieldDefinitions
            .Where(f => f.TenantId == tenantId && f.IsActive)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();
        return Ok(fields);
    }

    [HttpPost("custom-fields")]
    public async Task<IActionResult> CreateCustomField([FromBody] CustomFieldDefinition request)
    {
        request.Id = Guid.NewGuid();
        request.TenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        _context.CustomFieldDefinitions.Add(request);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCustomFields), new { id = request.Id }, request);
    }

    [HttpDelete("custom-fields/{id}")]
    public async Task<IActionResult> DeleteCustomField(Guid id)
    {
        var field = await _context.CustomFieldDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (field == null) return NotFound();
        // Assuming RLS handles tenant check, but explicit check is safer if not fully trusted
        var tenantId = _tenantProvider.GetTenantId();
        if (field.TenantId != tenantId) return NotFound();

        _context.CustomFieldDefinitions.Remove(field);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // --- Booking Settings (JSON in Tenant.Settings["booking"]) ---

    [HttpGet("booking")]
    public async Task<IActionResult> GetBookingSettings()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);

        if (tenant?.Settings != null && tenant.Settings.TryGetValue("booking", out var settingsObj))
        {
            return Ok(settingsObj);
        }

        // Return defaults if not set
        return Ok(new
        {
            allowOnlineBooking = true,
            minAdvanceBooking = 1,
            maxAdvanceBooking = 30,
            slotDuration = 30,
            allowCancellation = true,
            cancellationDeadline = 24
        });
    }

    [HttpPut("booking")]
    public async Task<IActionResult> UpdateBookingSettings([FromBody] object request) // Accept raw object to store as JSON
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant == null) return NotFound();

        if (tenant.Settings == null) tenant.Settings = new Dictionary<string, object>();
        tenant.Settings["booking"] = request;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // --- Notification Settings (JSON in Tenant.Settings["notifications"]) ---

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationSettings()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);

        if (tenant?.Settings != null && tenant.Settings.TryGetValue("notifications", out var settingsObj))
        {
            return Ok(settingsObj);
        }

        return Ok(new
        {
            emailBookings = true,
            emailReminders = true,
            emailMarketing = false,
            smsReminders = true,
            pushNotifications = true,
            weeklyReport = true,
            playSound = true,
            showBadge = true
        });
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] object request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant == null) return NotFound();

        if (tenant.Settings == null) tenant.Settings = new Dictionary<string, object>();
        tenant.Settings["notifications"] = request;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("push-key")]
    public async Task<IActionResult> GetPushKey([FromServices] ISecretProvider secretProvider)
    {
        var publicKey = await secretProvider.GetSecretAsync("Push:Vapid:PublicKey");
        return Ok(new { publicKey });
    }

    // --- Payment Settings (JSON in Tenant.Settings["payments"]) ---

    [HttpGet("payments")]
    public async Task<IActionResult> GetPaymentSettings()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);

        if (tenant?.Settings != null && tenant.Settings.TryGetValue("payments", out var settingsObj))
        {
            return Ok(settingsObj);
        }

        return Ok(new { acceptCards = true, currency = "USD" });
    }

    [HttpPut("payments")]
    public async Task<IActionResult> UpdatePaymentSettings([FromBody] object request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant == null) return NotFound();

        if (tenant.Settings == null) tenant.Settings = new Dictionary<string, object>();
        tenant.Settings["payments"] = request;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // --- Integrations (Using TenantIntegration) ---

    [HttpGet("integrations")]
    public async Task<IActionResult> GetIntegrations()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var connected = await _context.TenantIntegrations
            .Where(ti => ti.TenantId == tenantId.Value && ti.IsConnected && !ti.IsDeleted)
            .ToListAsync();

        // Hardcoded catalog of available integrations
        var catalog = new[]
        {
            new { id = "google-calendar", name = "Google Calendar", description = "Sync bookings" },
            new { id = "stripe", name = "Stripe", description = "Payments" },
            new { id = "twilio", name = "Twilio SMS", description = "SMS Notifications" },
            new { id = "zoom", name = "Zoom", description = "Video Calls" }
        };

        var result = catalog.Select(cat => new
        {
            cat.id,
            cat.name,
            cat.description,
            connected = connected.Any(c => c.IntegrationId == cat.id),
            connectedAt = connected.FirstOrDefault(c => c.IntegrationId == cat.id)?.ConnectedAt
        });

        return Ok(new { data = result });
    }

    [HttpPost("integrations/{integrationId}/connect")]
    public async Task<IActionResult> ConnectIntegration(string integrationId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Check if integration already exists
        var existing = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.TenantId == tenantId.Value && ti.IntegrationId == integrationId);

        if (existing != null && existing.IsConnected && !existing.IsDeleted)
        {
            return Ok(new { message = "Integration already connected", connected = true });
        }

        // Create or reactivate
        if (existing == null)
        {
            existing = new TenantIntegration
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                IntegrationId = integrationId,
                IsConnected = true,
                ConnectedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _context.TenantIntegrations.Add(existing);
        }
        else
        {
            existing.IsConnected = true;
            existing.IsDeleted = false;
            existing.ConnectedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Return integration-specific auth URL for OAuth-based integrations
        var authUrl = integrationId switch
        {
            "google-calendar" => $"/api/integrations/google-calendar/auth?tenantId={tenantId}",
            "stripe" => $"/api/integrations/stripe/auth?tenantId={tenantId}",
            "zoom" => $"/api/integrations/zoom/auth?tenantId={tenantId}",
            _ => (string?)null
        };

        return Ok(new
        {
            connected = true,
            authUrl,
            message = $"Integration '{integrationId}' connected successfully"
        });
    }

    [HttpDelete("integrations/{integrationId}")]
    public async Task<IActionResult> DisconnectIntegration(string integrationId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.TenantId == tenantId.Value && ti.IntegrationId == integrationId);

        if (integration != null)
        {
            integration.IsConnected = false;
            integration.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    // --- Team Management (Using User entity) ---

    [HttpGet("team")]
    public async Task<IActionResult> GetTeamMembers()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Query users directly using TenantId filter (enforced by RLS typically, but explicit here for clarity)
        var users = await _context.Users
            .Where(u => u.TenantId == tenantId.Value && u.Status != UserStatus.Inactive)
            .ToListAsync();

        var team = users.Select(u => new
        {
            id = u.Id,
            name = u.FullName,
            email = u.Email,
            role = u.Role.ToString().ToLower(),
            status = u.Status.ToString().ToLower(),
            lastLogin = u.LastLoginAt
        });

        return Ok(new { data = team });
    }

    [HttpPost("team/invite")]
    public async Task<IActionResult> InviteTeamMember([FromBody] InviteTeamMemberRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Check if user already exists in this tenant
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenantId.Value);

        if (existingUser != null)
        {
            return Conflict(new { message = "User already exists in this team" });
        }

        // Create new user
        var newUser = new User
        {
            TenantId = tenantId.Value,
            Email = request.Email,
            FirstName = "", // To be filled by user
            LastName = "",
            Role = Enum.TryParse<UserRole>(request.Role, true, out var role) ? role : UserRole.Staff,
            Status = UserStatus.Pending,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        // Generate a secure random temporary password
        var passwordChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%&*";
        var randomBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var tempPassword = new string(randomBytes.Select(b => passwordChars[b % passwordChars.Length]).ToArray());
        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, tempPassword);

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        await _emailService.SendSystemEmailAsync(
            newUser.Email,
            "You've been invited to join a team on Upkilo",
            $@"<h2>Team Invitation</h2>
               <p>You have been invited to join a team with the role of {role}.</p>
               <p>You can log in at <a href='{(_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/')}/login'>app.upkilo.com/login</a></p>
               <p>Your temporary password is: <strong>{tempPassword}</strong></p>
               <p>Please log in and change your password immediately.</p>"
        );

        return Ok(new { success = true, userId = newUser.Id, message = "Team member created and invitation email sent." });
    }

    [HttpPut("team/{userId}/role")]
    public async Task<IActionResult> UpdateTeamMemberRole(Guid userId, [FromBody] UpdateRoleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId.Value);
        if (user == null) return NotFound();

        if (Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            user.Role = role;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        return BadRequest("Invalid role");
    }

    [HttpDelete("team/{userId}")]
    public async Task<IActionResult> RemoveTeamMember(Guid userId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId.Value);
        if (user == null) return NotFound();

        // Soft delete or set inactive
        user.Status = UserStatus.Inactive;
        // Or actually delete if preferred, but auditing usually requires keeping record
        // _context.Users.Remove(user); 

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // --- API Keys (Delegated to ApiKey entity) ---

    [HttpGet("api-keys")]
    public async Task<IActionResult> GetApiKeys()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var keys = await _context.ApiKeys
            .Where(k => k.TenantId == tenantId.Value && !k.IsDeleted) // RLS might handle TenantId but good to be explicit
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        var result = keys.Select(k => new
        {
            k.Id,
            k.Name,
            k.Prefix,
            lastFourChars = k.LastFourChars,
            k.CreatedAt,
            k.LastUsedAt,
            k.ExpiresAt
        });

        return Ok(new { data = result });
    }

    [HttpPost("api-keys")]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        // Simple creation redirecting to main logic or duplicating standard logic
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var fullKey = $"upk_{Guid.NewGuid().ToString("N")}";

        var keyEntity = new ApiKey
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            Prefix = "upk_",
            LastFourChars = fullKey.Substring(fullKey.Length - 4),
            KeyHash = _passwordHasher.HashPassword(null, fullKey), // Use generic hasher or dedicated key hasher
            Scopes = request.Scopes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ApiKeys.Add(keyEntity);
        await _context.SaveChangesAsync();

        return Ok(new { id = keyEntity.Id, key = fullKey, message = "Store this key safely, it will not be shown again." });
    }

    [HttpDelete("api-keys/{keyId}")]
    public async Task<IActionResult> RevokeApiKey(Guid keyId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var key = await _context.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.TenantId == tenantId.Value);
        if (key != null)
        {
            key.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
        return NoContent();
    }
}

// Request DTOs
public class UpdateBusinessSettingsRequest
{
    public string? Name { get; set; }
    public string? Subdomain { get; set; } // becomes tenant.Slug / booking page URL
    public string? Description { get; set; } // Google meta description + schema
    public string? Keywords { get; set; } // comma-separated SEO keywords
    public string? Timezone { get; set; }
    public string? Currency { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? BusinessType { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? Website { get; set; }
    public AddressDto? Address { get; set; }
}

public class AddressDto
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}

public class InviteTeamMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "staff";
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
}

