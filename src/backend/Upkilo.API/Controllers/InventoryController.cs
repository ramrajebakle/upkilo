using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Inventory controller for managing products and stock
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<InventoryController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory items
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetItems([FromQuery] string? category = null, [FromQuery] bool? lowStock = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Set<InventoryItem>()
            .Where(i => i.TenantId == tenantId && i.IsActive);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(i => i.Category == category);

        if (lowStock == true)
            query = query.Where(i => i.QuantityOnHand <= i.ReorderLevel);

        var items = await query
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Sku,
                i.Category,
                i.CostPrice,
                i.SalePrice,
                i.QuantityOnHand,
                i.ReorderLevel,
                IsLowStock = i.QuantityOnHand <= i.ReorderLevel,
                i.IsRetail
            })
            .ToListAsync();

        return Ok(new { data = items });
    }

    /// <summary>
    /// Get inventory item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetItem(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var item = await _context.Set<InventoryItem>()
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (item == null) return NotFound();

        return Ok(item);
    }

    /// <summary>
    /// Create inventory item
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateItem([FromBody] CreateInventoryItemRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            Sku = request.Sku,
            Description = request.Description,
            Category = request.Category,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            QuantityOnHand = request.InitialQuantity,
            ReorderLevel = request.ReorderLevel,
            ReorderQuantity = request.ReorderQuantity,
            Supplier = request.Supplier,
            IsRetail = request.IsRetail,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<InventoryItem>().Add(item);

        // Record initial stock transaction
        if (request.InitialQuantity > 0)
        {
            var transaction = new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                InventoryItemId = item.Id,
                Type = InventoryTransactionType.StockIn,
                Quantity = request.InitialQuantity,
                QuantityAfter = request.InitialQuantity,
                Notes = "Initial stock",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Set<InventoryTransaction>().Add(transaction);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created inventory item {Name} with {Quantity} units", request.Name, request.InitialQuantity);

        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }

    /// <summary>
    /// Update inventory item
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateInventoryItemRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var item = await _context.Set<InventoryItem>()
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (item == null) return NotFound();

        if (request.Name != null) item.Name = request.Name;
        if (request.Sku != null) item.Sku = request.Sku;
        if (request.Description != null) item.Description = request.Description;
        if (request.Category != null) item.Category = request.Category;
        if (request.CostPrice.HasValue) item.CostPrice = request.CostPrice.Value;
        if (request.SalePrice.HasValue) item.SalePrice = request.SalePrice;
        if (request.ReorderLevel.HasValue) item.ReorderLevel = request.ReorderLevel.Value;
        if (request.Supplier != null) item.Supplier = request.Supplier;
        if (request.IsRetail.HasValue) item.IsRetail = request.IsRetail.Value;
        if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;

        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(item);
    }

    /// <summary>
    /// Adjust stock quantity
    /// </summary>
    [HttpPost("{id}/adjust")]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] InventoryStockAdjustRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var item = await _context.Set<InventoryItem>()
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (item == null) return NotFound();

        var newQuantity = item.QuantityOnHand + request.QuantityChange;
        if (newQuantity < 0)
            return BadRequest("Insufficient stock for this adjustment");

        item.QuantityOnHand = newQuantity;
        item.UpdatedAt = DateTime.UtcNow;

        var transaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            InventoryItemId = id,
            Type = request.Type,
            Quantity = request.QuantityChange,
            QuantityAfter = newQuantity,
            Notes = request.Notes,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<InventoryTransaction>().Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Adjusted stock for {ItemId}: {Change} ({Type})", id, request.QuantityChange, request.Type);

        return Ok(new { newQuantity, transactionId = transaction.Id });
    }

    /// <summary>
    /// Get transaction history for an item
    /// </summary>
    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Set<InventoryTransaction>()
            .Where(t => t.InventoryItemId == id && t.TenantId == tenantId);

        var total = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { data = transactions, total, page, pageSize });
    }

    /// <summary>
    /// Get low stock alerts
    /// </summary>
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockAlerts()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var lowStock = await _context.Set<InventoryItem>()
            .Where(i => i.TenantId == tenantId && i.IsActive && i.QuantityOnHand <= i.ReorderLevel)
            .OrderBy(i => i.QuantityOnHand)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Sku,
                i.QuantityOnHand,
                i.ReorderLevel,
                i.ReorderQuantity,
                i.Supplier,
                Urgency = i.QuantityOnHand == 0 ? "critical" : "low"
            })
            .ToListAsync();

        return Ok(new
        {
            data = lowStock,
            summary = new
            {
                outOfStock = lowStock.Count(x => x.Urgency == "critical"),
                low = lowStock.Count(x => x.Urgency == "low")
            }
        });
    }

    /// <summary>
    /// Get inventory value summary
    /// </summary>
    [HttpGet("value")]
    public async Task<IActionResult> GetInventoryValue()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var items = await _context.Set<InventoryItem>()
            .Where(i => i.TenantId == tenantId && i.IsActive)
            .ToListAsync();

        var totalCostValue = items.Sum(i => i.QuantityOnHand * i.CostPrice);
        var totalRetailValue = items.Where(i => i.IsRetail && i.SalePrice.HasValue)
            .Sum(i => i.QuantityOnHand * i.SalePrice!.Value);

        return Ok(new
        {
            totalItems = items.Count,
            totalUnits = items.Sum(i => i.QuantityOnHand),
            totalCostValue,
            totalRetailValue,
            potentialProfit = totalRetailValue - totalCostValue
        });
    }

    /// <summary>
    /// Adjust stock quantity for multiple items
    /// </summary>
    [HttpPost("bulk-adjust")]
    public async Task<IActionResult> BulkAdjustStock([FromBody] BulkStockAdjustRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var results = new List<object>();
        foreach (var adjustment in request.Adjustments)
        {
            var item = await _context.Set<InventoryItem>()
                .FirstOrDefaultAsync(i => i.Id == adjustment.ItemId && i.TenantId == tenantId);

            if (item == null) continue;

            var newQuantity = item.QuantityOnHand + adjustment.QuantityChange;
            if (newQuantity < 0) continue; // Skip items with insufficient stock

            item.QuantityOnHand = newQuantity;
            item.UpdatedAt = DateTime.UtcNow;

            var transaction = new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                InventoryItemId = item.Id,
                Type = adjustment.Type,
                Quantity = adjustment.QuantityChange,
                QuantityAfter = newQuantity,
                Notes = adjustment.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<InventoryTransaction>().Add(transaction);
            results.Add(new { item.Id, item.Name, newQuantity });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk adjusted stock for {Count} items in tenant {TenantId}", results.Count, tenantId);

        return Ok(new { adjustedCount = results.Count, results });
    }

    /// <summary>
    /// Send low stock alert notifications for a batch of items
    /// </summary>
    [HttpPost("alerts/send")]
    public async Task<IActionResult> SendBatchAlerts([FromBody] List<Guid> itemIds)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var items = await _context.Set<InventoryItem>()
            .Where(i => itemIds.Contains(i.Id) && i.TenantId == tenantId && i.QuantityOnHand <= i.ReorderLevel)
            .ToListAsync();

        if (!items.Any()) return BadRequest("No valid low-stock items found for alerts");

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.LastAlertSentAt = now;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Sent batch alerts for {Count} items in tenant {TenantId}", items.Count, tenantId);

        return Ok(new { alertedCount = items.Count, message = $"Alerts marked as sent for {items.Count} items" });
    }
}

// DTOs
public record CreateInventoryItemRequest(
    string Name,
    string? Sku = null,
    string? Description = null,
    string? Category = null,
    decimal CostPrice = 0,
    decimal? SalePrice = null,
    int InitialQuantity = 0,
    int ReorderLevel = 5,
    int? ReorderQuantity = null,
    string? Supplier = null,
    bool IsRetail = false
);

public record UpdateInventoryItemRequest(
    string? Name = null,
    string? Sku = null,
    string? Description = null,
    string? Category = null,
    decimal? CostPrice = null,
    decimal? SalePrice = null,
    int? ReorderLevel = null,
    string? Supplier = null,
    bool? IsRetail = null,
    bool? IsActive = null
);

public record InventoryStockAdjustRequest(
    int QuantityChange,
    InventoryTransactionType Type = InventoryTransactionType.Adjustment,
    string? Notes = null,
    string? ReferenceType = null,
    Guid? ReferenceId = null
);

public record BulkStockAdjustRequest(List<SingleStockAdjust> Adjustments);
public record SingleStockAdjust(
    Guid ItemId,
    int QuantityChange,
    InventoryTransactionType Type = InventoryTransactionType.Adjustment,
    string? Notes = null
);

