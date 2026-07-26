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
public class DealsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public DealsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetDeals([FromQuery] Guid? pipelineId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.Deals.Include(d => d.Client).AsQueryable();

        if (pipelineId.HasValue)
            query = query.Where(d => d.PipelineId == pipelineId.Value);

        var deals = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return Ok(deals);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeal([FromBody] CreateDealRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var deal = new Deal
        {
            TenantId = tenantId.Value,
            Title = request.Title,
            Value = request.Value,
            PipelineId = request.PipelineId,
            StageId = request.StageId,
            ClientId = request.ClientId,
            AssignedToId = request.AssignedToId,
            ExpectedCloseDate = request.ExpectedCloseDate,
            Status = DealStatus.Open
        };

        _context.Deals.Add(deal);
        await _context.SaveChangesAsync();
        
        // In a real scenario, this would publish a DealCreated Domain Event
        return CreatedAtAction(nameof(GetDeals), new { id = deal.Id }, deal);
    }

    [HttpPut("{id}/stage")]
    public async Task<IActionResult> UpdateDealStage(Guid id, [FromBody] UpdateDealStageRequest request)
    {
        var deal = await _context.Deals.FindAsync(id);
        if (deal == null) return NotFound();

        deal.StageId = request.StageId;
        
        if (request.Status != null)
            deal.Status = Enum.Parse<DealStatus>(request.Status, true);

        await _context.SaveChangesAsync();
        // Domain event: DealStageChanged (useful for workflow triggers)
        
        return Ok(deal);
    }
}

public class CreateDealRequest
{
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public Guid PipelineId { get; set; }
    public Guid StageId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? AssignedToId { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
}

public class UpdateDealStageRequest
{
    public Guid StageId { get; set; }
    public string? Status { get; set; } // Won, Lost, Open
}
