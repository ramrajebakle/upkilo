using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.API.Attributes;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[FeatureGuard("marketing_automation")]
public class MarketingAutomationController : ControllerBase
{
    private readonly IMarketingAutomationService _marketingService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MarketingAutomationController> _logger;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public MarketingAutomationController(
        IMarketingAutomationService marketingService,
        ITenantProvider tenantProvider,
        ILogger<MarketingAutomationController> logger,
        AppDbContext context,
        IConfiguration configuration)
    {
        _marketingService = marketingService;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _context = context;
        _configuration = configuration;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Get the consolidated marketing automation dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var dashboard = await _marketingService.GetDashboardAsync(GetTenantId());
            var config = await _context.MarketingConfigs.FirstOrDefaultAsync(c => c.TenantId == GetTenantId());

            return Ok(new
            {
                dashboard,
                config = config == null ? null : new
                {
                    config.IsOnboarded,
                    config.IsAutonomousMode,
                    config.PrimaryGoal,
                    config.BusinessUrl,
                    config.IndustryNiche
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch marketing automation dashboard");
            return StatusCode(500, new { error = "Failed to fetch dashboard" });
        }
    }

    /// <summary>
    /// Get 30-day data-driven forecasts
    /// </summary>
    [HttpGet("forecasts")]
    public async Task<IActionResult> GetForecasts([FromQuery] int horizonDays = 30)
    {
        var forecasts = await _marketingService.GetForecastsAsync(GetTenantId(), horizonDays);
        return Ok(forecasts);
    }

    /// <summary>
    /// Get recent autonomous agent actions
    /// </summary>
    [HttpGet("actions")]
    public async Task<IActionResult> GetActions([FromQuery] int count = 20)
    {
        var actions = await _marketingService.GetRecentActionsAsync(GetTenantId(), count);
        return Ok(actions);
    }

    /// <summary>
    /// Update autonomous mode configuration
    /// </summary>
    [HttpPost("toggle-autonomous")]
    public async Task<IActionResult> ToggleAutonomous([FromBody] ToggleRequest request)
    {
        var config = await _context.MarketingConfigs.FirstOrDefaultAsync(c => c.TenantId == GetTenantId());
        if (config == null) return NotFound(new { error = "Marketing configuration not found" });

        config.IsAutonomousMode = request.IsEnabled;
        await _context.SaveChangesAsync();

        return Ok(new { config.IsAutonomousMode });
    }

    /// <summary>
    /// Onboard a tenant to the marketing engine
    /// </summary>
    [HttpPost("onboard")]
    public async Task<IActionResult> Onboard([FromBody] OnboardRequest request)
    {
        var config = await _marketingService.OnboardAsync(
            GetTenantId(),
            request.BusinessUrl,
            request.PrimaryGoal,
            request.TargetRegions
        );
        return Ok(config);
    }

    /// <summary>
    /// Get the status of external marketing integrations
    /// </summary>
    [HttpGet("integrations")]
    public async Task<IActionResult> GetIntegrationsStatus()
    {
        var tenantId = GetTenantId();
        var platforms = new[] { "Google", "Bing", "LinkedIn", "Twitter" };
        var statuses = new List<object>();

        foreach (var p in platforms)
        {
            var isConnected = await _context.AdAccounts.AnyAsync(a => a.TenantId == tenantId && a.Platform == p && a.IsConnected);
            statuses.Add(new { Platform = p, IsConnected = isConnected });
        }

        return Ok(statuses);
    }

    /// <summary>
    /// Initiates OAuth2 authorization flow for an ad platform integration.
    /// Returns the authorization URL the client must redirect to.
    /// Token exchange is completed via GET /integrations/callback once the user approves.
    /// </summary>
    [HttpPost("integrations/connect")]
    public IActionResult ConnectAccount([FromBody] ConnectRequest request)
    {
        var tenantId = GetTenantId();

        var authUrl = request.Platform.ToLowerInvariant() switch
        {
            "google" => BuildGoogleOAuthUrl(tenantId, request.Platform),
            "facebook" or "meta" => BuildMetaOAuthUrl(tenantId, request.Platform),
            _ => null
        };

        if (authUrl == null)
            return BadRequest(new { error = $"Platform '{request.Platform}' does not support OAuth integration." });

        return Ok(new { authorizationUrl = authUrl });
    }

    private string BuildGoogleOAuthUrl(Guid tenantId, string platform)
    {
        var clientId = _configuration["Google:AdsClientId"]
            ?? throw new InvalidOperationException("Google:AdsClientId is not configured.");
        var redirectUri = _configuration["Google:AdsRedirectUri"]
            ?? throw new InvalidOperationException("Google:AdsRedirectUri is not configured.");

        var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{tenantId}:{platform}"));
        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={Uri.EscapeDataString(clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/adwords")}" +
               $"&access_type=offline&prompt=consent" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    private string BuildMetaOAuthUrl(Guid tenantId, string platform)
    {
        var appId = _configuration["Meta:AppId"]
            ?? throw new InvalidOperationException("Meta:AppId is not configured.");
        var redirectUri = _configuration["Meta:AdsRedirectUri"]
            ?? throw new InvalidOperationException("Meta:AdsRedirectUri is not configured.");

        var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{tenantId}:{platform}"));
        return $"https://www.facebook.com/v19.0/dialog/oauth" +
               $"?client_id={Uri.EscapeDataString(appId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&scope={Uri.EscapeDataString("ads_management,ads_read")}" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    public record ToggleRequest(bool IsEnabled);
    public record OnboardRequest(string BusinessUrl, string PrimaryGoal, string? TargetRegions);
    public record ConnectRequest(string Platform);
}
