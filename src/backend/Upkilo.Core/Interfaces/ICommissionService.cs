using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces
{
    public interface ICommissionService
    {
        Task<StaffCommission> CalculateCommissionAsync(Guid tenantId, Guid staffId, Guid bookingId, decimal amount);
        Task<IEnumerable<StaffCommission>> GetStaffEarningsAsync(Guid staffId, DateTime? from = null, DateTime? to = null);
        Task<decimal> GetTotalUnpaidCommissionsAsync(Guid tenantId);
        Task ApproveCommissionsAsync(List<Guid> commissionIds);
        Task<CommissionSummary> GetCommissionStatsAsync(Guid tenantId, DateTime start, DateTime end);
    }

    public class CommissionSummary
    {
        public decimal TotalCommissions { get; set; }
        public decimal TotalTips { get; set; }
        public int CalculationCount { get; set; }
        public Dictionary<Guid, decimal> EarningsByStaff { get; set; } = new();
    }
}
