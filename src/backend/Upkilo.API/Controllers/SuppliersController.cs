using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Text.Json;

namespace Upkilo.API.Controllers;

/// <summary>
/// Suppliers controller for vendor management.
/// Uses real database queries against Suppliers and PurchaseOrders.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ILogger<SuppliersController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public SuppliersController(
        ILogger<SuppliersController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all suppliers with filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Suppliers
            .Where(s => s.TenantId == tenantId.Value && !s.IsDeleted);

        if (active.HasValue)
            query = query.Where(s => s.IsActive == active.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.Name.Contains(search) ||
                (s.ContactName != null && s.ContactName.Contains(search)) ||
                (s.Email != null && s.Email.Contains(search)));

        var total = await query.CountAsync();

        var suppliers = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.ContactName,
                s.Email,
                s.Phone,
                s.PaymentTerms,
                s.IsActive,
                orderCount = s.PurchaseOrders.Count(po => !po.IsDeleted),
                s.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = suppliers, total, page, pageSize });
    }

    /// <summary>
    /// Get supplier by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSupplier(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId.Value && !s.IsDeleted);

        if (supplier == null) return NotFound();

        return Ok(new
        {
            supplier.Id,
            supplier.Name,
            supplier.ContactName,
            supplier.Email,
            supplier.Phone,
            supplier.Address,
            supplier.Website,
            supplier.PaymentTerms,
            supplier.Notes,
            supplier.IsActive,
            supplier.CreatedAt,
            supplier.UpdatedAt
        });
    }

    /// <summary>
    /// Create supplier
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Supplier name is required." });

        var supplier = new Supplier
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            ContactName = request.ContactName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            Website = request.Website,
            PaymentTerms = request.PaymentTerms,
            Notes = request.Notes,
            IsActive = true
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Supplier created: {Id} - {Name}", supplier.Id, supplier.Name);

        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, new { supplier.Id, supplier.Name, supplier.CreatedAt });
    }

    /// <summary>
    /// Update supplier
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] UpdateSupplierRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId.Value && !s.IsDeleted);

        if (supplier == null) return NotFound();

        if (request.Name != null) supplier.Name = request.Name;
        if (request.ContactName != null) supplier.ContactName = request.ContactName;
        if (request.Email != null) supplier.Email = request.Email;
        if (request.Phone != null) supplier.Phone = request.Phone;
        if (request.Address != null) supplier.Address = request.Address;
        if (request.Website != null) supplier.Website = request.Website;
        if (request.PaymentTerms != null) supplier.PaymentTerms = request.PaymentTerms;
        if (request.Notes != null) supplier.Notes = request.Notes;
        if (request.IsActive.HasValue) supplier.IsActive = request.IsActive.Value;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, supplier.UpdatedAt });
    }

    /// <summary>
    /// Delete supplier (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSupplier(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId.Value && !s.IsDeleted);

        if (supplier == null) return NotFound();

        supplier.IsDeleted = true;
        supplier.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get purchase orders for a supplier
    /// </summary>
    [HttpGet("{supplierId}/orders")]
    public async Task<IActionResult> GetSupplierOrders(Guid supplierId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var orders = await _context.PurchaseOrders
            .Where(po => po.SupplierId == supplierId && po.TenantId == tenantId.Value && !po.IsDeleted)
            .OrderByDescending(po => po.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new
            {
                po.Id,
                po.OrderNumber,
                status = po.Status.ToString(),
                po.TotalAmount,
                po.SubmittedAt,
                po.ReceivedAt,
                po.ExpectedDeliveryDate,
                po.CreatedAt
            })
            .ToListAsync();

        var total = await _context.PurchaseOrders
            .CountAsync(po => po.SupplierId == supplierId && po.TenantId == tenantId.Value && !po.IsDeleted);

        return Ok(new { data = orders, total, page, pageSize });
    }
}

/// <summary>
/// Purchase orders controller for inventory ordering.
/// </summary>
[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly ILogger<PurchaseOrdersController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public PurchaseOrdersController(
        ILogger<PurchaseOrdersController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetPurchaseOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Where(po => po.TenantId == tenantId.Value && !po.IsDeleted);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PurchaseOrderStatus>(status, true, out var statusEnum))
            query = query.Where(po => po.Status == statusEnum);

        var total = await query.CountAsync();

        var orders = await query
            .OrderByDescending(po => po.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new
            {
                po.Id,
                po.OrderNumber,
                supplier = new { po.Supplier.Id, po.Supplier.Name },
                status = po.Status.ToString(),
                po.TotalAmount,
                po.SubmittedAt,
                po.ReceivedAt,
                po.ExpectedDeliveryDate,
                po.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = orders, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPurchaseOrder(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var po = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (po == null) return NotFound();

        return Ok(new
        {
            po.Id,
            po.OrderNumber,
            supplier = new { po.Supplier.Id, po.Supplier.Name, po.Supplier.Email },
            status = po.Status.ToString(),
            po.TotalAmount,
            po.Notes,
            items = po.ItemsJson,
            po.SubmittedAt,
            po.ReceivedAt,
            po.ExpectedDeliveryDate,
            po.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.TenantId == tenantId.Value && !s.IsDeleted);

        if (supplier == null) return BadRequest(new { error = "Supplier not found." });

        var lastOrder = await _context.PurchaseOrders
            .Where(po => po.TenantId == tenantId.Value)
            .OrderByDescending(po => po.CreatedAt)
            .FirstOrDefaultAsync();

        var nextNumber = lastOrder != null
            ? int.TryParse(lastOrder.OrderNumber.Replace("PO-", ""), out var n) ? n + 1 : 1001
            : 1001;

        var po = new PurchaseOrder
        {
            TenantId = tenantId.Value,
            OrderNumber = $"PO-{nextNumber}",
            SupplierId = request.SupplierId,
            Status = PurchaseOrderStatus.Draft,
            Notes = request.Notes,
            TotalAmount = request.Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            ItemsJson = request.Items != null ? JsonSerializer.Serialize(request.Items) : null
        };

        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        _logger.LogInformation("PO created: {OrderNumber}", po.OrderNumber);

        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = po.Id }, new { po.Id, po.OrderNumber, po.CreatedAt });
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitPurchaseOrder(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var po = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (po == null) return NotFound();
        if (po.Status != PurchaseOrderStatus.Draft)
            return BadRequest(new { error = "Only draft orders can be submitted." });

        po.Status = PurchaseOrderStatus.Submitted;
        po.SubmittedAt = DateTime.UtcNow;
        po.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, status = po.Status.ToString() });
    }

    [HttpPost("{id}/receive")]
    public async Task<IActionResult> ReceivePurchaseOrder(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var po = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (po == null) return NotFound();
        if (po.Status == PurchaseOrderStatus.Cancelled || po.Status == PurchaseOrderStatus.Received)
            return BadRequest(new { error = "Cannot receive this order." });

        po.Status = PurchaseOrderStatus.Received;
        po.ReceivedAt = DateTime.UtcNow;
        po.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("PO {OrderNumber} marked as received", po.OrderNumber);
        return Ok(new { success = true, status = po.Status.ToString(), po.ReceivedAt });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelPurchaseOrder(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var po = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (po == null) return NotFound();
        if (po.Status == PurchaseOrderStatus.Received)
            return BadRequest(new { error = "Cannot cancel a received order." });

        po.Status = PurchaseOrderStatus.Cancelled;
        po.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePurchaseOrder(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var po = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (po == null) return NotFound();
        if (po.Status != PurchaseOrderStatus.Draft)
            return BadRequest(new { error = "Only draft orders can be deleted." });

        po.IsDeleted = true;
        po.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// Request DTOs
public class CreateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
}

public class UpdateSupplierRequest
{
    public string? Name { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }
    public string? Notes { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<PurchaseOrderItemDto>? Items { get; set; }
}

public class PurchaseOrderItemDto
{
    public Guid? ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

