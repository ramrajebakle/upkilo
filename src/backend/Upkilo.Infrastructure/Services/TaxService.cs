using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class TaxService : ITaxService
{
    private readonly AppDbContext _context;

    public TaxService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaxRate>> GetTaxRatesAsync(Guid tenantId, bool onlyActive = true)
    {
        var query = _context.TaxRates.Where(t => t.TenantId == tenantId);
        if (onlyActive)
        {
            query = query.Where(t => t.IsActive);
        }
        return await query.ToListAsync();
    }

    public async Task<TaxRate?> GetTaxRateByIdAsync(Guid tenantId, Guid id)
    {
        return await _context.TaxRates.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
    }

    public async Task<TaxRate> CreateTaxRateAsync(Guid tenantId, TaxRate taxRate)
    {
        taxRate.TenantId = tenantId;
        
        if (taxRate.IsDefault)
        {
            await ClearDefaultTaxRateAsync(tenantId);
        }

        _context.TaxRates.Add(taxRate);
        await _context.SaveChangesAsync();
        return taxRate;
    }

    public async Task<TaxRate> UpdateTaxRateAsync(Guid tenantId, TaxRate taxRate)
    {
        if (taxRate.IsDefault)
        {
            await ClearDefaultTaxRateAsync(tenantId);
        }

        _context.TaxRates.Update(taxRate);
        await _context.SaveChangesAsync();
        return taxRate;
    }

    public async Task<bool> DeleteTaxRateAsync(Guid tenantId, Guid id)
    {
        var taxRate = await GetTaxRateByIdAsync(tenantId, id);
        if (taxRate == null) return false;

        _context.TaxRates.Remove(taxRate);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TaxRate?> GetDefaultTaxRateAsync(Guid tenantId)
    {
        return await _context.TaxRates.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.IsDefault && t.IsActive);
    }

    private async Task ClearDefaultTaxRateAsync(Guid tenantId)
    {
        var existingDefault = await _context.TaxRates.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.IsDefault);
        if (existingDefault != null)
        {
            existingDefault.IsDefault = false;
        }
    }
}
