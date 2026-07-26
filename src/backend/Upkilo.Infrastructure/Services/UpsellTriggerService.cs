using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Contextual upsell trigger engine — 6 trigger points for revenue optimization.
/// P3: All DB count queries run in parallel via Task.WhenAll instead of sequentially.
/// </summary>
public class UpsellTriggerService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UpsellTriggerService> _logger;

    public UpsellTriggerService(AppDbContext context, ILogger<UpsellTriggerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<UpsellTrigger>> EvaluateTriggersAsync(Guid tenantId)
    {
        var triggers = new List<UpsellTrigger>();
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return triggers;

        var subscription = await _context.Subscriptions
            .Include(s => s.PricingPlan).ThenInclude(p => p!.FeatureMappings).ThenInclude(m => m.PricingFeature)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status != SubscriptionStatus.Cancelled);

        int GetLimit(string featureKey, int fallback)
        {
            var mapping = subscription?.PricingPlan?.FeatureMappings
                .FirstOrDefault(m => m.PricingFeature.Key == featureKey);
            return mapping?.NumericLimit ?? fallback;
        }

        // P3: Run all 5 count queries in parallel — was sequential, each awaited independently.
        // Task.WhenAll lets the DB connection pool service them concurrently.
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var (monthlyBookings, staffCount, locationCount, clientCount, smsCount) = await (
            _context.Bookings.CountAsync(b => b.TenantId == tenantId && b.CreatedAt >= thirtyDaysAgo),
            _context.StaffMembers.CountAsync(s => s.TenantId == tenantId),
            _context.Locations.CountAsync(l => l.TenantId == tenantId),
            _context.Clients.CountAsync(c => c.TenantId == tenantId),
            _context.CommunicationLogs.CountAsync(c => c.TenantId == tenantId && c.Type == CommunicationType.SMS && c.CreatedAt >= thirtyDaysAgo)
        ).WhenAll();

        // Trigger 1: Approaching booking limit
        var bookingLimit = GetLimit("max_bookings_per_month", 50);
        if (monthlyBookings >= bookingLimit * 0.8)
            triggers.Add(new UpsellTrigger("booking_limit", "You're approaching your monthly booking limit. Upgrade for unlimited bookings.", "High"));

        // Trigger 2: Staff limit reached
        var staffLimit = GetLimit("max_staff", 1);
        if (staffCount >= staffLimit)
            triggers.Add(new UpsellTrigger("staff_limit", $"You've reached your staff limit ({staffLimit}). Add more staff for $5/month each.", "High"));

        // Trigger 3: Location limit reached
        var locationLimit = GetLimit("max_locations", 1);
        if (locationCount >= locationLimit)
            triggers.Add(new UpsellTrigger("location_limit", "Need more locations? Add locations for $19/month each.", "Medium"));

        // Trigger 4: Trial ending within 3 days
        if (tenant.TrialEndsAt.HasValue && (tenant.TrialEndsAt.Value - DateTime.UtcNow).TotalDays <= 3)
            triggers.Add(new UpsellTrigger("trial_ending", "Your free trial ends soon! Upgrade now to keep all your data.", "Critical"));

        // Trigger 5: Growing beyond starter limits
        if (tenant.SubscriptionTier == SubscriptionTier.Starter || tenant.SubscriptionTier == SubscriptionTier.Free)
        {
            if (clientCount > 50)
                triggers.Add(new UpsellTrigger("growing_business", "Your business is growing! Professional tier includes CRM, analytics, and marketing tools.", "Medium"));
        }

        // Trigger 6: SMS usage high
        if (smsCount > 100)
            triggers.Add(new UpsellTrigger("sms_usage", "High SMS usage detected. Upgrade to get bundled SMS credits at a lower rate.", "Low"));

        return triggers;
    }
}

/// <summary>
/// Helpers to run a tuple of tasks concurrently and destructure the results.
/// </summary>
file static class TaskExtensions
{
    public static async Task<(T1, T2, T3, T4, T5)> WhenAll<T1, T2, T3, T4, T5>(
        this (Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5) tasks)
    {
        await Task.WhenAll(tasks.t1, tasks.t2, tasks.t3, tasks.t4, tasks.t5);
        return (tasks.t1.Result, tasks.t2.Result, tasks.t3.Result, tasks.t4.Result, tasks.t5.Result);
    }
}

public record UpsellTrigger(string Type, string Message, string Priority);
