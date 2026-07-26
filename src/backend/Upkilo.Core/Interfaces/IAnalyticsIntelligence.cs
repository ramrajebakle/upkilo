namespace Upkilo.Core.Interfaces
{
    public interface IAnalyticsSyncService
    {
        Task SyncDataAsync();
        Task SyncIncrementalAsync(string tableName, DateTime lastSync);
    }

    public interface IFinancialProjectionService
    {
        Task<decimal> PredictRevenueAsync(Guid tenantId, int monthsAhead);
        Task<IEnumerable<ChurnRisk>> PredictChurnRiskAsync(Guid tenantId);
        Task<CashflowForecast> GetCashflowForecastAsync(Guid tenantId);
        Task<TaxReport> GenerateTaxReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    }

    public class ChurnRisk
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public double RiskScore { get; set; } // 0 to 1
        public string PrimaryReason { get; set; } = string.Empty;
    }

    public class CashflowForecast
    {
        public Guid TenantId { get; set; }
        public List<ForecastPoint> ForecastPoints { get; set; } = new();
    }

    public class ForecastPoint
    {
        public DateTime Date { get; set; }
        public decimal ProjectedRevenue { get; set; }
        public decimal ProjectedExpenses { get; set; }
        public double ConfidenceInterval { get; set; }
    }

    public class TaxReport
    {
        public decimal TotalRevenue { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxLiability { get; set; }
        public List<TaxBreakdown> Breakdown { get; set; } = new();
    }

    public class TaxBreakdown
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
    }
}
