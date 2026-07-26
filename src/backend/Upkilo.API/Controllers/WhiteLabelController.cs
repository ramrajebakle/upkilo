
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Attributes;
using Upkilo.API.Infrastructure;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/whitelabel")]
[Authorize]
[FeatureGuard("white_label")]
public class WhiteLabelController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<WhiteLabelController> _logger;

    // WL-05: reserved slugs that must not be assigned to sub-accounts
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
        { "app", "api", "www", "mail", "admin", "upkilo", "support", "help", "dashboard", "status" };

    // WL-05: valid slug pattern — 3-63 chars, lowercase alphanumeric + hyphens, no leading/trailing hyphen
    private static readonly Regex SlugPattern = new(@"^[a-z0-9][a-z0-9\-]{1,61}[a-z0-9]$", RegexOptions.Compiled);

    public WhiteLabelController(AppDbContext context, ITenantProvider tenantProvider, ILogger<WhiteLabelController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var config = await _context.WhiteLabelConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config == null)
        {
            // WL-11: return DTO, not entity
            return Ok(new WhiteLabelConfigDto { PrimaryColor = "#06B6D4" });
        }

        // WL-11: only Owner/Admin roles can see the raw CustomCss
        bool canSeeCss = User.IsInRole("Owner") || User.IsInRole("Admin");
        return Ok(WhiteLabelConfigDto.From(config, includeCss: canSeeCss));
    }

    [HttpPut]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> UpdateConfig([FromBody] WhiteLabelDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // WL-01: sanitize CSS, WL-02: validate URLs — throw on violation → 400
        string? sanitizedCss;
        string? validatedLogo;
        string? validatedFavicon;
        try
        {
            sanitizedCss   = BrandingValidator.SanitizeCss(request.CustomCss);
            validatedLogo  = BrandingValidator.ValidateHttpsUrl(request.CustomLogoUrl, "Logo URL");
            validatedFavicon = BrandingValidator.ValidateHttpsUrl(request.CustomFavicon, "Favicon URL");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        var config = await _context.WhiteLabelConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config == null)
        {
            config = new WhiteLabelConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.WhiteLabelConfigs.Add(config);
        }

        // Reset verification if domain changed
        if (config.CustomDomain != request.CustomDomain)
        {
            config.IsVerified = false;
            config.DomainVerifiedAt = null;
        }

        config.CustomDomain      = request.CustomDomain;
        config.CustomLogoUrl     = validatedLogo;
        config.PrimaryColor      = request.PrimaryColor;
        config.SecondaryColor    = request.SecondaryColor;
        config.RemovePoweredBy   = request.RemovePoweredBy;
        config.CustomFavicon     = validatedFavicon;
        config.CustomCss         = sanitizedCss;
        config.CustomEmailDomain = request.CustomEmailDomain;
        config.UpdatedAt         = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("White-label config updated for tenant {TenantId}", tenantId);
        return Ok(WhiteLabelConfigDto.From(config, includeCss: true));
    }

    [HttpPost("verify-domain")]
    public async Task<IActionResult> VerifyCustomDomain()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var config = await _context.WhiteLabelConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        if (config == null || string.IsNullOrWhiteSpace(config.CustomDomain))
            return BadRequest(new { error = "No custom domain configured." });

        try
        {
            var lookup = new DnsClient.LookupClient();
            var result = await lookup.QueryAsync(config.CustomDomain, DnsClient.QueryType.CNAME);

            var cnameRecord = result.Answers.OfType<DnsClient.Protocol.CNameRecord>().FirstOrDefault();

            bool isValid = cnameRecord != null &&
                          (cnameRecord.CanonicalName.Value.TrimEnd('.').Equals("app.upkilo.com", StringComparison.OrdinalIgnoreCase) ||
                           cnameRecord.CanonicalName.Value.TrimEnd('.').Equals("proxy.upkilo.com", StringComparison.OrdinalIgnoreCase));

            if (isValid)
            {
                config.IsVerified = true;
                config.DomainVerifiedAt = DateTime.UtcNow;
                config.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Custom domain {Domain} verified for tenant {TenantId}", config.CustomDomain, tenantId);
                return Ok(new { success = true, isVerified = true, domain = config.CustomDomain });
            }

            return Ok(new
            {
                success = false,
                isVerified = false,
                error = $"CNAME record for {config.CustomDomain} does not point to app.upkilo.com. Found: {cnameRecord?.CanonicalName.Value ?? "None"}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying DNS for {Domain}", config.CustomDomain);
            return StatusCode(500, new { error = "DNS lookup failed. Please try again later." });
        }
    }

    [HttpPost("verify-email-domain")]
    public async Task<IActionResult> VerifyEmailDomain()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var config = await _context.WhiteLabelConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        if (config == null || string.IsNullOrWhiteSpace(config.CustomEmailDomain))
            return BadRequest(new { error = "No custom email domain configured." });

        try
        {
            var lookup = new DnsClient.LookupClient();

            var spfResult = await lookup.QueryAsync(config.CustomEmailDomain, DnsClient.QueryType.TXT);
            bool spfValid = spfResult.Answers.TxtRecords()
                .Any(r => r.Text.Any(t => t.Contains("v=spf1") && t.Contains("include:upkilo.com")));

            var dkimSelector = "upkilo";
            var dkimDomain = $"{dkimSelector}._domainkey.{config.CustomEmailDomain}";
            var dkimResult = await lookup.QueryAsync(dkimDomain, DnsClient.QueryType.CNAME);
            bool dkimValid = dkimResult.Answers.OfType<DnsClient.Protocol.CNameRecord>()
                .Any(r => r.CanonicalName.Value.TrimEnd('.').EndsWith("dkim.upkilo.com", StringComparison.OrdinalIgnoreCase));

            // WL-12: persist verification result
            config.IsEmailVerified = spfValid && dkimValid;
            config.EmailVerifiedAt  = spfValid && dkimValid ? DateTime.UtcNow : config.EmailVerifiedAt;
            config.UpdatedAt        = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (spfValid && dkimValid)
                return Ok(new { success = true, spfValid, dkimValid });

            return Ok(new { success = false, spfValid, dkimValid, message = "Some DNS records are missing or incorrect." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email DNS for {Domain}", config.CustomEmailDomain);
            return StatusCode(500, new { error = "DNS lookup failed." });
        }
    }

    [HttpGet("sub-accounts")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> GetSubAccounts()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var subAccounts = await _context.Tenants
            .Where(t => t.ParentTenantId == tenantId && !t.IsDeleted)
            .Select(t => new
            {
                t.Id, t.BusinessName, t.Slug, t.Sector, t.CreatedAt,
                status = t.IsSuspended ? "Suspended" : "Active"
            })
            .ToListAsync();

        return Ok(new { data = subAccounts });
    }

    [HttpPost("sub-accounts")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> CreateSubAccount([FromBody] CreateSubAccountRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var parentId = _tenantProvider.GetTenantId();
        if (parentId == null) return Unauthorized();

        // WL-10: case-insensitive tier check using SubscriptionTier enum
        var parent = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == parentId);
        if (parent == null || parent.SubscriptionTier != SubscriptionTier.Agency)
            return BadRequest(new { error = "Only Agency-tier accounts can create sub-accounts." });

        // WL-05: slug format + reserved name check
        if (!SlugPattern.IsMatch(request.Slug))
            return BadRequest(new { error = "Slug must be 3-63 lowercase alphanumeric characters or hyphens, with no leading/trailing hyphen." });

        if (ReservedSlugs.Contains(request.Slug))
            return BadRequest(new { error = $"The subdomain '{request.Slug}' is reserved and cannot be used." });

        // WL-05: slug uniqueness across all tenants
        bool slugTaken = await _context.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Slug == request.Slug);
        if (slugTaken)
            return Conflict(new { error = $"The subdomain '{request.Slug}' is already in use." });

        var newTenant = new Tenant
        {
            BusinessName      = request.BusinessName,
            Slug              = request.Slug,
            Sector            = request.Sector,
            ParentTenantId    = parentId.Value,
            SubscriptionTier  = SubscriptionTier.Starter,
            CreatedAt         = DateTime.UtcNow
        };

        _context.Tenants.Add(newTenant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sub-account {ChildId} created by agency {ParentId}", newTenant.Id, parentId);
        return Ok(new { newTenant.Id, newTenant.BusinessName, newTenant.Slug });
    }

    [HttpGet("billing")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetAgencyBilling()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var subAccounts = await _context.Tenants
            .Where(t => t.ParentTenantId == tenantId && !t.IsDeleted)
            .ToListAsync();

        var activeCount = subAccounts.Count(t => !t.IsSuspended);
        var subAccountTotal = activeCount * 29m;
        var basePlanCost = 199m;

        return Ok(new
        {
            currentCycle = new
            {
                startsAt = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                endsAt   = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month))
            },
            basePlanCost,
            subAccounts = new
            {
                activeCount,
                costPerAccount = 29m,
                totalCost = subAccountTotal
            },
            estimatedTotal = basePlanCost + subAccountTotal
        });
    }
}

// WL-06: validated input DTO with data annotations
public class WhiteLabelDto
{
    [MaxLength(253)]
    [RegularExpression(@"^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "CustomDomain must be a valid hostname.")]
    public string? CustomDomain { get; set; }

    [MaxLength(2048)]
    public string? CustomLogoUrl { get; set; }

    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "PrimaryColor must be a 6-digit hex color code (e.g. #2563EB).")]
    public string? PrimaryColor { get; set; }

    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "SecondaryColor must be a 6-digit hex color code.")]
    public string? SecondaryColor { get; set; }

    public bool RemovePoweredBy { get; set; }

    [MaxLength(2048)]
    public string? CustomFavicon { get; set; }

    [MaxLength(50_000)]
    public string? CustomCss { get; set; }

    [MaxLength(253)]
    [RegularExpression(@"^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "CustomEmailDomain must be a valid domain.")]
    public string? CustomEmailDomain { get; set; }
}

// WL-11: response DTO — never leaks internal fields
public class WhiteLabelConfigDto
{
    public string? CustomDomain { get; set; }
    public string? CustomLogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public bool RemovePoweredBy { get; set; }
    public string? CustomFavicon { get; set; }
    public string? CustomCss { get; set; }        // only included for Owner/Admin
    public string? CustomEmailDomain { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? DomainVerifiedAt { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

    public static WhiteLabelConfigDto From(WhiteLabelConfig c, bool includeCss)
    {
        return new WhiteLabelConfigDto
        {
            CustomDomain      = c.CustomDomain,
            CustomLogoUrl     = c.CustomLogoUrl,
            PrimaryColor      = c.PrimaryColor,
            SecondaryColor    = c.SecondaryColor,
            RemovePoweredBy   = c.RemovePoweredBy,
            CustomFavicon     = c.CustomFavicon,
            CustomCss         = includeCss ? c.CustomCss : null,
            CustomEmailDomain = c.CustomEmailDomain,
            IsVerified        = c.IsVerified,
            DomainVerifiedAt  = c.DomainVerifiedAt,
            IsEmailVerified   = c.IsEmailVerified,
            EmailVerifiedAt   = c.EmailVerifiedAt
        };
    }
}

public class CreateSubAccountRequest
{
    [Required, MaxLength(100)]
    public string BusinessName { get; set; } = string.Empty;

    [Required, MaxLength(63)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Sector { get; set; } = string.Empty;
}
