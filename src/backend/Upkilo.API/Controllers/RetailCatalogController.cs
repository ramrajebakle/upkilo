using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Retail catalog controller for client-facing product browsing
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RetailCatalogController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<RetailCatalogController> _logger;

    public RetailCatalogController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<RetailCatalogController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get retail products (public endpoint)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts([FromQuery] Guid tenantId, [FromQuery] string? category = null)
    {
        var products = await _context.Set<InventoryItem>()
            .Where(i => i.TenantId == tenantId &&
                        i.IsActive &&
                        i.IsRetail &&
                        i.QuantityOnHand > 0)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Description,
                i.Category,
                Price = i.SalePrice ?? 0,
                InStock = i.QuantityOnHand > 0
            })
            .ToListAsync();

        if (!string.IsNullOrEmpty(category))
            products = products.Where(p => p.Category == category).ToList();

        var categories = products.Select(p => p.Category).Distinct().ToList();

        return Ok(new { data = products, categories });
    }

    /// <summary>
    /// Get product details
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(Guid id, [FromQuery] Guid tenantId)
    {
        var product = await _context.Set<InventoryItem>()
            .FirstOrDefaultAsync(i => i.Id == id &&
                                      i.TenantId == tenantId &&
                                      i.IsRetail &&
                                      i.IsActive);

        if (product == null) return NotFound();

        return Ok(new
        {
            product.Id,
            product.Name,
            product.Description,
            product.Category,
            Price = product.SalePrice ?? 0,
            InStock = product.QuantityOnHand > 0,
            QuantityAvailable = product.QuantityOnHand
        });
    }

    /// <summary>
    /// Add product to booking (for add-on sales)
    /// </summary>
    [HttpPost("add-to-booking")]
    [Authorize]
    public async Task<IActionResult> AddToBooking([FromBody] AddProductToBookingRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var product = await _context.Set<InventoryItem>()
            .FirstOrDefaultAsync(i => i.Id == request.ProductId &&
                                      i.TenantId == tenantId &&
                                      i.IsRetail);

        if (product == null) return NotFound("Product not found");

        if (product.QuantityOnHand < request.Quantity)
            return BadRequest("Insufficient stock");

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.TenantId == tenantId);

        if (booking == null) return NotFound("Booking not found");

        // Create inventory transaction
        var transaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            InventoryItemId = request.ProductId,
            Type = InventoryTransactionType.Sale,
            Quantity = -request.Quantity,
            QuantityAfter = product.QuantityOnHand - request.Quantity,
            ReferenceType = "Booking",
            ReferenceId = request.BookingId,
            Notes = $"Added to booking {booking.Id}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        product.QuantityOnHand -= request.Quantity;
        
        // Add to booking price
        var productTotal = (product.SalePrice ?? 0) * request.Quantity;
        booking.Price = (booking.Price ?? 0) + productTotal;

        _context.Set<InventoryTransaction>().Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added {Quantity}x {Product} to booking {BookingId}",
            request.Quantity, product.Name, request.BookingId);

        return Ok(new
        {
            success = true,
            productName = product.Name,
            quantity = request.Quantity,
            productTotal,
            newBookingTotal = booking.Price
        });
    }

    /// <summary>
    /// Get featured/recommended products
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeaturedProducts([FromQuery] Guid tenantId)
    {
        // Get top-selling products based on transaction count
        var productSales = await _context.Set<InventoryTransaction>()
            .Where(t => t.TenantId == tenantId && t.Type == InventoryTransactionType.Sale)
            .GroupBy(t => t.InventoryItemId)
            .Select(g => new { ProductId = g.Key, SalesCount = g.Count() })
            .OrderByDescending(x => x.SalesCount)
            .Take(6)
            .ToListAsync();

        var productIds = productSales.Select(x => x.ProductId).ToList();

        var products = await _context.Set<InventoryItem>()
            .Where(i => productIds.Contains(i.Id) && i.IsRetail && i.IsActive)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Description,
                Price = i.SalePrice ?? 0,
                InStock = i.QuantityOnHand > 0
            })
            .ToListAsync();

        return Ok(new { data = products });
    }
}

// DTOs
public record AddProductToBookingRequest(
    Guid ProductId,
    Guid BookingId,
    int Quantity = 1
);

