using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class InventoryItemsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public InventoryItemsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet("{productId}")]
    public async Task<IActionResult> GetInventoryForProduct(Guid productId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var inventory = await _context.InventoryItems
            .Where(i => i.ProductId == productId)
            .OrderByDescending(i => i.LastRestockedAt)
            .ToListAsync();

        return Ok(inventory);
    }

    [HttpPost]
    public async Task<IActionResult> Restock([FromBody] RestockRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var inventoryItem = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.LocationId == request.LocationId);

        if (inventoryItem == null)
        {
            inventoryItem = new InventoryItem
            {
                TenantId = tenantId.Value,
                ProductId = request.ProductId,
                LocationId = request.LocationId,
                Quantity = request.QuantityAdded,
                LastRestockedAt = DateTime.UtcNow,
                LowStockThreshold = request.LowStockThreshold ?? 5
            };
            _context.InventoryItems.Add(inventoryItem);
        }
        else
        {
            inventoryItem.Quantity += request.QuantityAdded;
            inventoryItem.LastRestockedAt = DateTime.UtcNow;
            if (request.LowStockThreshold.HasValue)
                inventoryItem.LowStockThreshold = request.LowStockThreshold.Value;
        }

        await _context.SaveChangesAsync();
        return Ok(inventoryItem);
    }
}

public class RestockRequest
{
    public Guid ProductId { get; set; }
    public Guid? LocationId { get; set; }
    public int QuantityAdded { get; set; }
    public int? LowStockThreshold { get; set; }
}
