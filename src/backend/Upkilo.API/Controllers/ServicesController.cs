using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.API.Middleware;

namespace Upkilo.API.Controllers;

/// <summary>
/// Services controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly ILogger<ServicesController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAIService _aiService;
    private readonly ICacheService _cache;

    public ServicesController(ILogger<ServicesController> logger, AppDbContext context, ITenantProvider tenantProvider, IAIService aiService, ICacheService cache)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _aiService = aiService;
        _cache = cache;
    }

    /// <summary>
    /// Get all services. SC7: Redis L2 cache with 5-min TTL and tag-based invalidation on write.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetServices()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var services = await _cache.GetOrSetAsync<List<object>>(
            tenantId.Value,
            "catalog:services",
            async () => await _context.Services
                .Where(s => s.TenantId == tenantId.Value && s.IsActive)
                .Select(s => (object)new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.DurationMinutes,
                    s.Price,
                    s.Currency,
                    s.Color,
                    s.IsActive,
                    s.MaxAttendees,
                    // Included so the services list and edit form can show the policy without a
                    // per-row fetch, and so the booking page can state it before someone pays.
                    s.FullRefundHours,
                    s.PartialRefundHours,
                    s.PartialRefundPercent,
                    s.CancellationPolicy,
                    s.RebookAfterDays,
                    s.IsMobile,
                    s.TravelBufferMinutes
                })
                .ToListAsync(),
            expiration: TimeSpan.FromMinutes(5));

        return Ok(new { data = services ?? new List<object>() });
    }

    /// <summary>
    /// Get service by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetService(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (service == null) return NotFound();

        return Ok(service);
    }

    /// <summary>
    /// Create service
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Currency is inherited from the tenant, never taken from the request.
        //
        // A business settles through one Stripe account in one currency, so a per-service currency
        // could only ever diverge from it. Worse, mixed currencies within a tenant silently corrupt
        // every revenue figure: the revenue sums across the codebase add Price values without
        // grouping by currency, so one INR service alongside a USD one produces a total that is
        // the sum of two different units.
        //
        // The tenant's own currency comes from their connected Stripe account — see
        // TenantCurrencySyncService.
        var tenantCurrency = await _context.Tenants
            .Where(t => t.Id == tenantId.Value)
            .Select(t => t.Currency)
            .FirstOrDefaultAsync();

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            Currency = Upkilo.Core.Helpers.Currency.Normalize(tenantCurrency),
            Color = request.Color ?? "#3B82F6",
            BufferBeforeMinutes = request.BufferBefore,
            BufferAfterMinutes = request.BufferAfter,
            MaxAttendees = request.MaxAttendees,
            RequiresPayment = request.RequiresPayment,
            DepositAmount = request.DepositAmount,
            FullRefundHours = request.FullRefundHours ?? 18,
            PartialRefundHours = request.PartialRefundHours ?? 12,
            PartialRefundPercent = request.PartialRefundPercent ?? 50m,
            CancellationPolicy = request.CancellationPolicy,
            RebookAfterDays = request.RebookAfterDays,
            IsMobile = request.IsMobile ?? false,
            TravelBufferMinutes = request.TravelBufferMinutes ?? 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Validated rather than trusted: these three numbers decide whether a customer gets
        // their money back, and the refund engine reads them directly. A negative or inverted
        // window would otherwise be stored and silently reinterpreted at cancellation time.
        if (ValidateRefundPolicy(service.FullRefundHours, service.PartialRefundHours, service.PartialRefundPercent) is { } createError)
            return BadRequest(new { error = createError });

        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        await _cache.InvalidateAsync(service.TenantId, "catalog:services");

        _logger.LogInformation("Service created: {ServiceId}", service.Id);

        return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
    }

    /// <summary>
    /// Update service
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (service == null) return NotFound();

        if (request.Name != null) service.Name = request.Name;
        if (request.Description != null) service.Description = request.Description;
        if (request.DurationMinutes.HasValue) service.DurationMinutes = request.DurationMinutes.Value;
        if (request.Price.HasValue) service.Price = request.Price.Value;
        if (request.Color != null) service.Color = request.Color;
        if (request.IsActive.HasValue) service.IsActive = request.IsActive.Value;
        if (request.FullRefundHours.HasValue) service.FullRefundHours = request.FullRefundHours.Value;
        if (request.PartialRefundHours.HasValue) service.PartialRefundHours = request.PartialRefundHours.Value;
        if (request.PartialRefundPercent.HasValue) service.PartialRefundPercent = request.PartialRefundPercent.Value;
        if (request.CancellationPolicy != null) service.CancellationPolicy = request.CancellationPolicy;
        if (request.RebookAfterDays.HasValue) service.RebookAfterDays = request.RebookAfterDays.Value;
        if (request.IsMobile.HasValue) service.IsMobile = request.IsMobile.Value;
        if (request.TravelBufferMinutes.HasValue) service.TravelBufferMinutes = request.TravelBufferMinutes.Value;

        // Re-validated against the merged result, not the request: a partial update that changes
        // only one threshold can still leave the pair inverted.
        if (ValidateRefundPolicy(service.FullRefundHours, service.PartialRefundHours, service.PartialRefundPercent) is { } updateError)
            return BadRequest(new { error = updateError });

        await _context.SaveChangesAsync();
        await _cache.InvalidateAsync(service.TenantId, "catalog:services");
        _logger.LogInformation("Service updated: {ServiceId}", id);

        return Ok(service);
    }

    /// <summary>
    /// Delete service
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (service == null) return NotFound();

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Service deleted: {ServiceId}", id);
        return NoContent();
    }

    /// <summary>
    /// Create a service bundle
    /// </summary>
    [HttpPost("bundles")]
    public async Task<IActionResult> CreateBundle([FromBody] CreateBundleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var bundleService = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        int totalDuration = 0;
        for (int i = 0; i < request.ServiceIds.Count; i++)
        {
            var componentId = request.ServiceIds[i];
            var component = await _context.Services.FindAsync(componentId);
            if (component != null)
            {
                totalDuration += component.DurationMinutes;
                bundleService.BundleItems.Add(new ServiceBundleItem
                {
                    Id = Guid.NewGuid(),
                    BundleServiceId = bundleService.Id,
                    ComponentServiceId = componentId,
                    Order = i
                });
            }
        }

        bundleService.DurationMinutes = totalDuration;
        _context.Services.Add(bundleService);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Service bundle created: {BundleId} with {Count} components", bundleService.Id, request.ServiceIds.Count);

        return Ok(bundleService);
    }

    /// <summary>
    /// Add an upsell suggestion to a service
    /// </summary>
    [HttpPost("{id}/upsells")]
    public async Task<IActionResult> AddUpsell(Guid id, [FromBody] AddUpsellRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var upsell = new ServiceUpsell
        {
            Id = Guid.NewGuid(),
            MainServiceId = id,
            UpsellServiceId = request.UpsellServiceId,
            Pitch = request.Pitch,
            DiscountedPrice = request.DiscountedPrice,
            CreatedAt = DateTime.UtcNow
        };

        _context.ServiceUpsells.Add(upsell);
        await _context.SaveChangesAsync();

        return Ok(upsell);
    }

    /// <summary>
    /// A5: Auto-generate SEO description + marketing copy for a service using AI.
    /// Requires AiCopilot feature flag. Generated content is returned for preview;
    /// caller decides whether to apply via PATCH /services/{id}.
    /// </summary>
    [HttpPost("{id}/ai-content")]
    [RequiresFeature(FeatureKeys.AiCopilot)]
    public async Task<IActionResult> GenerateServiceContent(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.Services
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

        if (service == null) return NotFound();

        var businessName = service.Tenant?.Name ?? "our business";

        var prompt =
            $"You are a marketing copywriter for a service business. " +
            $"Write compelling content for the following service:\n\n" +
            $"Business: {businessName}\n" +
            $"Service Name: {service.Name}\n" +
            $"Duration: {service.DurationMinutes} minutes\n" +
            $"Price: {service.Price:C}\n\n" +
            $"Return ONLY a JSON object (no markdown) with exactly these fields:\n" +
            "{ \"description\": \"2-3 sentence SEO-friendly service description\", " +
            "\"tagline\": \"one punchy headline under 10 words\", " +
            "\"benefits\": [\"benefit 1\", \"benefit 2\", \"benefit 3\"], " +
            "\"seoKeywords\": [\"keyword1\", \"keyword2\", \"keyword3\"] }";

        var result = await _aiService.GenerateTextAsync(tenantId.Value, Guid.Empty, prompt);
        if (!result.Success)
            return StatusCode(503, new { error = "AI content generation unavailable.", details = result.Error });

        var json = result.Content?.Trim() ?? "{}";
        // Strip markdown fences if present
        if (json.StartsWith("```")) json = string.Join("\n", json.Split('\n').Skip(1).TakeWhile(l => !l.StartsWith("```")));

        _logger.LogInformation("[A5] Generated content for service {ServiceId} in tenant {TenantId}", id, tenantId);

        return Ok(new
        {
            serviceId = id,
            serviceName = service.Name,
            generated = System.Text.Json.JsonDocument.Parse(json).RootElement,
            previewOnly = true,
            hint = "Call PATCH /services/{id} with the description field to apply."
        });
    }

    /// <summary>
    /// Get upsell suggestions for a service
    /// </summary>
    [HttpGet("{id}/upsells")]
    public async Task<IActionResult> GetUpsells(Guid id)
    {
        var upsells = await _context.ServiceUpsells
            .Include(u => u.UpsellService)
            .Where(u => u.MainServiceId == id)
            .Select(u => new
            {
                u.UpsellServiceId,
                u.UpsellService!.Name,
                u.UpsellService.Description,
                u.Pitch,
                u.DiscountedPrice,
                OriginalPrice = u.UpsellService.Price,
                u.UpsellService.DurationMinutes
            })
            .ToListAsync();

        return Ok(upsells);
    }

    /// <summary>
    /// Returns an error message if the refund policy is not coherent, otherwise null.
    /// </summary>
    /// <remarks>
    /// PublicBookingController.CanCancel defends itself by ordering the two thresholds before
    /// use, so a bad pair cannot produce a perverse refund at cancellation time. That is a last
    /// line of defence, not a substitute for rejecting the input: a tenant who saves 12/18 the
    /// wrong way round should be told, not silently have it reinterpreted, because they would
    /// otherwise believe a policy is in force that is not the one being applied.
    /// </remarks>
    private static string? ValidateRefundPolicy(int fullRefundHours, int partialRefundHours, decimal partialRefundPercent)
    {
        if (fullRefundHours < 0 || partialRefundHours < 0)
            return "Refund windows cannot be negative.";

        if (partialRefundHours > fullRefundHours)
            return "The partial-refund window must be shorter than the full-refund window — a later cancellation cannot earn a larger refund.";

        if (partialRefundPercent < 0m || partialRefundPercent > 100m)
            return "Partial refund percentage must be between 0 and 100.";

        return null;
    }
}

// Refund policy is captured per service at creation. Nullable on the request only so that an
// older client that does not send it still works — the controller substitutes the entity
// defaults (18h / 12h / 50%) rather than leaving a service with no policy at all.
public record CreateServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string? Currency,
    string? Color,
    int BufferBefore,
    int BufferAfter,
    int MaxAttendees,
    bool RequiresPayment,
    decimal? DepositAmount,
    int? FullRefundHours = null,
    int? PartialRefundHours = null,
    decimal? PartialRefundPercent = null,
    string? CancellationPolicy = null,
    int? RebookAfterDays = null,
    bool? IsMobile = null,
    int? TravelBufferMinutes = null
);

public record UpdateServiceRequest(
    string? Name,
    string? Description,
    int? DurationMinutes,
    decimal? Price,
    string? Color,
    bool? IsActive,
    int? FullRefundHours = null,
    int? PartialRefundHours = null,
    decimal? PartialRefundPercent = null,
    string? CancellationPolicy = null,
    int? RebookAfterDays = null,
    bool? IsMobile = null,
    int? TravelBufferMinutes = null
);

public record CreateBundleRequest(string Name, string? Description, decimal Price, List<Guid> ServiceIds);

public record AddUpsellRequest(Guid UpsellServiceId, string? Pitch, decimal? DiscountedPrice);
public record ApplyGeneratedContentRequest(bool ApplyDescription, bool ApplyMarketingCopy);
