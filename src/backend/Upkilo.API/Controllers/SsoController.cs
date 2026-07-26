using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Upkilo.API.Attributes;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers;

/// <summary>
/// Enterprise SSO/SAML configuration controller.
/// Manages SAML and OIDC identity provider settings for tenant SSO.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[FeatureGuard("advanced_security")]
public class SsoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SsoController> _logger;
    private readonly IAuthService _authService;
    private readonly SsoIntegrationService _ssoIntegrationService;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;

    public SsoController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        IAuthService authService,
        SsoIntegrationService ssoIntegrationService,
        IConfiguration configuration,
        IDistributedCache cache,
        ILogger<SsoController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _authService = authService;
        _cache = cache;
        _ssoIntegrationService = ssoIntegrationService;
        _configuration = configuration;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Get SSO configuration for the current tenant (both SsoConfig and SamlConfiguration)
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var tenantId = GetTenantId();

        var ssoConfig = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);

        var samlConfig = await _context.SamlConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);

        return Ok(new
        {
            sso = ssoConfig != null ? new
            {
                ssoConfig.Id,
                ssoConfig.Provider,
                ssoConfig.Protocol,
                ssoConfig.EntityId,
                ssoConfig.MetadataUrl,
                ssoConfig.SignInUrl,
                hasCertificate = !string.IsNullOrEmpty(ssoConfig.Certificate),
                ssoConfig.ClientId,
                hasClientSecret = !string.IsNullOrEmpty(ssoConfig.ClientSecret),
                ssoConfig.AttributeMapping,
                ssoConfig.IsEnabled,
                ssoConfig.EnforceForAllUsers,
                ssoConfig.CreatedAt,
                ssoConfig.UpdatedAt
            } : null,
            saml = samlConfig != null ? new
            {
                samlConfig.Id,
                samlConfig.IsEnabled,
                samlConfig.EntityId,
                samlConfig.IdpMetadataUrl,
                hasCertificate = !string.IsNullOrEmpty(samlConfig.IdpCertificate),
                samlConfig.SignOnUrl,
                samlConfig.LogoutUrl,
                samlConfig.AttributeMapping,
                samlConfig.AllowPasswordLogin,
                samlConfig.AutoCreateUsers,
                samlConfig.DefaultRoleId,
                samlConfig.CreatedAt,
                samlConfig.UpdatedAt
            } : null
        });
    }

    /// <summary>
    /// Create or update SSO configuration
    /// </summary>
    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateSsoConfigRequest request)
    {
        var tenantId = GetTenantId();

        if (string.IsNullOrWhiteSpace(request.Provider))
            return BadRequest(new { error = "SSO provider is required." });

        var existing = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);

        if (existing == null)
        {
            existing = new SsoConfig { TenantId = tenantId };
            _context.SsoConfigs.Add(existing);
        }

        existing.Provider = request.Provider;
        existing.Protocol = request.Protocol ?? "SAML";
        if (request.EntityId != null) existing.EntityId = request.EntityId;
        if (request.MetadataUrl != null) existing.MetadataUrl = request.MetadataUrl;
        if (request.SignInUrl != null) existing.SignInUrl = request.SignInUrl;
        if (request.Certificate != null) existing.Certificate = request.Certificate;
        if (request.ClientId != null) existing.ClientId = request.ClientId;
        if (request.ClientSecret != null) existing.ClientSecret = request.ClientSecret;
        if (request.AttributeMapping != null) existing.AttributeMapping = request.AttributeMapping;
        if (request.IsEnabled.HasValue) existing.IsEnabled = request.IsEnabled.Value;
        if (request.EnforceForAllUsers.HasValue) existing.EnforceForAllUsers = request.EnforceForAllUsers.Value;
        existing.UpdatedAt = DateTime.UtcNow;

        // Also update the SAML-specific config if protocol is SAML
        if (existing.Protocol == "SAML")
        {
            var saml = await _context.SamlConfigurations
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);

            if (saml == null)
            {
                saml = new SamlConfiguration { TenantId = tenantId };
                _context.SamlConfigurations.Add(saml);
            }

            saml.IsEnabled = existing.IsEnabled;
            saml.EntityId = existing.EntityId ?? string.Empty;
            saml.IdpMetadataUrl = existing.MetadataUrl ?? string.Empty;
            saml.IdpCertificate = existing.Certificate;
            saml.SignOnUrl = existing.SignInUrl;
            if (request.LogoutUrl != null) saml.LogoutUrl = request.LogoutUrl;
            if (request.AttributeMapping != null) saml.AttributeMapping = request.AttributeMapping;
            if (request.AllowPasswordLogin.HasValue) saml.AllowPasswordLogin = request.AllowPasswordLogin.Value;
            if (request.AutoCreateUsers.HasValue) saml.AutoCreateUsers = request.AutoCreateUsers.Value;
            if (request.DefaultRoleId != null) saml.DefaultRoleId = request.DefaultRoleId;
            saml.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("SSO config updated for tenant {TenantId}, provider: {Provider}", tenantId, request.Provider);
        return Ok(new { success = true, existing.UpdatedAt });
    }

    /// <summary>
    /// Test SSO connection by validating metadata URL or certificate
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection()
    {
        var tenantId = GetTenantId();

        var config = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);

        if (config == null)
            return BadRequest(new { error = "No SSO configuration found. Please configure SSO first." });

        var issues = new List<string>();

        if (string.IsNullOrEmpty(config.EntityId))
            issues.Add("Entity ID is not configured.");
        if (string.IsNullOrEmpty(config.MetadataUrl) && string.IsNullOrEmpty(config.SignInUrl))
            issues.Add("Either Metadata URL or Sign-In URL must be configured.");
        if (config.Protocol == "SAML" && string.IsNullOrEmpty(config.Certificate))
            issues.Add("SAML certificate is not configured.");
        if (config.Protocol == "OIDC" && (string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret)))
            issues.Add("OIDC Client ID and Client Secret are required.");

        // Try to validate metadata URL if present
        if (!string.IsNullOrEmpty(config.MetadataUrl))
        {
            try
            {
                var ssrfResult = await Upkilo.API.Middleware.SsrfPreventionMiddleware.ValidateUrlAsync(config.MetadataUrl, _logger);
                if (!ssrfResult.IsValid)
                {
                    issues.Add($"Metadata URL is not allowed: {ssrfResult.Error}");
                }
                else
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var response = await httpClient.GetAsync(config.MetadataUrl);
                    if (!response.IsSuccessStatusCode)
                        issues.Add($"Metadata URL returned HTTP {(int)response.StatusCode}.");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to reach metadata URL: {ex.Message}");
            }
        }

        var isHealthy = issues.Count == 0;

        _logger.LogInformation("SSO test for tenant {TenantId}: {Status}", tenantId, isHealthy ? "Healthy" : "Issues found");

        return Ok(new
        {
            status = isHealthy ? "healthy" : "issues_found",
            provider = config.Provider,
            protocol = config.Protocol,
            issues,
            testedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Delete SSO configuration
    /// </summary>
    [HttpDelete("config")]
    public async Task<IActionResult> DeleteConfig()
    {
        var tenantId = GetTenantId();

        var ssoConfig = await _context.SsoConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);

        if (ssoConfig != null)
        {
            ssoConfig.IsDeleted = true;
            ssoConfig.DeletedAt = DateTime.UtcNow;
        }

        var samlConfig = await _context.SamlConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);

        if (samlConfig != null)
        {
            samlConfig.IsDeleted = true;
            samlConfig.DeletedAt = DateTime.UtcNow;
        }

        if (ssoConfig == null && samlConfig == null)
            return NotFound(new { error = "No SSO configuration found." });

        await _context.SaveChangesAsync();

        _logger.LogInformation("SSO config deleted for tenant {TenantId}", tenantId);
        return NoContent();
    }

    /// <summary>
    /// Get SP metadata for the current tenant (for IdP configuration)
    /// </summary>
    [HttpGet("metadata")]
    [AllowAnonymous]
    public IActionResult GetServiceProviderMetadata()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var entityId = $"{baseUrl}/api/v1/sso/metadata";
        var acsUrl = $"{baseUrl}/api/v1/sso/callback";
        var sloUrl = $"{baseUrl}/api/v1/sso/logout";

        var metadata = $@"<?xml version=""1.0""?>
<EntityDescriptor xmlns=""urn:oasis:names:tc:SAML:2.0:metadata""
    entityID=""{entityId}"">
  <SPSSODescriptor AuthnRequestsSigned=""false""
      WantAssertionsSigned=""true""
      protocolSupportEnumeration=""urn:oasis:names:tc:SAML:2.0:protocol"">
    <NameIDFormat>urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress</NameIDFormat>
    <AssertionConsumerService
        Binding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST""
        Location=""{acsUrl}""
        index=""1"" />
    <SingleLogoutService
        Binding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect""
        Location=""{sloUrl}"" />
  </SPSSODescriptor>
</EntityDescriptor>";

        return Content(metadata, "application/xml");
    }

    /// <summary>
    /// Get available SSO providers
    /// </summary>
    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var providers = new[]
        {
            new { id = "okta", name = "Okta", protocols = new[] { "SAML", "OIDC" }, icon = "okta" },
            new { id = "azure_ad", name = "Azure Active Directory", protocols = new[] { "SAML", "OIDC" }, icon = "microsoft" },
            new { id = "google", name = "Google Workspace", protocols = new[] { "SAML", "OIDC" }, icon = "google" },
            new { id = "onelogin", name = "OneLogin", protocols = new[] { "SAML" }, icon = "onelogin" },
            new { id = "ping", name = "Ping Identity", protocols = new[] { "SAML", "OIDC" }, icon = "ping" },
            new { id = "custom", name = "Custom SAML/OIDC", protocols = new[] { "SAML", "OIDC" }, icon = "settings" }
        };

        return Ok(new { data = providers });
    }

    /// <summary>
    /// Initiates an SP-initiated SAML SSO login sequence.
    /// </summary>
    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<IActionResult> InitiateSso([FromQuery] string tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant))
        {
            return BadRequest(new { error = "Tenant identifier is required." });
        }

        _logger.LogInformation("Initiating SP-initiated SSO login flow for tenant: {Tenant}", tenant);

        Tenant? tenantObj = null;
        if (Guid.TryParse(tenant, out var parsedTenantId))
        {
            tenantObj = await _context.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == parsedTenantId && t.Status == TenantStatus.Active);
        }
        else
        {
            tenantObj = await _context.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == tenant && t.Status == TenantStatus.Active);
        }

        if (tenantObj == null)
        {
            return BadRequest(new { error = $"Active tenant '{tenant}' not found." });
        }

        var samlConfig = await _context.SamlConfigurations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantObj.Id && !c.IsDeleted);

        if (samlConfig == null || !samlConfig.IsEnabled)
        {
            return BadRequest(new { error = "SSO is not enabled or configured for this tenant." });
        }

        if (string.IsNullOrEmpty(samlConfig.SignOnUrl))
        {
            return BadRequest(new { error = "SAML Sign-On URL is not configured." });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var issuer = $"{baseUrl}/api/v1/sso/metadata";
        var acsUrl = $"{baseUrl}/api/v1/sso/callback";
        var destination = samlConfig.SignOnUrl;

        string samlRequest;
        try
        {
            samlRequest = _ssoIntegrationService.CreateSamlRequest(issuer, acsUrl, destination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAML AuthnRequest for tenant {TenantId}.", tenantObj.Id);
            return StatusCode(500, new { error = "Failed to generate SAML authentication request." });
        }

        var redirectUrl = destination;
        var queryGlue = redirectUrl.Contains("?") ? "&" : "?";
        redirectUrl += $"{queryGlue}SAMLRequest={System.Net.WebUtility.UrlEncode(samlRequest)}&RelayState={System.Net.WebUtility.UrlEncode(tenantObj.Id.ToString())}";

        _logger.LogInformation("Redirecting user to IdP SignOnUrl: {SignOnUrl} for Tenant {TenantId}", destination, tenantObj.Id);
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// SAML Assertion Consumer Service (ACS) callback.
    /// Receives and validates the SAML response from the IdP.
    /// </summary>
    [HttpPost("callback")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> SsoCallback([FromForm] string SAMLResponse, [FromForm] string RelayState)
    {
        _logger.LogInformation("SAML ACS Callback received.");

        var frontendUrl = _configuration["App:FrontendUrl"] ?? _configuration["APP_URL"] ?? "http://localhost:3000";

        if (string.IsNullOrEmpty(SAMLResponse))
        {
            return Redirect($"{frontendUrl}/login?sso_error={System.Net.WebUtility.UrlEncode("SAML Response is missing.")}");
        }

        if (string.IsNullOrEmpty(RelayState) || !Guid.TryParse(RelayState, out var tenantId))
        {
            return Redirect($"{frontendUrl}/login?sso_error={System.Net.WebUtility.UrlEncode("Invalid or missing Relay State (Tenant ID).")}");
        }

        var samlConfig = await _context.SamlConfigurations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && !c.IsDeleted);

        if (samlConfig == null || !samlConfig.IsEnabled)
        {
            return Redirect($"{frontendUrl}/login?sso_error={System.Net.WebUtility.UrlEncode("SSO is not configured or active for this tenant.")}");
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var expectedAudience = $"{baseUrl}/api/v1/sso/metadata";

        SamlUserResult samlUserResult;
        try
        {
            samlUserResult = _ssoIntegrationService.ValidateSamlResponse(SAMLResponse, samlConfig, expectedAudience);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAML Assertion validation failed for tenant {TenantId}.", tenantId);
            return Redirect($"{frontendUrl}/login?sso_error={System.Net.WebUtility.UrlEncode($"SAML Validation Failed: {ex.Message}")}");
        }

        // F-07: One-time-use replay protection. Reject any assertion ID already consumed,
        // keyed by tenant. TTL is bounded by the assertion's NotOnOrAfter (+skew) so the
        // cache entry naturally outlives the window in which the assertion is replayable.
        if (!string.IsNullOrEmpty(samlUserResult.AssertionId))
        {
            var replayKey = $"saml_replay:{tenantId}:{samlUserResult.AssertionId}";
            if (await _cache.GetStringAsync(replayKey) != null)
            {
                _logger.LogWarning("SAML replay blocked for tenant {TenantId}, assertion {AssertionId}.",
                    tenantId, samlUserResult.AssertionId);
                return Redirect($"{frontendUrl}/login?sso_error={System.Net.WebUtility.UrlEncode("SAML assertion has already been used.")}");
            }

            var ttl = samlUserResult.NotOnOrAfter.HasValue
                ? (samlUserResult.NotOnOrAfter.Value - DateTime.UtcNow) + TimeSpan.FromMinutes(5)
                : TimeSpan.FromMinutes(10);
            if (ttl < TimeSpan.FromMinutes(1)) ttl = TimeSpan.FromMinutes(10);

            await _cache.SetStringAsync(replayKey, "1",
                new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var userAgent = Request.Headers["User-Agent"].ToString() ?? "Unknown";

        _logger.LogInformation("SAML Assertion validated successfully for {Email}. Initiating SSO session login...", samlUserResult.Email);

        var authResult = await _authService.SsoLoginAsync(
            samlUserResult.Email,
            samlUserResult.FirstName,
            samlUserResult.LastName,
            "SAML",
            tenantId,
            ipAddress,
            userAgent
        );

        if (!authResult.Success)
        {
            _logger.LogWarning("SSO login service rejected session for {Email}: {Message}", samlUserResult.Email, authResult.Message);
            return Redirect($"{frontendUrl}/login?sso_error={System.Net.WebUtility.UrlEncode(authResult.Message ?? "Failed to create user session.")}");
        }

        // VULN-A14 FIX: JWT tokens must never appear in the redirect URL — they are written to
        // browser history, Referer headers, and server access logs.
        // Exchange-code pattern: store tokens in Redis under a one-time code; the frontend
        // calls GET /sso/exchange/{code} to redeem them.
        var exchangeCode = Guid.NewGuid().ToString("N");
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            token = authResult.Token,
            refreshToken = authResult.RefreshToken
        });
        await _cache.SetStringAsync(
            $"sso_exchange:{exchangeCode}",
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            });

        _logger.LogInformation("SSO session established successfully for {Email}. Redirecting to frontend...", samlUserResult.Email);
        return Redirect($"{frontendUrl}/sso/callback?code={exchangeCode}");
    }

    /// <summary>
    /// VULN-A14: Redeem a one-time exchange code issued by the SAML ACS callback.
    /// The code is valid for 2 minutes and deleted after first use.
    /// </summary>
    [HttpGet("exchange/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> ExchangeSsoCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 32)
            return BadRequest(new { error = "invalid_code" });

        var cacheKey = $"sso_exchange:{code}";
        var payload = await _cache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(payload))
            return NotFound(new { error = "code_expired_or_invalid" });

        // Delete immediately — one-time use
        await _cache.RemoveAsync(cacheKey);

        var result = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(payload);
        return Ok(new
        {
            token        = result.GetProperty("token").GetString(),
            refreshToken = result.GetProperty("refreshToken").GetString()
        });
    }
}

// ─── Request DTOs ───

public class UpdateSsoConfigRequest
{
    public string Provider { get; set; } = string.Empty;
    public string? Protocol { get; set; }
    public string? EntityId { get; set; }
    public string? MetadataUrl { get; set; }
    public string? SignInUrl { get; set; }
    public string? Certificate { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AttributeMapping { get; set; }
    public bool? IsEnabled { get; set; }
    public bool? EnforceForAllUsers { get; set; }
    // SAML-specific fields
    public string? LogoutUrl { get; set; }
    public bool? AllowPasswordLogin { get; set; }
    public bool? AutoCreateUsers { get; set; }
    public string? DefaultRoleId { get; set; }
}
