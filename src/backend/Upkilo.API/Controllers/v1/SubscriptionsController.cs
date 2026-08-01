
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers.v1;

/// <summary>
/// Subscription management API
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ITenantProvider tenantProvider,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _subscriptionService.GetAllPricingPlansAsync();
        return Ok(plans.Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.TrialDays,
            p.IsCustom,
            monthlyPrice = p.Prices.FirstOrDefault(x => x.Cycle == Upkilo.Core.Entities.BillingCycle.Monthly)?.Amount,
            annualPrice = p.Prices.FirstOrDefault(x => x.Cycle == Upkilo.Core.Entities.BillingCycle.Annual)?.Amount,
            features = p.FeatureMappings.Select(m => new
            {
                key = m.PricingFeature?.Key,
                name = m.PricingFeature?.Name,
                enabled = m.IsEnabled,
                limit = m.NumericLimit
            })
        }));
    }

    /// <summary>
    /// Get current tenant's subscription
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<TenantSubscriptionDto>> GetCurrentSubscription()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var subscription = await _subscriptionService.GetSubscriptionAsync(tenantId.Value);
        if (subscription == null)
            return NotFound("No subscription found");

        return Ok(new TenantSubscriptionDto
        {
            Id = subscription.Id,
            PlanId = subscription.PricingPlanId,
            PlanName = subscription.PricingPlan?.Name,
            Status = subscription.Status.ToString(),
            BillingInterval = subscription.BillingInterval.ToString(),
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CancelledAt = subscription.CancelledAt,
            ExtraStaffCount = subscription.ExtraStaffCount,
            ExtraLocationCount = subscription.ExtraLocationCount
        });
    }

    /// <summary>
    /// Get current usage summary
    /// </summary>
    [HttpGet("usage")]
    public async Task<ActionResult<UsageSummary>> GetUsage()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var usage = await _subscriptionService.GetUsageAsync(tenantId.Value);
        return Ok(usage);
    }

    /// <summary>
    /// Create a new subscription
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SubscriptionResult>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var result = await _subscriptionService.CreateSubscriptionAsync(
            tenantId.Value, request.PlanId, request.BillingInterval, request.PromoCode);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Change subscription plan
    /// </summary>
    [HttpPut("change")]
    public async Task<ActionResult<SubscriptionResult>> ChangeSubscription([FromBody] ChangeSubscriptionRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var result = await _subscriptionService.ChangeSubscriptionAsync(
            tenantId.Value, request.NewPlanId, request.NewBillingInterval);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    [HttpPost("cancel")]
    public async Task<ActionResult<SubscriptionResult>> CancelSubscription([FromBody] CancelSubscriptionRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var result = await _subscriptionService.CancelSubscriptionAsync(tenantId.Value, request.Immediate);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Pause subscription
    /// </summary>
    [HttpPost("pause")]
    public async Task<ActionResult<SubscriptionResult>> PauseSubscription([FromBody] PauseSubscriptionRequest? request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var result = await _subscriptionService.PauseSubscriptionAsync(tenantId.Value, request?.ResumeAt);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Resume paused subscription
    /// </summary>
    [HttpPost("resume")]
    public async Task<ActionResult<SubscriptionResult>> ResumeSubscription()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var result = await _subscriptionService.ResumeSubscriptionAsync(tenantId.Value);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Validate a promotion code
    /// </summary>
    [HttpGet("promo/{code}")]
    public async Task<ActionResult<PromotionCodeDto>> ValidatePromoCode(string code)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var promo = await _subscriptionService.ValidatePromoCodeAsync(code, tenantId.Value);
        if (promo == null)
            return NotFound("Invalid or expired promotion code");

        return Ok(new PromotionCodeDto
        {
            Code = promo.Code,
            Description = promo.Description,
            DiscountType = promo.DiscountType.ToString(),
            DiscountValue = promo.DiscountValue,
            ExpiresAt = promo.ExpiresAt
        });
    }

    /// <summary>
    /// Add extra staff seats
    /// </summary>
    [HttpPost("expansion/staff")]
    public async Task<ActionResult<SubscriptionResult>> AddExtraStaff([FromBody] AddExtraRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var result = await _subscriptionService.AddExtraStaffAsync(tenantId.Value, request.Count);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Add extra locations
    /// </summary>
    [HttpPost("expansion/locations")]
    public async Task<ActionResult<SubscriptionResult>> AddExtraLocation([FromBody] AddExtraRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var result = await _subscriptionService.AddExtraLocationAsync(tenantId.Value, request.Count);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Check if a specific feature is available
    /// </summary>
    [HttpGet("features/{featureName}")]
    public async Task<ActionResult<FeatureAccessResult>> CheckFeatureAccess(string featureName)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var hasAccess = await _subscriptionService.CheckFeatureAccessAsync(tenantId.Value, featureName);

        return Ok(new FeatureAccessResult
        {
            FeatureName = featureName,
            HasAccess = hasAccess
        });
    }

    /// <summary>
    /// Calculate proration for plan change
    /// </summary>
    [HttpGet("proration/{newPlanId}")]
    public async Task<ActionResult<ProrationResult>> GetProration(Guid newPlanId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var amount = await _subscriptionService.CalculateProratedAmountAsync(tenantId.Value, newPlanId);

        return Ok(new ProrationResult
        {
            NewPlanId = newPlanId,
            ProratedAmount = amount
        });
    }

    /// <summary>
    /// Update AI monthly budget (Soft Limit)
    /// </summary>
    [HttpPut("ai-budget")]
    public async Task<ActionResult<SubscriptionResult>> UpdateAiBudget([FromBody] UpdateAiBudgetRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var result = await _subscriptionService.UpdateAiBudgetAsync(tenantId.Value, request.Budget);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}

// DTOs
public record SubscriptionPlanDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public decimal MonthlyPrice { get; init; }
    public decimal AnnualPrice { get; init; }
    public int TrialDays { get; init; }
    public PlanFeatures? Features { get; init; }
}

public record TenantSubscriptionDto
{
    public Guid Id { get; init; }
    public Guid? PlanId { get; init; }
    public string? PlanName { get; init; }
    public string Status { get; init; } = string.Empty;
    public string BillingInterval { get; init; } = string.Empty;
    public DateTime CurrentPeriodStart { get; init; }
    public DateTime CurrentPeriodEnd { get; init; }
    public DateTime? CancelledAt { get; init; }
    public int ExtraStaffCount { get; init; }
    public int ExtraLocationCount { get; init; }
}

public record PromotionCodeDto
{
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record CreateSubscriptionRequest
{
    public Guid PlanId { get; init; }
    public BillingInterval BillingInterval { get; init; }
    public string? PromoCode { get; init; }
}

public record ChangeSubscriptionRequest
{
    public Guid NewPlanId { get; init; }
    public BillingInterval? NewBillingInterval { get; init; }
}

public record CancelSubscriptionRequest
{
    public bool Immediate { get; init; }
}

public record PauseSubscriptionRequest
{
    public DateTime? ResumeAt { get; init; }
}

public record AddExtraRequest
{
    public int Count { get; init; } = 1;
}

public record FeatureAccessResult
{
    public string FeatureName { get; init; } = string.Empty;
    public bool HasAccess { get; init; }
}

public record ProrationResult
{
    public Guid NewPlanId { get; init; }
    public decimal ProratedAmount { get; init; }
}

public record UpdateAiBudgetRequest
{
    public decimal Budget { get; init; }
}
