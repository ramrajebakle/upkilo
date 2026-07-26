using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Manages subscription plan versioning for grandfathering, freemium, and annual pricing
/// </summary>
public class SubscriptionPlanVersioningService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubscriptionPlanVersioningService> _logger;

    public SubscriptionPlanVersioningService(AppDbContext context, ILogger<SubscriptionPlanVersioningService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculates annual pricing with 17% discount
    /// </summary>
    public decimal CalculateAnnualPrice(decimal monthlyPrice)
    {
        const decimal annualDiscountPercent = 17m;
        var annualTotal = monthlyPrice * 12;
        var discount = annualTotal * (annualDiscountPercent / 100m);
        return Math.Round(annualTotal - discount, 2);
    }

    /// <summary>
    /// Calculates usage-based billing charges for SMS and AI credits
    /// </summary>
    public async Task<UsageBillingSummary> CalculateUsageBillingAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd)
    {
        var summary = new UsageBillingSummary { TenantId = tenantId, PeriodStart = periodStart, PeriodEnd = periodEnd };

        // SMS usage — $0.015 per SMS (filter by CommunicationType.SMS enum)
        var smsCount = await _context.CommunicationLogs
            .Where(c => c.TenantId == tenantId && c.Type == CommunicationType.SMS && c.CreatedAt >= periodStart && c.CreatedAt <= periodEnd)
            .CountAsync();
        summary.SmsCount = smsCount;
        summary.SmsCost = smsCount * 0.015m;

        // AI usage — actual cost from AIUsageLogs
        var aiCost = await _context.AIUsageLogs
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= periodStart && a.CreatedAt <= periodEnd)
            .SumAsync(a => a.Cost);
        summary.AiCost = aiCost;

        summary.TotalUsageCost = summary.SmsCost + summary.AiCost;
        return summary;
    }
}

public class UsageBillingSummary
{
    public Guid TenantId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int SmsCount { get; set; }
    public decimal SmsCost { get; set; }
    public decimal AiCost { get; set; }
    public decimal TotalUsageCost { get; set; }
}
