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
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ProductsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] bool onlyActive = true)
    {
        var query = _context.Products.AsQueryable();
        if (onlyActive) query = query.Where(p => p.IsActive);

        var products = await query.OrderBy(p => p.Name).ToListAsync();
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var product = new Product
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Sku = request.Sku,
            Barcode = request.Barcode,
            RequiresShipping = request.RequiresShipping,
            Weight = request.Weight,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
    }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public bool RequiresShipping { get; set; }
    public decimal? Weight { get; set; }
}
