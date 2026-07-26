using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

/// <summary>
/// Controller for managing tax rates
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class TaxRatesController : ControllerBase
{
    private readonly ITaxService _taxService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TaxRatesController> _logger;

    public TaxRatesController(
        ITaxService taxService,
        ITenantProvider tenantProvider,
        ILogger<TaxRatesController> logger)
    {
        _taxService = taxService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() ?? Guid.Empty;

    /// <summary>
    /// Get all tax rates for the tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTaxRates([FromQuery] bool onlyActive = true)
    {
        var tenantId = GetTenantId();
        var taxRates = await _taxService.GetTaxRatesAsync(tenantId, onlyActive);
        return Ok(taxRates);
    }

    /// <summary>
    /// Get tax rate details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaxRate(Guid id)
    {
        var tenantId = GetTenantId();
        var taxRate = await _taxService.GetTaxRateByIdAsync(tenantId, id);
        if (taxRate == null) return NotFound();
        return Ok(taxRate);
    }

    /// <summary>
    /// Create a new tax rate
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTaxRate([FromBody] TaxRate taxRate)
    {
        var tenantId = GetTenantId();
        var created = await _taxService.CreateTaxRateAsync(tenantId, taxRate);
        _logger.LogInformation("Tax rate created: {TaxRateId} for tenant {TenantId}", created.Id, tenantId);
        return CreatedAtAction(nameof(GetTaxRate), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update an existing tax rate
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTaxRate(Guid id, [FromBody] TaxRate taxRate)
    {
        if (id != taxRate.Id) return BadRequest();
        
        var tenantId = GetTenantId();
        var existing = await _taxService.GetTaxRateByIdAsync(tenantId, id);
        if (existing == null) return NotFound();

        var updated = await _taxService.UpdateTaxRateAsync(tenantId, taxRate);
        _logger.LogInformation("Tax rate updated: {TaxRateId} for tenant {TenantId}", id, tenantId);
        return Ok(updated);
    }

    /// <summary>
    /// Delete a tax rate
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTaxRate(Guid id)
    {
        var tenantId = GetTenantId();
        var result = await _taxService.DeleteTaxRateAsync(tenantId, id);
        if (!result) return NotFound();
        
        _logger.LogInformation("Tax rate deleted: {TaxRateId} for tenant {TenantId}", id, tenantId);
        return NoContent();
    }
}

