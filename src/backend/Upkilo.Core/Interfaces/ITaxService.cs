using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ITaxService
{
    Task<IEnumerable<TaxRate>> GetTaxRatesAsync(Guid tenantId, bool onlyActive = true);
    Task<TaxRate?> GetTaxRateByIdAsync(Guid tenantId, Guid id);
    Task<TaxRate> CreateTaxRateAsync(Guid tenantId, TaxRate taxRate);
    Task<TaxRate> UpdateTaxRateAsync(Guid tenantId, TaxRate taxRate);
    Task<bool> DeleteTaxRateAsync(Guid tenantId, Guid id);
    Task<TaxRate?> GetDefaultTaxRateAsync(Guid tenantId);
}
