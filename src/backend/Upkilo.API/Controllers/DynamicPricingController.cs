using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Dynamic pricing / yield management — surge, off-peak, seasonal rules.
/// Rules are stored in Redis (keyed per tenant) so they survive restarts and
/// are consistent across multiple API instances.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class DynamicPricingController : ControllerBase
{
    private readonly ILogger<DynamicPricingController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDistributedCache _cache;

    private static readonly TimeSpan RuleTtl = TimeSpan.FromDays(365);

    public DynamicPricingController(ILogger<DynamicPricingController> logger, AppDbContext context, ITenantProvider tenantProvider, IDistributedCache cache)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _cache = cache;
    }

    private string RulesKey(Guid tenantId) => $"pricing:rules:{tenantId}";

    private async Task<Dictionary<string, PricingRule>> LoadRulesAsync(Guid tenantId)
    {
        var json = await _cache.GetStringAsync(RulesKey(tenantId));
        return json == null
            ? new Dictionary<string, PricingRule>()
            : JsonSerializer.Deserialize<Dictionary<string, PricingRule>>(json) ?? new();
    }

    private Task SaveRulesAsync(Guid tenantId, Dictionary<string, PricingRule> rules)
        => _cache.SetStringAsync(RulesKey(tenantId), JsonSerializer.Serialize(rules),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = RuleTtl });

    /// <summary>GET /api/v1/dynamicpricing/rules — list all pricing rules</summary>
    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var dict = await LoadRulesAsync(tenantId.Value);
        var rules = dict.Values.ToList();
        return Ok(ApiResponse<object>.Ok(new { rules, total = rules.Count }));
    }

    /// <summary>POST /api/v1/dynamicpricing/rules — create pricing rule</summary>
    [HttpPost("rules")]
    public async Task<IActionResult> CreateRule([FromBody] CreatePricingRuleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(ApiResponse.Fail("Name required"));

        var rule = new PricingRule
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = request.Name,
            Type = request.Type,
            AdjustmentType = request.AdjustmentType,
            AdjustmentValue = request.AdjustmentValue,
            IsActive = true,
            ApplicableDays = request.ApplicableDays ?? new List<string>(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ServiceIds = request.ServiceIds ?? new List<string>(),
            MinBookingsThreshold = request.MinBookingsThreshold,
            CreatedAt = DateTime.UtcNow.ToString("o"),
        };

        var dict = await LoadRulesAsync(tenantId.Value);
        dict[rule.Id] = rule;
        await SaveRulesAsync(tenantId.Value, dict);

        _logger.LogInformation("Pricing rule {Name} created for tenant {TenantId}", rule.Name, tenantId);
        return Ok(ApiResponse<object>.Ok(new { id = rule.Id, name = rule.Name }));
    }

    /// <summary>PUT /api/v1/dynamicpricing/rules/{id}/toggle — toggle rule active state</summary>
    [HttpPut("rules/{id}/toggle")]
    public async Task<IActionResult> ToggleRule(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var dict = await LoadRulesAsync(tenantId.Value);
        if (!dict.TryGetValue(id, out var rule)) return NotFound();

        rule.IsActive = !rule.IsActive;
        await SaveRulesAsync(tenantId.Value, dict);
        return Ok(ApiResponse<object>.Ok(new { id, isActive = rule.IsActive }));
    }

    /// <summary>DELETE /api/v1/dynamicpricing/rules/{id} — delete rule</summary>
    [HttpDelete("rules/{id}")]
    public async Task<IActionResult> DeleteRule(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var dict = await LoadRulesAsync(tenantId.Value);
        if (!dict.Remove(id)) return NotFound();
        await SaveRulesAsync(tenantId.Value, dict);

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    /// <summary>GET /api/v1/dynamicpricing/calculate — compute effective price for a booking scenario</summary>
    [HttpGet("calculate")]
    public async Task<IActionResult> CalculatePrice(
        [FromQuery] Guid serviceId,
        [FromQuery] DateTime? bookingTime,
        [FromQuery] int? currentBookingsCount)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.Services
            .Where(s => s.Id == serviceId && s.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (service == null) return NotFound(ApiResponse.Fail("Service not found"));

        decimal basePrice = service.Price;
        decimal effectivePrice = basePrice;
        var appliedRules = new List<object>();

        var rulesDict = await LoadRulesAsync(tenantId.Value);
        var rules = rulesDict.Values.Where(r => r.IsActive).ToList();

        var target = bookingTime ?? DateTime.UtcNow;

        foreach (var rule in rules)
        {
            bool applies = true;

            // Day-of-week filter
            if (rule.ApplicableDays?.Any() == true)
            {
                var dayName = target.DayOfWeek.ToString();
                if (!rule.ApplicableDays.Contains(dayName)) applies = false;
            }

            // Time window filter
            if (applies && !string.IsNullOrEmpty(rule.StartTime) && !string.IsNullOrEmpty(rule.EndTime))
            {
                var currentTime = target.TimeOfDay;
                if (TimeSpan.TryParse(rule.StartTime, out var start) && TimeSpan.TryParse(rule.EndTime, out var end))
                {
                    if (currentTime < start || currentTime > end) applies = false;
                }
            }

            // Date range filter
            if (applies && rule.StartDate.HasValue && target < rule.StartDate.Value) applies = false;
            if (applies && rule.EndDate.HasValue && target > rule.EndDate.Value) applies = false;

            // Demand-based: only apply if bookings exceed threshold
            if (applies && rule.Type == "demand" && rule.MinBookingsThreshold.HasValue)
            {
                if ((currentBookingsCount ?? 0) < rule.MinBookingsThreshold.Value) applies = false;
            }

            if (applies)
            {
                decimal adjustment = 0;
                if (rule.AdjustmentType == "percentage")
                    adjustment = effectivePrice * (rule.AdjustmentValue / 100);
                else
                    adjustment = rule.AdjustmentValue;

                effectivePrice += adjustment;
                appliedRules.Add(new { rule.Name, rule.Type, adjustment, rule.AdjustmentType, rule.AdjustmentValue });
            }
        }

        effectivePrice = Math.Max(0, effectivePrice);

        return Ok(ApiResponse<object>.Ok(new
        {
            serviceId,
            serviceName = service.Name,
            basePrice,
            effectivePrice = Math.Round(effectivePrice, 2),
            discount = Math.Round(basePrice - effectivePrice, 2),
            appliedRules,
            bookingTime = target
        }));
    }
}

public class PricingRule
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "time"; // time, demand, seasonal, day-of-week
    public string AdjustmentType { get; set; } = "percentage"; // percentage or fixed
    public decimal AdjustmentValue { get; set; } // positive = surcharge, negative = discount
    public bool IsActive { get; set; } = true;
    public List<string> ApplicableDays { get; set; } = new();
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> ServiceIds { get; set; } = new();
    public int? MinBookingsThreshold { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreatePricingRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "time";
    public string AdjustmentType { get; set; } = "percentage";
    public decimal AdjustmentValue { get; set; }
    public List<string>? ApplicableDays { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? ServiceIds { get; set; }
    public int? MinBookingsThreshold { get; set; }
}
