using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Enterprise sales infrastructure — lead capture, custom plan creation, compliance page data.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/enterprise")]
public class EnterpriseController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<EnterpriseController> _logger;
    private readonly IConfiguration _configuration;

    public EnterpriseController(AppDbContext context, IEmailService emailService, ILogger<EnterpriseController> logger, IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// POST /api/v1/enterprise/contact — enterprise lead form submission.
    /// Notifies founder + adds lead to pipeline.
    /// </summary>
    [HttpPost("contact")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitLead([FromBody] EnterpriseLeadRequest lead)
    {
        if (string.IsNullOrWhiteSpace(lead.Email) || string.IsNullOrWhiteSpace(lead.CompanyName))
            return BadRequest(ApiResponse.Fail("Company name and email are required"));

        // Persist lead
        _context.EnterpriseLeads.Add(new Upkilo.Core.Entities.EnterpriseLead
        {
            Id = Guid.NewGuid(),
            CompanyName = lead.CompanyName,
            ContactName = lead.ContactName,
            Email = lead.Email,
            Phone = lead.Phone,
            TeamSize = lead.TeamSize,
            CurrentPlatform = lead.CurrentPlatform,
            UseCase = lead.UseCase,
            Message = lead.Message,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Notify founder — email is configurable via Enterprise:NotificationEmail in config
        var notificationEmail = _configuration["Enterprise:NotificationEmail"] ?? "sales@upkilo.com";
        await _emailService.SendSystemEmailAsync(
            notificationEmail,
            $"🎯 New Enterprise Lead: {lead.CompanyName}",
            $"""
            <h2>Enterprise Lead Received</h2>
            <table style="border-collapse:collapse;width:100%">
                <tr><td style="padding:8px;font-weight:bold;">Company</td><td style="padding:8px;">{lead.CompanyName}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;">Contact</td><td style="padding:8px;">{lead.ContactName}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;">Email</td><td style="padding:8px;">{lead.Email}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;">Phone</td><td style="padding:8px;">{lead.Phone ?? "N/A"}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;">Team Size</td><td style="padding:8px;">{lead.TeamSize}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;">Current Platform</td><td style="padding:8px;">{lead.CurrentPlatform ?? "N/A"}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;">Use Case</td><td style="padding:8px;">{lead.UseCase ?? "N/A"}</td></tr>
                <tr><td style="padding:8px;font-weight:bold;">Message</td><td style="padding:8px;">{lead.Message ?? "N/A"}</td></tr>
            </table>
            <p><strong>Respond within 24 hours for maximum conversion.</strong></p>
            """
        );

        // Send auto-response to prospect
        await _emailService.SendSystemEmailAsync(
            lead.Email,
            $"We received your Upkilo Enterprise inquiry — {lead.CompanyName}",
            $"""
            <h2>Thank you for your interest in Upkilo Enterprise!</h2>
            <p>Hi {lead.ContactName ?? "there"},</p>
            <p>We've received your inquiry for {lead.CompanyName} and our team will be in touch within <strong>24 hours</strong> to discuss your requirements.</p>
            <p>In the meantime, here's what Enterprise customers get:</p>
            <ul>
                <li>✅ Unlimited staff, locations, and clients</li>
                <li>✅ Custom AI budget and model routing</li>
                <li>✅ SSO / SAML integration</li>
                <li>✅ 99.9% SLA with dedicated support</li>
                <li>✅ SOC2 Type II compliance</li>
                <li>✅ White-label booking pages</li>
                <li>✅ Custom contract and invoicing</li>
            </ul>
            <p>The Upkilo Team</p>
            """
        );

        _logger.LogInformation("[Enterprise] Lead from {Company} ({Email}) submitted", lead.CompanyName, lead.Email);

        return Ok(ApiResponse<object>.Ok(new
        {
            message = "We'll be in touch within 24 hours to discuss your requirements.",
            referenceId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()
        }));
    }

    /// <summary>
    /// GET /api/v1/enterprise/features — returns the enterprise feature checklist for the marketing page.
    /// </summary>
    [HttpGet("features")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400)]
    public IActionResult GetFeatures()
    {
        return Ok(new
        {
            categories = new[]
            {
                new
                {
                    label = "Scale",
                    features = new[] { "Unlimited staff members", "Unlimited locations", "Unlimited clients", "100,000 AI actions/month" }
                },
                new
                {
                    label = "Security",
                    features = new[] { "SSO / SAML 2.0", "SCIM user provisioning", "Extended audit logs (90 days)", "IP allowlisting", "SOC2 Type II (in progress)" }
                },
                new
                {
                    label = "Support",
                    features = new[] { "Dedicated Customer Success Manager", "Priority support (< 2hr SLA)", "Onboarding & migration assistance", "Custom training sessions" }
                },
                new
                {
                    label = "Commercial",
                    features = new[] { "Custom contract & invoicing", "Annual billing with PO support", "Volume discounts", "Custom AI budget allocation" }
                },
                new
                {
                    label = "Integrations",
                    features = new[] { "Custom API rate limits", "Webhooks with retry guarantees", "Zapier enterprise tier", "Custom integrations on request" }
                }
            }
        });
    }

    /// <summary>
    /// GET /api/v1/enterprise/leads — admin only; list all enterprise leads.
    /// </summary>
    [HttpGet("leads")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetLeads([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var leads = await _context.EnterpriseLeads
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var total = await _context.EnterpriseLeads.CountAsync();

        return Ok(new { data = leads, total, page, pageSize });
    }

    /// <summary>
    /// POST /api/v1/enterprise/custom-plan — SuperAdmin creates a custom enterprise plan for a specific tenant.
    /// </summary>
    [HttpPost("custom-plan")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateCustomPlan([FromBody] CustomPlanRequest request)
    {
        var tenant = await _context.Tenants.FindAsync(request.TenantId);
        if (tenant == null) return NotFound(ApiResponse.Fail("Tenant not found"));

        var plan = new PricingPlan
        {
            Id = Guid.NewGuid(),
            Name = request.PlanName,
            Description = request.Description ?? $"Custom enterprise plan for {tenant.Name}",
            IsActive = true,
            IsCustom = true,
            TrialDays = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add a single price row for billing
        if (request.MonthlyPrice > 0)
        {
            plan.Prices = new List<PlanPrice>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PricingPlan = plan,
                    Amount = request.MonthlyPrice,
                    CurrencyCode = request.Currency ?? "USD",
                    Cycle = BillingCycle.Monthly
                }
            };
        }

        // Feature mappings based on request limits
        var allFeatures = await _context.PricingFeatures.ToListAsync();
        plan.FeatureMappings = allFeatures.Select(f => new PlanFeatureMapping
        {
            Id = Guid.NewGuid(),
            PricingPlan = plan,
            PricingFeature = f,
            IsEnabled = true,
            NumericLimit = f.Key switch
            {
                "max_staff" => request.MaxStaff,
                "max_locations" => request.MaxLocations,
                "max_clients" => request.MaxClients,
                "ai_actions" => request.AiActionsPerMonth,
                _ => null
            }
        }).ToList();

        _context.PricingPlans.Add(plan);

        // Assign plan to tenant
        tenant.PricingPlanId = plan.Id;
        await _context.SaveChangesAsync();

        _logger.LogInformation("[Enterprise] Custom plan {PlanId} created for tenant {TenantId}", plan.Id, tenant.Id);

        return Ok(ApiResponse<object>.Ok(new { planId = plan.Id, tenantId = tenant.Id, planName = plan.Name }));
    }
}

public class EnterpriseLeadRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? TeamSize { get; set; }
    public string? CurrentPlatform { get; set; }
    public string? UseCase { get; set; }
    public string? Message { get; set; }
}

public class CustomPlanRequest
{
    public Guid TenantId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public string? Currency { get; set; }
    public int? MaxStaff { get; set; }
    public int? MaxLocations { get; set; }
    public int? MaxClients { get; set; }
    public int? AiActionsPerMonth { get; set; }
}
