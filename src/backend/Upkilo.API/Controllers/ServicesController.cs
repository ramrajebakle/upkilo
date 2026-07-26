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
                    s.MaxAttendees
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

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
    [RequiresFeature("AiCopilot")]
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
}

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
    decimal? DepositAmount
);

public record UpdateServiceRequest(
    string? Name,
    string? Description,
    int? DurationMinutes,
    decimal? Price,
    string? Color,
    bool? IsActive
);

public record CreateBundleRequest(string Name, string? Description, decimal Price, List<Guid> ServiceIds);
public record AddUpsellRequest(Guid UpsellServiceId, string? Pitch, decimal? DiscountedPrice);
public record ApplyGeneratedContentRequest(bool ApplyDescription, bool ApplyMarketingCopy);
