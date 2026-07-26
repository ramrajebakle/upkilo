using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class MembershipService : IMembershipService
{
    private readonly AppDbContext _context;
    private readonly ILogger<MembershipService> _logger;

    public MembershipService(AppDbContext context, ILogger<MembershipService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Plan Management

    public async Task<MembershipPlan> CreatePlanAsync(Guid tenantId, MembershipPlan plan)
    {
        plan.TenantId = tenantId;
        _context.MembershipPlans.Add(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<MembershipPlan?> GetPlanAsync(Guid planId, Guid tenantId)
    {
        return await _context.MembershipPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.TenantId == tenantId);
    }

    public async Task<IEnumerable<MembershipPlan>> GetPlansAsync(Guid tenantId)
    {
        var plans = await _context.MembershipPlans
            .Where(p => p.TenantId == tenantId)
            .ToListAsync();
        return plans.OrderBy(p => p.Price);
    }

    public async Task<bool> UpdatePlanAsync(Guid planId, Guid tenantId, MembershipPlan updatedPlan)
    {
        var plan = await GetPlanAsync(planId, tenantId);
        if (plan == null) return false;

        plan.Name = updatedPlan.Name;
        plan.Description = updatedPlan.Description;
        plan.Price = updatedPlan.Price;
        plan.BillingInterval = updatedPlan.BillingInterval;
        plan.ServicesIncluded = updatedPlan.ServicesIncluded;
        plan.DiscountPercent = updatedPlan.DiscountPercent;
        plan.FeaturesJson = updatedPlan.FeaturesJson;
        plan.IsActive = updatedPlan.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePlanAsync(Guid planId, Guid tenantId)
    {
        var plan = await GetPlanAsync(planId, tenantId);
        if (plan == null) return false;

        // Check if there are active subscriptions
        var hasActiveSubs = await _context.ClientMemberships
            .AnyAsync(s => s.MembershipPlanId == planId && s.Status == MembershipStatus.Active);

        if (hasActiveSubs)
        {
            _logger.LogWarning("Cannot delete plan {PlanId} as it has active subscriptions", planId);
            return false;
        }

        _context.MembershipPlans.Remove(plan);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Subscription Management

    public async Task<ClientMembership> SubscribeClientAsync(Guid tenantId, Guid clientId, Guid planId)
    {
        var plan = await GetPlanAsync(planId, tenantId);
        if (plan == null) throw new KeyNotFoundException("Membership plan not found.");

        var subscription = new ClientMembership
        {
            TenantId = tenantId,
            ClientId = clientId,
            MembershipPlanId = planId,
            Status = MembershipStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(plan.BillingInterval == "yearly" ? 12 : 1),
            ServicesUsedThisPeriod = 0
        };

        _context.ClientMemberships.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {ClientId} subscribed to plan {PlanId} in tenant {TenantId}", clientId, planId, tenantId);
        return subscription;
    }

    public async Task<ClientMembership?> GetSubscriptionAsync(Guid id, Guid tenantId)
    {
        return await _context.ClientMemberships
            .Include(s => s.Client)
            .Include(s => s.MembershipPlan)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
    }

    public async Task<IEnumerable<ClientMembership>> GetSubscriptionsAsync(Guid tenantId, string? status = null)
    {
        var query = _context.ClientMemberships
            .Include(s => s.Client)
            .Include(s => s.MembershipPlan)
            .Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MembershipStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(s => s.Status == parsedStatus);
        }

        return await query.OrderByDescending(s => s.StartDate).ToListAsync();
    }

    public async Task<bool> CancelSubscriptionAsync(Guid id, Guid tenantId, bool immediately)
    {
        var sub = await GetSubscriptionAsync(id, tenantId);
        if (sub == null) return false;

        if (immediately)
        {
            sub.Status = MembershipStatus.Cancelled;
            sub.EndDate = DateTime.UtcNow;
        }
        else
        {
            // Set to end at current period
            sub.EndDate = sub.NextBillingDate;
            // We might need a "PendingCancellation" status or just use EndDate to handle it
            sub.Status = MembershipStatus.Cancelled; 
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PauseSubscriptionAsync(Guid id, Guid tenantId, DateTime? resumeDate)
    {
        var sub = await GetSubscriptionAsync(id, tenantId);
        if (sub == null) return false;

        sub.Status = MembershipStatus.Paused;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResumeSubscriptionAsync(Guid id, Guid tenantId)
    {
        var sub = await GetSubscriptionAsync(id, tenantId);
        if (sub == null) return false;

        sub.Status = MembershipStatus.Active;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RecordUsageAsync(Guid id, Guid tenantId, Guid serviceId)
    {
        var sub = await _context.ClientMemberships
            .Include(s => s.MembershipPlan)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

        if (sub == null || sub.Status != MembershipStatus.Active) return false;

        var plan = sub.MembershipPlan;
        
        // If not unlimited (-1) and already at limit
        if (plan.ServicesIncluded != -1 && sub.ServicesUsedThisPeriod >= plan.ServicesIncluded)
        {
            _logger.LogWarning("Subscription {SubId} has reached its service limit for this period", id);
            return false;
        }

        sub.ServicesUsedThisPeriod++;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    public async Task<object> GetAnalyticsAsync(Guid tenantId)
    {
        var subs = await _context.ClientMemberships
            .Include(s => s.MembershipPlan)
            .Where(s => s.TenantId == tenantId)
            .ToListAsync();

        var totalMRR = subs.Where(s => s.Status == MembershipStatus.Active && s.MembershipPlan.BillingInterval == "monthly")
            .Sum(s => s.MembershipPlan.Price);

        return new
        {
            totalSubscribers = subs.Count,
            activeSubscribers = subs.Count(s => s.Status == MembershipStatus.Active),
            monthlyRecurringRevenue = totalMRR,
            byPlan = subs.GroupBy(s => s.MembershipPlan.Name)
                .Select(g => new
                {
                    planName = g.Key,
                    subscribers = g.Count(),
                    mrr = g.Where(s => s.Status == MembershipStatus.Active && s.MembershipPlan.BillingInterval == "monthly").Sum(s => s.MembershipPlan.Price)
                })
        };
    }
}
