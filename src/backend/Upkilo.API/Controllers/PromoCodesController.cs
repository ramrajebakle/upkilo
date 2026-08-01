using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/promo-codes")]
[Authorize]
public class PromoCodesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<PromoCodesController> _logger;

    public PromoCodesController(AppDbContext context, ITenantProvider tenantProvider, ILogger<PromoCodesController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPromoCodes([FromQuery] bool activeOnly = true)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.PromoCodes.Where(p => p.TenantId == tenantId.Value);
        if (activeOnly) query = query.Where(p => p.IsActive);

        var codes = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(codes);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePromoCode([FromBody] CreatePromoCodeRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var exists = await _context.PromoCodes
            .AnyAsync(p => p.TenantId == tenantId.Value && p.Code == request.Code.ToUpperInvariant());
        if (exists) return Conflict($"Promo code '{request.Code}' already exists.");

        var code = new PromoCode
        {
            TenantId = tenantId.Value,
            Code = request.Code.ToUpperInvariant(),
            Description = request.Description,
            DiscountType = Enum.Parse<PromoType>(request.DiscountType),
            DiscountValue = request.DiscountValue,
            MaxUses = request.MaxUses,
            CurrentUses = 0,
            MinimumOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MaxUsagePerCustomer = request.MaxUsagePerCustomer ?? 1,
            FirstTimeOnly = request.IsFirstTimeOnly,
            ValidFrom = request.ValidFrom ?? DateTime.UtcNow,
            ValidUntil = request.ValidUntil,
            ApplicableServices = request.ApplicableServiceIds,
            IsActive = true
        };

        _context.PromoCodes.Add(code);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Promo code '{Code}' created for tenant {TenantId}", code.Code, tenantId);
        return CreatedAtAction(nameof(GetPromoCodes), new { id = code.Id }, code);
    }

    /// <summary>
    /// Validate a promo code during booking checkout (public — no auth required).
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCode([FromBody] ValidatePromoRequest request)
    {
        var promo = await _context.PromoCodes
            .FirstOrDefaultAsync(p => p.Code == request.Code.ToUpperInvariant()
                                      && (request.TenantId == null || p.TenantId == request.TenantId)
                                      && p.IsActive);

        if (promo == null)
            return BadRequest(new { valid = false, reason = "Code not found or inactive." });

        if (promo.StartsAt.HasValue && promo.StartsAt > DateTime.UtcNow)
            return BadRequest(new { valid = false, reason = "Code is not active yet." });

        if (promo.ExpiresAt.HasValue && promo.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { valid = false, reason = "Code has expired." });

        if (promo.UsageLimit.HasValue && promo.TimesUsed >= promo.UsageLimit)
            return BadRequest(new { valid = false, reason = "Code usage limit reached." });

        if (promo.MinimumOrderAmount.HasValue && request.OrderAmount < promo.MinimumOrderAmount)
            return BadRequest(new { valid = false, reason = $"Minimum order of {promo.MinimumOrderAmount:C} required." });

        var discount = promo.DiscountType switch
        {
            PromoType.Percentage => Math.Min(
                request.OrderAmount * (promo.DiscountValue / 100m),
                promo.MaxDiscountAmount ?? decimal.MaxValue),
            PromoType.FixedAmount => Math.Min(promo.DiscountValue, request.OrderAmount),
            PromoType.FreeTrial => request.OrderAmount,
            _ => 0m
        };

        return Ok(new
        {
            valid = true,
            code = promo.Code,
            discountType = promo.DiscountType.ToString(),
            discountValue = promo.DiscountValue,
            calculatedDiscount = Math.Round(discount, 2),
            finalAmount = Math.Round(request.OrderAmount - discount, 2),
            message = promo.DiscountType == PromoType.Percentage
                ? $"{promo.DiscountValue}% off applied!"
                : $"{discount:C} discount applied!"
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeactivatePromoCode(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var promo = await _context.PromoCodes
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);
        if (promo == null) return NotFound();

        promo.IsActive = false;
        promo.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Promo code '{Code}' deactivated for tenant {TenantId}", promo.Code, tenantId);
        return Ok(new { message = $"Promo code '{promo.Code}' deactivated." });
    }
}

public class CreatePromoCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public int? MaxUses { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
    public bool IsFirstTimeOnly { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? ApplicableServiceIds { get; set; }
}

public class ValidatePromoRequest
{
    public string Code { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public decimal OrderAmount { get; set; }
}
