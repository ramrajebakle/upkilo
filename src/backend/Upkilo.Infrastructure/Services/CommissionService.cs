using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CommissionService> _logger;

        public CommissionService(AppDbContext context, ILogger<CommissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StaffCommission> CalculateCommissionAsync(Guid tenantId, Guid staffId, Guid bookingId, decimal amount)
        {
            _logger.LogInformation("Calculating commission for staff {StaffId}, booking {BookingId}", staffId, bookingId);

            var booking = await _context.Bookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new Exception("Booking not found");

            // Find best matching rule
            // 1. Staff specific + Service specific
            // 2. Staff specific + Category
            // 3. Staff specific (default)
            // 4. Service specific (global)
            // 5. Global default
            
            var rules = await _context.Set<CommissionRule>()
                .Where(r => r.TenantId == tenantId && r.IsActive)
                .Where(r => r.EffectiveFrom <= DateTime.UtcNow && (r.EffectiveUntil == null || r.EffectiveUntil >= DateTime.UtcNow))
                .OrderByDescending(r => r.Priority)
                .ToListAsync();

            var rule = rules.FirstOrDefault(r => r.StaffId == staffId && r.ServiceId == booking.ServiceId)
                    ?? rules.FirstOrDefault(r => r.StaffId == staffId && r.ServiceCategory == booking.Service?.Category)
                    ?? rules.FirstOrDefault(r => r.StaffId == staffId && r.ServiceId == null && r.ServiceCategory == null)
                    ?? rules.FirstOrDefault(r => r.StaffId == null && r.ServiceId == booking.ServiceId)
                    ?? rules.FirstOrDefault(r => r.StaffId == null && r.ServiceId == null && r.ServiceCategory == null);

            decimal rate = 0;
            decimal totalEarned = 0;
            CommissionType type = CommissionType.Percentage;

            if (rule != null)
            {
                rate = rule.Rate;
                type = rule.Type;
                if (rule.Type == CommissionType.Percentage)
                {
                    totalEarned = amount * (rate / 100);
                }
                else
                {
                    totalEarned = rate;
                }

                // Apply floor/ceiling
                if (rule.MinAmount.HasValue && totalEarned < rule.MinAmount.Value) totalEarned = rule.MinAmount.Value;
                if (rule.MaxAmount.HasValue && totalEarned > rule.MaxAmount.Value) totalEarned = rule.MaxAmount.Value;
            }
            else
            {
                // Fallback to staff base commission if rule doesn't exist
                var staff = await _context.Set<StaffMember>().FindAsync(staffId);
                if (staff != null)
                {
                    rate = staff.BaseCommissionRate;
                    type = staff.CommissionType;
                    if (type == CommissionType.Percentage)
                    {
                        totalEarned = amount * (rate / 100);
                    }
                    else
                    {
                        totalEarned = rate;
                    }
                }
            }

            var commission = new StaffCommission
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StaffId = staffId,
                BookingId = bookingId,
                BaseAmount = amount,
                CommissionRate = rate,
                TotalEarned = totalEarned,
                Status = CommissionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<StaffCommission>().Add(commission);
            await _context.SaveChangesAsync();

            return commission;
        }

        public async Task<IEnumerable<StaffCommission>> GetStaffEarningsAsync(Guid staffId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Set<StaffCommission>()
                .Where(c => c.StaffId == staffId);

            if (from.HasValue) query = query.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(c => c.CreatedAt <= to.Value);

            return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task<decimal> GetTotalUnpaidCommissionsAsync(Guid tenantId)
        {
            return await _context.Set<StaffCommission>()
                .Where(c => c.TenantId == tenantId && c.Status != CommissionStatus.Paid)
                .SumAsync(c => c.TotalEarned + c.TipAmount);
        }

        public async Task ApproveCommissionsAsync(List<Guid> commissionIds)
        {
            var commissions = await _context.Set<StaffCommission>()
                .Where(c => commissionIds.Contains(c.Id))
                .ToListAsync();

            foreach (var c in commissions)
            {
                if (c.Status == CommissionStatus.Pending)
                    c.Status = CommissionStatus.Approved;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<CommissionSummary> GetCommissionStatsAsync(Guid tenantId, DateTime start, DateTime end)
        {
            var commissions = await _context.Set<StaffCommission>()
                .Where(c => c.TenantId == tenantId && c.CreatedAt >= start && c.CreatedAt <= end)
                .ToListAsync();

            return new CommissionSummary
            {
                TotalCommissions = commissions.Sum(c => c.TotalEarned),
                TotalTips = commissions.Sum(c => c.TipAmount),
                CalculationCount = commissions.Count,
                EarningsByStaff = commissions.GroupBy(c => c.StaffId)
                    .ToDictionary(g => g.Key, g => g.Sum(c => c.TotalEarned + c.TipAmount))
            };
        }
    }
}
