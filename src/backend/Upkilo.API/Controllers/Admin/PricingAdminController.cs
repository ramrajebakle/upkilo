using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Helpers;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Stripe;

namespace Upkilo.API.Controllers.Admin;

[ApiController]
[Route("api/admin/pricing")]
[Authorize(Roles = "SuperAdmin")]
public class PricingAdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<PricingAdminController> _logger;

    public PricingAdminController(AppDbContext context, ISecretProvider secretProvider, ILogger<PricingAdminController> logger)
    {
        _context = context;
        _secretProvider = secretProvider;
        _logger = logger;
    }

    /// <summary>
    /// Clones request options with an idempotency key. Without one, a retried or double-clicked
    /// sync creates duplicate Stripe products and prices, and Stripe offers no way to merge them
    /// afterwards.
    /// </summary>
    private static RequestOptions WithIdempotency(RequestOptions source, string key) =>
        new() { ApiKey = source.ApiKey, IdempotencyKey = key };

    [HttpPost("seed")]
    public async Task<IActionResult> SeedPricing()
    {
        await Upkilo.Infrastructure.Data.Seeders.PricingSeeder.SeedAsync(_context);
        return Ok(new { message = "Database seeded with Pricing Plans and Features." });
    }

    /// <summary>
    /// Reports problems with the published pricing catalogue — missing cycles, duplicate rows,
    /// non-discounted annual pricing, partial Stripe sync.
    ///
    /// The same checks back the "pricing" health probe; this endpoint exposes the detail, so an
    /// operator seeing /ready go unhealthy can find out exactly which plan is wrong.
    /// </summary>
    [HttpGet("validate")]
    public async Task<IActionResult> ValidatePricing(
        [FromServices] Upkilo.Infrastructure.Services.PricingIntegrityService pricing,
        CancellationToken ct)
    {
        var issues = await pricing.ValidateAsync(ct);
        var critical = issues.Count(i => i.Severity == Upkilo.Infrastructure.Services.PricingIssueSeverity.Critical);

        return Ok(new
        {
            valid = critical == 0,
            criticalCount = critical,
            warningCount = issues.Count - critical,
            issues = issues.Select(i => new
            {
                severity = i.Severity.ToString(),
                code = i.Code,
                message = i.Message
            })
        });
    }

    /// <summary>
    /// Creates Stripe Products and Prices for any plans that are missing StripeProductId / StripePriceId,
    /// then persists the IDs back to the database. Run once after initial seed.
    /// </summary>
    [HttpPost("sync-stripe")]
    public async Task<IActionResult> SyncWithStripe()
    {
        var stripeKey = _secretProvider.GetSecret("Stripe--SecretKey");
        if (string.IsNullOrEmpty(stripeKey))
            return StatusCode(500, new { error = "Stripe API key not configured." });

        var requestOptions = new RequestOptions { ApiKey = stripeKey };

        var plans = await _context.PricingPlans
            .Include(p => p.Prices)
            .ToListAsync();

        var productService = new ProductService();
        var priceService = new PriceService();

        var synced = new List<object>();
        var failures = new List<object>();

        foreach (var plan in plans)
        {
            // Each plan is wrapped and saved independently. Previously a single Stripe failure
            // aborted the whole run before SaveChangesAsync, so IDs for everything created up to
            // that point were lost locally while the objects still existed in Stripe — and the
            // next run created duplicates of all of them.
            try
            {
                if (string.IsNullOrEmpty(plan.StripeProductId))
                {
                    var product = await productService.CreateAsync(new ProductCreateOptions
                    {
                        Name = plan.Name,
                        Description = plan.Description,
                        Metadata = new Dictionary<string, string> { { "plan_id", plan.Id.ToString() } }
                    }, WithIdempotency(requestOptions, $"product_{plan.Id}"));

                    plan.StripeProductId = product.Id;
                    _logger.LogInformation("Created Stripe product {ProductId} for plan {PlanName}", product.Id, plan.Name);
                }

                foreach (var price in plan.Prices.Where(p => string.IsNullOrEmpty(p.StripePriceId)))
                {
                    var interval = price.Cycle == BillingCycle.Annual ? "year" : "month";
                    // Was `(long)(price.Amount * 100)` for every currency. Zero-decimal currencies
                    // (JPY, KRW, VND …) have no minor unit, so scaling by 100 created a Stripe price
                    // 100x the intended amount. Currency.ToMinorUnits applies the ISO 4217 exponent.
                    var unitAmount = Currency.ToMinorUnits(price.Amount, price.CurrencyCode);

                    if (unitAmount <= 0)
                    {
                        failures.Add(new { plan = plan.Name, cycle = price.Cycle.ToString(), error = "Amount is not positive; refusing to create a free Stripe price." });
                        continue;
                    }

                    var stripePrice = await priceService.CreateAsync(new PriceCreateOptions
                    {
                        Product = plan.StripeProductId,
                        UnitAmount = unitAmount,
                        Currency = price.CurrencyCode.ToLower(),
                        Recurring = new PriceRecurringOptions { Interval = interval },
                        Metadata = new Dictionary<string, string>
                        {
                            { "plan_id", plan.Id.ToString() },
                            { "cycle", price.Cycle.ToString() }
                        }
                        // Keyed on the amount as well as the plan and cycle: Stripe prices are
                        // immutable, so a corrected amount must create a NEW price rather than
                        // replay the old one from the idempotency cache.
                    }, WithIdempotency(requestOptions, $"price_{price.Id}_{price.Cycle}_{unitAmount}_{price.CurrencyCode}"));

                    price.StripePriceId = stripePrice.Id;
                    _logger.LogInformation("Created Stripe price {PriceId} ({Cycle}) for plan {PlanName}", stripePrice.Id, price.Cycle, plan.Name);
                }

                // Persist per plan so a later failure cannot discard IDs already obtained.
                await _context.SaveChangesAsync();

                synced.Add(new { plan = plan.Name, stripeProductId = plan.StripeProductId, prices = plan.Prices.Select(p => new { p.Cycle, p.StripePriceId }) });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe sync failed for plan {PlanName}", plan.Name);
                failures.Add(new { plan = plan.Name, error = ex.Message });
            }
        }

        if (failures.Count > 0)
        {
            // 207: some plans synced, some did not. Returning 200 here would report a partial
            // sync as success and leave unbuyable plans live.
            return StatusCode(207, new { message = "Stripe sync completed with failures.", synced, failures });
        }
        return Ok(new { message = "Stripe sync complete.", synced });
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _context.PricingPlans
            .Include(p => p.Prices)
            .Include(p => p.FeatureMappings)
            .ThenInclude(fm => fm.PricingFeature)
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] PricingPlan plan)
    {
        _context.PricingPlans.Add(plan);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPlans), new { id = plan.Id }, plan);
    }

    [HttpPut("plans/{id}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] PricingPlan planData)
    {
        var plan = await _context.PricingPlans.FindAsync(id);
        if (plan == null) return NotFound();

        plan.Name = planData.Name;
        plan.Description = planData.Description;
        plan.IsActive = planData.IsActive;
        plan.TrialDays = planData.TrialDays;
        if (!string.IsNullOrEmpty(planData.StripeProductId))
            plan.StripeProductId = planData.StripeProductId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("plans/{id}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        var plan = await _context.PricingPlans.FindAsync(id);
        if (plan == null) return NotFound();

        _context.PricingPlans.Remove(plan);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures()
    {
        var features = await _context.PricingFeatures.ToListAsync();
        return Ok(features);
    }

    [HttpPost("features")]
    public async Task<IActionResult> CreateFeature([FromBody] PricingFeature feature)
    {
        _context.PricingFeatures.Add(feature);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetFeatures), new { id = feature.Id }, feature);
    }

    [HttpGet("discounts")]
    public async Task<IActionResult> GetDiscounts()
    {
        var discounts = await _context.PlatformDiscounts.ToListAsync();
        return Ok(discounts);
    }

    [HttpPost("discounts")]
    public async Task<IActionResult> CreateDiscount([FromBody] PlatformDiscount discount)
    {
        _context.PlatformDiscounts.Add(discount);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetDiscounts), new { id = discount.Id }, discount);
    }

    [HttpPut("discounts/{id}")]
    public async Task<IActionResult> UpdateDiscount(Guid id, [FromBody] PlatformDiscount discountData)
    {
        var discount = await _context.PlatformDiscounts.FindAsync(id);
        if (discount == null) return NotFound();

        discount.Code = discountData.Code;
        discount.Description = discountData.Description;
        discount.Type = discountData.Type;
        discount.Value = discountData.Value;
        discount.IsActive = discountData.IsActive;
        discount.ValidUntil = discountData.ValidUntil;
        discount.MaxRedemptions = discountData.MaxRedemptions;
        discount.StripeCouponId = discountData.StripeCouponId;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
