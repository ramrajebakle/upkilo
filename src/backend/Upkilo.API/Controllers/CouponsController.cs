using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Coupons controller for discount/promo code management.
/// Uses real database queries against PromoCodes table.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CouponsController : ControllerBase
{
    private readonly ILogger<CouponsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CouponsController(
        ILogger<CouponsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all coupons with filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCoupons(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? active = null,
        [FromQuery] string? search = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.PromoCodes
            .Where(c => c.TenantId == tenantId.Value && !c.IsDeleted);

        if (active.HasValue)
            query = query.Where(c => c.IsActive == active.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Code.Contains(search));

        var total = await query.CountAsync();

        var coupons = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.DiscountType,
                c.DiscountValue,
                c.MinimumOrderAmount,
                c.UsageLimit,
                c.TimesUsed,
                c.FirstTimeOnly,
                c.ExpiresAt,
                c.IsActive,
                isExpired = c.ExpiresAt.HasValue && c.ExpiresAt.Value < DateTime.UtcNow,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = coupons, total, page, pageSize });
    }

    /// <summary>
    /// Get coupon by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCoupon(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var coupon = await _context.PromoCodes
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (coupon == null) return NotFound();

        return Ok(new
        {
            coupon.Id,
            coupon.Code,
            coupon.DiscountType,
            coupon.DiscountValue,
            coupon.MinimumOrderAmount,
            coupon.UsageLimit,
            coupon.TimesUsed,
            coupon.FirstTimeOnly,
            coupon.ApplicableServices,
            coupon.ExpiresAt,
            coupon.IsActive,
            isExpired = coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow,
            coupon.CreatedAt,
            coupon.UpdatedAt
        });
    }

    /// <summary>
    /// Create a coupon
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { error = "Coupon code is required." });

        // Check for duplicate code
        var exists = await _context.PromoCodes
            .AnyAsync(c => c.TenantId == tenantId.Value && c.Code == request.Code.ToUpper() && !c.IsDeleted);
        if (exists)
            return Conflict(new { error = "A coupon with this code already exists." });

        var coupon = new PromoCode
        {
            TenantId = tenantId.Value,
            Code = request.Code.ToUpper(),
            DiscountType = Enum.TryParse<PromoType>(request.DiscountType, out var pt) ? pt : PromoType.Percentage,
            DiscountValue = request.DiscountValue,
            MinimumOrderAmount = request.MinimumOrderAmount,
            UsageLimit = request.UsageLimit,
            FirstTimeOnly = request.FirstTimeOnly,
            ApplicableServices = request.ApplicableServices,
            ExpiresAt = request.ExpiresAt,
            IsActive = true
        };

        _context.PromoCodes.Add(coupon);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Coupon created: {Code} ({DiscountType} {DiscountValue})", coupon.Code, coupon.DiscountType, coupon.DiscountValue);

        return CreatedAtAction(nameof(GetCoupon), new { id = coupon.Id }, new
        {
            coupon.Id,
            coupon.Code,
            coupon.DiscountType,
            coupon.DiscountValue,
            coupon.CreatedAt
        });
    }

    /// <summary>
    /// Create a batch of coupons
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> CreateCouponBatch([FromBody] CreateCouponBatchRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.Count <= 0 || request.Count > 500)
            return BadRequest(new { error = "Count must be between 1 and 500." });

        if (string.IsNullOrWhiteSpace(request.Prefix))
            return BadRequest(new { error = "Prefix is required." });

        var randomLength = request.RandomLength > 0 ? request.RandomLength : 6;
        var coupons = new List<PromoCode>();
        var random = new Random();

        for (int i = 0; i < request.Count; i++)
        {
            var randomString = Guid.NewGuid().ToString("N").Substring(0, Math.Min(randomLength, 32)).ToUpper();
            var code = $"{request.Prefix.ToUpper()}-{randomString}";

            // Ensure uniqueness within the batch
            if (coupons.Any(c => c.Code == code)) continue;

            coupons.Add(new PromoCode
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                Code = code,
                DiscountType = Enum.TryParse<PromoType>(request.DiscountType, out var pt) ? pt : PromoType.Percentage,
                DiscountValue = request.DiscountValue,
                MinimumOrderAmount = request.MinimumOrderAmount,
                UsageLimit = request.UsageLimit,
                MaxUsagePerCustomer = request.MaxUsagePerCustomer,
                FirstTimeOnly = request.FirstTimeOnly,
                ApplicableServices = request.ApplicableServices,
                StartsAt = request.StartsAt,
                ExpiresAt = request.ExpiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.PromoCodes.AddRange(coupons);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated batch of {Count} coupons starting with {Prefix}", coupons.Count, request.Prefix);

        return Ok(new { success = true, count = coupons.Count, codes = coupons.Select(c => c.Code) });
    }

    /// <summary>
    /// Update a coupon
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCoupon(Guid id, [FromBody] UpdateCouponRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var coupon = await _context.PromoCodes
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (coupon == null) return NotFound();

        if (request.DiscountValue.HasValue) coupon.DiscountValue = request.DiscountValue.Value;
        if (request.MinimumOrderAmount.HasValue) coupon.MinimumOrderAmount = request.MinimumOrderAmount;
        if (request.UsageLimit.HasValue) coupon.UsageLimit = request.UsageLimit;
        if (request.ExpiresAt.HasValue) coupon.ExpiresAt = request.ExpiresAt;
        if (request.IsActive.HasValue) coupon.IsActive = request.IsActive.Value;
        coupon.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, coupon.UpdatedAt });
    }

    /// <summary>
    /// Delete a coupon (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCoupon(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var coupon = await _context.PromoCodes
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (coupon == null) return NotFound();

        coupon.IsDeleted = true;
        coupon.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Coupon deleted: {Code}", coupon.Code);
        return NoContent();
    }

    /// <summary>
    /// Validate a coupon code (used before applying)
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var coupon = await _context.PromoCodes
            .FirstOrDefaultAsync(c => c.TenantId == tenantId.Value && c.Code == request.Code.ToUpper() && !c.IsDeleted);

        if (coupon == null)
            return Ok(new { valid = false, error = "Coupon code not found." });

        if (!coupon.IsActive)
            return Ok(new { valid = false, error = "This coupon is no longer active." });

        if (coupon.StartsAt.HasValue && coupon.StartsAt.Value > DateTime.UtcNow)
            return Ok(new { valid = false, error = "This coupon is not yet valid." });

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
            return Ok(new { valid = false, error = "This coupon has expired." });

        if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
            return Ok(new { valid = false, error = "This coupon has reached its total usage limit." });

        if (coupon.MinimumOrderAmount.HasValue && request.OrderAmount < coupon.MinimumOrderAmount.Value)
            return Ok(new { valid = false, error = $"Minimum order amount is {coupon.MinimumOrderAmount:C}." });

        // Strict Check: MaxUsagePerCustomer
        if (coupon.MaxUsagePerCustomer.HasValue && request.ClientId.HasValue)
        {
            var customerUsageCount = await _context.PromoRedemptions
                .CountAsync(r => r.PromoCodeId == coupon.Id && r.ClientId == request.ClientId.Value);

            if (customerUsageCount >= coupon.MaxUsagePerCustomer.Value)
            {
                return Ok(new { valid = false, error = "You have reached the maximum usage limit for this coupon." });
            }
        }

        // Strict Check: ApplicableServices
        if (!string.IsNullOrEmpty(coupon.ApplicableServices) && request.ServiceId.HasValue)
        {
            var allowedServices = coupon.ApplicableServices.Split(',').Select(id => id.Trim());
            if (!allowedServices.Contains(request.ServiceId.Value.ToString()))
            {
                return Ok(new { valid = false, error = "This coupon cannot be applied to the selected service." });
            }
        }

        // Strict Check: First Time Only
        if (coupon.FirstTimeOnly && request.ClientId.HasValue)
        {
            var hasPreviousBookings = await _context.Bookings
                .AnyAsync(b => b.ClientId == request.ClientId.Value && b.Status == BookingStatus.Completed);

            if (hasPreviousBookings)
            {
                return Ok(new { valid = false, error = "This coupon is valid for new clients only." });
            }
        }

        var discount = coupon.DiscountType == PromoType.Percentage
            ? request.OrderAmount * (coupon.DiscountValue / 100m)
            : coupon.DiscountValue;

        return Ok(new
        {
            valid = true,
            couponId = coupon.Id,
            coupon.Code,
            coupon.DiscountType,
            coupon.DiscountValue,
            calculatedDiscount = Math.Min(discount, request.OrderAmount)
        });
    }

    /// <summary>
    /// Apply coupon (increment usage counter and record redemption)
    /// </summary>
    [HttpPost("{id}/apply")]
    public async Task<IActionResult> ApplyCoupon(Guid id, [FromBody] ApplyCouponRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var coupon = await _context.PromoCodes
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted && c.IsActive);

            if (coupon == null) return NotFound();

            // 1. Create redemption record
            var redemption = new PromoRedemption
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                PromoCodeId = coupon.Id,
                ClientId = request.ClientId,
                BookingId = request.BookingId,
                RedeemedAt = DateTime.UtcNow,
                DiscountApplied = request.DiscountApplied
            };

            // 2. Increment global usage
            coupon.TimesUsed++;
            coupon.UpdatedAt = DateTime.UtcNow;

            _context.PromoRedemptions.Add(redemption);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Coupon {Code} applied successfully for Client {ClientId}. Discount: {Discount}", coupon.Code, request.ClientId, request.DiscountApplied);

            return Ok(new { success = true, timesUsed = coupon.TimesUsed, redemptionId = redemption.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to apply coupon {Id}", id);
            return StatusCode(500, "Internal server error occurred while applying coupon.");
        }
    }

    /// <summary>
    /// Deactivate a coupon
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateCoupon(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var coupon = await _context.PromoCodes
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (coupon == null) return NotFound();

        coupon.IsActive = false;
        coupon.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Coupon deactivated." });
    }

    /// <summary>
    /// Duplicate a coupon with a new code
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> DuplicateCoupon(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var original = await _context.PromoCodes
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (original == null) return NotFound();

        var duplicate = new PromoCode
        {
            TenantId = tenantId.Value,
            Code = $"{original.Code}-COPY",
            DiscountType = original.DiscountType,
            DiscountValue = original.DiscountValue,
            MinimumOrderAmount = original.MinimumOrderAmount,
            UsageLimit = original.UsageLimit,
            FirstTimeOnly = original.FirstTimeOnly,
            ApplicableServices = original.ApplicableServices,
            ExpiresAt = original.ExpiresAt,
            IsActive = true
        };

        _context.PromoCodes.Add(duplicate);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCoupon), new { id = duplicate.Id }, new { duplicate.Id, duplicate.Code });
    }

    /// <summary>
    /// Bulk delete coupons (soft delete)
    /// </summary>
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (ids == null || !ids.Any())
            return BadRequest("No coupon IDs provided.");

        var couponsToDelete = await _context.PromoCodes
            .Where(c => ids.Contains(c.Id) && c.TenantId == tenantId.Value && !c.IsDeleted)
            .ToListAsync();

        foreach (var coupon in couponsToDelete)
        {
            coupon.IsDeleted = true;
            coupon.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk deleted {Count} coupons in tenant {TenantId}", couponsToDelete.Count, tenantId);

        return Ok(new { deletedCount = couponsToDelete.Count });
    }

    /// <summary>
    /// Get coupon usage analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetCouponAnalytics()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var coupons = await _context.PromoCodes
            .Where(c => c.TenantId == tenantId.Value && !c.IsDeleted)
            .ToListAsync();

        var totalCoupons = coupons.Count;
        var activeCoupons = coupons.Count(c => c.IsActive);
        var expiredCoupons = coupons.Count(c => c.ExpiresAt.HasValue && c.ExpiresAt.Value < DateTime.UtcNow);
        var totalRedemptions = coupons.Sum(c => c.TimesUsed);
        var topCoupons = coupons
            .Where(c => c.TimesUsed > 0)
            .OrderByDescending(c => c.TimesUsed)
            .Take(5)
            .Select(c => new { c.Code, c.TimesUsed, c.DiscountType, c.DiscountValue })
            .ToList();

        return Ok(new
        {
            totalCoupons,
            activeCoupons,
            expiredCoupons,
            totalRedemptions,
            topCoupons
        });
    }
}

// Request DTOs
public class CreateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
    public bool FirstTimeOnly { get; set; }
    public string? ApplicableServices { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateCouponRequest
{
    public decimal? DiscountValue { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool? IsActive { get; set; }
}

public class ValidateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public Guid? ServiceId { get; set; }
    public Guid? ClientId { get; set; }
}

public class ApplyCouponRequest
{
    public Guid? BookingId { get; set; }
    public Guid? ClientId { get; set; }
    public decimal DiscountApplied { get; set; }
}

public class CreateCouponBatchRequest : CreateCouponRequest
{
    public string Prefix { get; set; } = string.Empty;
    public int Count { get; set; }
    public int RandomLength { get; set; } = 6;
}

