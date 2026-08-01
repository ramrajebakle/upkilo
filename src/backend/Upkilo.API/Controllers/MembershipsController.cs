using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Text.Json;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

/// <summary>
/// Memberships controller for subscription and membership plan management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class MembershipsController : ControllerBase
{
    private readonly IMembershipService _membershipService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<MembershipsController> _logger;

    public MembershipsController(
        IMembershipService membershipService,
        ITenantProvider tenantProvider,
        IPaymentService paymentService,
        ILogger<MembershipsController> logger)
    {
        _membershipService = membershipService;
        _tenantProvider = tenantProvider;
        _paymentService = paymentService;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() ?? Guid.Empty;

    /// <summary>
    /// Get all membership plans
    /// </summary>
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var tenantId = GetTenantId();
        var plans = await _membershipService.GetPlansAsync(tenantId);

        var result = plans.Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.BillingInterval,
            p.ServicesIncluded,
            p.DiscountPercent,
            p.IsActive,
            features = string.IsNullOrEmpty(p.FeaturesJson) ? Array.Empty<string>() : System.Text.Json.JsonSerializer.Deserialize<string[]>(p.FeaturesJson)
        });

        return Ok(new { data = result });
    }

    /// <summary>
    /// Get membership plan by ID
    /// </summary>
    [HttpGet("plans/{id}")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        var tenantId = GetTenantId();
        var plan = await _membershipService.GetPlanAsync(id, tenantId);

        if (plan == null) return NotFound();

        return Ok(new
        {
            plan.Id,
            plan.Name,
            plan.Description,
            plan.Price,
            plan.BillingInterval,
            plan.ServicesIncluded,
            plan.DiscountPercent,
            plan.IsActive,
            features = string.IsNullOrEmpty(plan.FeaturesJson) ? Array.Empty<string>() : System.Text.Json.JsonSerializer.Deserialize<string[]>(plan.FeaturesJson),
            plan.CreatedAt
        });
    }

    /// <summary>
    /// Create a membership plan
    /// </summary>
    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateMembershipPlanRequest request)
    {
        var tenantId = GetTenantId();

        var plan = new MembershipPlan
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            BillingInterval = request.BillingInterval,
            StripePriceId = request.StripePriceId,
            ServicesIncluded = request.ServicesIncluded,
            DiscountPercent = request.DiscountPercent,
            FeaturesJson = request.Features != null ? System.Text.Json.JsonSerializer.Serialize(request.Features) : null,
            IsActive = true
        };

        var createdPlan = await _membershipService.CreatePlanAsync(tenantId, plan);

        _logger.LogInformation("Membership plan created: {PlanId} - {Name}", createdPlan.Id, createdPlan.Name);

        return CreatedAtAction(nameof(GetPlan), new { id = createdPlan.Id }, createdPlan);
    }

    /// <summary>
    /// Update a membership plan
    /// </summary>
    [HttpPut("plans/{id}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateMembershipPlanRequest request)
    {
        var tenantId = GetTenantId();

        var plan = new MembershipPlan();
        if (request.Name != null) plan.Name = request.Name;
        if (request.Description != null) plan.Description = request.Description;
        if (request.Price.HasValue) plan.Price = request.Price.Value;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;
        if (request.Features != null) plan.FeaturesJson = System.Text.Json.JsonSerializer.Serialize(request.Features);

        var success = await _membershipService.UpdatePlanAsync(id, tenantId, plan);
        if (!success) return NotFound();

        _logger.LogInformation("Membership plan updated: {PlanId}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Delete a membership plan
    /// </summary>
    [HttpDelete("plans/{id}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        var tenantId = GetTenantId();
        var success = await _membershipService.DeletePlanAsync(id, tenantId);

        if (!success) return BadRequest(new { message = "Plan not found or has active subscriptions." });

        _logger.LogInformation("Membership plan deleted: {PlanId}", id);
        return NoContent();
    }

    /// <summary>
    /// Get all active subscriptions
    /// </summary>
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var tenantId = GetTenantId();
        var subscriptions = await _membershipService.GetSubscriptionsAsync(tenantId, status);

        var result = subscriptions.Select(s => new
        {
            s.Id,
            s.ClientId,
            clientName = s.Client.FirstName + " " + s.Client.LastName,
            s.MembershipPlanId,
            planName = s.MembershipPlan.Name,
            status = s.Status.ToString().ToLower(),
            s.MembershipPlan.Price,
            s.MembershipPlan.BillingInterval,
            startDate = s.StartDate.ToString("yyyy-MM-dd"),
            nextBillingDate = s.NextBillingDate.ToString("yyyy-MM-dd"),
            servicesUsed = s.ServicesUsedThisPeriod,
            servicesRemaining = s.MembershipPlan.ServicesIncluded == -1 ? -1 : Math.Max(0, s.MembershipPlan.ServicesIncluded - s.ServicesUsedThisPeriod)
        });

        // Simplified pagination for now
        return Ok(new { data = result.Skip((page - 1) * pageSize).Take(pageSize), total = result.Count(), page, pageSize });
    }

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    [HttpGet("subscriptions/{id}")]
    public async Task<IActionResult> GetSubscription(Guid id)
    {
        var tenantId = GetTenantId();
        var sub = await _membershipService.GetSubscriptionAsync(id, tenantId);

        if (sub == null) return NotFound();

        return Ok(new
        {
            sub.Id,
            sub.ClientId,
            clientName = sub.Client.FirstName + " " + sub.Client.LastName,
            clientEmail = sub.Client.Email,
            sub.MembershipPlanId,
            planName = sub.MembershipPlan.Name,
            status = sub.Status.ToString().ToLower(),
            sub.MembershipPlan.Price,
            sub.MembershipPlan.BillingInterval,
            startDate = sub.StartDate.ToString("yyyy-MM-dd"),
            nextBillingDate = sub.NextBillingDate.ToString("yyyy-MM-dd"),
            servicesUsed = sub.ServicesUsedThisPeriod,
            servicesRemaining = sub.MembershipPlan.ServicesIncluded == -1 ? -1 : Math.Max(0, sub.MembershipPlan.ServicesIncluded - sub.ServicesUsedThisPeriod),
            sub.CreatedAt
        });
    }

    /// <summary>
    /// Create a subscription for a client
    /// </summary>
    [HttpPost("subscriptions")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var tenantId = GetTenantId();

        try
        {
            var sub = await _membershipService.SubscribeClientAsync(tenantId, request.ClientId, request.PlanId);
            _logger.LogInformation("Subscription created: {SubscriptionId} for client {ClientId}", sub.Id, request.ClientId);

            string? checkoutUrl = null;
            if (sub.MembershipPlan != null && !string.IsNullOrEmpty(sub.MembershipPlan.StripePriceId))
            {
                var checkoutReq = new CreateCheckoutRequest(
                    tenantId,
                    sub.MembershipPlan.StripePriceId,
                    request.SuccessUrl ?? $"{Request.Scheme}://{Request.Host}/dashboard?success=true",
                    request.CancelUrl ?? $"{Request.Scheme}://{Request.Host}/dashboard?cancel=true",
                    sub.MembershipPlan.BillingInterval == "yearly",
                    null,
                    null,
                    null
                );

                var checkoutResult = await _paymentService.CreateCheckoutSessionAsync(checkoutReq);
                if (checkoutResult.Success)
                {
                    checkoutUrl = checkoutResult.SessionUrl;
                }
            }

            return CreatedAtAction(nameof(GetSubscription), new { id = sub.Id }, new
            {
                subscription = sub,
                checkoutUrl
            });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    [HttpPost("subscriptions/{id}/cancel")]
    public async Task<IActionResult> CancelSubscription(Guid id, [FromBody] MembershipCancelRequest request)
    {
        var tenantId = GetTenantId();
        var success = await _membershipService.CancelSubscriptionAsync(id, tenantId, request.Immediately);

        if (!success) return NotFound();

        _logger.LogInformation("Subscription cancelled: {SubscriptionId}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Pause a subscription
    /// </summary>
    [HttpPost("subscriptions/{id}/pause")]
    public async Task<IActionResult> PauseSubscription(Guid id, [FromBody] PauseSubscriptionRequest request)
    {
        var tenantId = GetTenantId();
        var success = await _membershipService.PauseSubscriptionAsync(id, tenantId, request.ResumeDate);

        if (!success) return NotFound();

        _logger.LogInformation("Subscription paused: {SubscriptionId}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Resume a subscription
    /// </summary>
    [HttpPost("subscriptions/{id}/resume")]
    public async Task<IActionResult> ResumeSubscription(Guid id)
    {
        var tenantId = GetTenantId();
        var success = await _membershipService.ResumeSubscriptionAsync(id, tenantId);

        if (!success) return NotFound();

        _logger.LogInformation("Subscription resumed: {SubscriptionId}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Record service usage for a subscription
    /// </summary>
    [HttpPost("subscriptions/{id}/use-service")]
    public async Task<IActionResult> UseService(Guid id, [FromBody] UseServiceRequest request)
    {
        var tenantId = GetTenantId();
        var success = await _membershipService.RecordUsageAsync(id, tenantId, request.ServiceId);

        if (!success)
            return BadRequest(new { message = "Failed to record usage. Subscription may be inactive or limit reached." });

        _logger.LogInformation("Service used for subscription: {SubscriptionId}, Service: {ServiceId}", id, request.ServiceId);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Get membership analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var tenantId = GetTenantId();
        var analytics = await _membershipService.GetAnalyticsAsync(tenantId);
        return Ok(analytics);
    }
}

// Request DTOs
public class CreateMembershipPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string BillingInterval { get; set; } = "monthly";
    public List<string>? Features { get; set; }
    public int ServicesIncluded { get; set; }
    public int DiscountPercent { get; set; }
    public string? StripePriceId { get; set; }
}

public class UpdateMembershipPlanRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public List<string>? Features { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateSubscriptionRequest
{
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class MembershipCancelRequest
{
    public bool Immediately { get; set; }
}

public class PauseSubscriptionRequest
{
    public DateTime? ResumeDate { get; set; }
}

public class UseServiceRequest
{
    public Guid ServiceId { get; set; }
}

