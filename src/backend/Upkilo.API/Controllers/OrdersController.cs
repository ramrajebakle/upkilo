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
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public OrdersController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var orders = await _context.Invoices // Assuming Orders are tracked via the generic Invoice entity or an Order entity
            .Where(o => o.Type == "ProductOrder")
            .Include(o => o.Items)
            .OrderByDescending(o => o.IssueDate)
            .ToListAsync();
            
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var order = new Invoice
        {
            TenantId = tenantId.Value,
            ClientId = request.ClientId,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
            Status = InvoiceStatus.Draft,
            Type = "ProductOrder",
            TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice)
        };

        foreach(var item in request.Items)
        {
            order.Items.Add(new InvoiceItem
            {
                TenantId = tenantId.Value,
                Description = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Total = item.Quantity * item.UnitPrice
            });
        }

        _context.Invoices.Add(order);
        await _context.SaveChangesAsync();
        
        // Next step: initiate payment via Stripe, raise OrderCreated domain event
        return Ok(order);
    }
}

public class CreateOrderRequest
{
    public Guid ClientId { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
