using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// Per-tenant resource quotas for API rate limits, storage, AI tokens, and feature limits.
/// Enforced by the SubscriptionEnforcerMiddleware.
/// </summary>
public class TenantQuota : TenantEntity
{
    public int MaxApiRequestsPerMinute { get; set; } = 60;
    public int MaxStorageMb { get; set; } = 500;
    public int MaxStaffMembers { get; set; } = 5;
    public int MaxClients { get; set; } = 500;
    public int MaxMonthlyEmails { get; set; } = 1000;
    public int MaxMonthlySms { get; set; } = 100;
    public int MaxMonthlyAiTokens { get; set; } = 50000;
    public decimal MaxMonthlyAiBudget { get; set; } = 10.00m;
    public int MaxBookingsPerDay { get; set; } = 50;
    public int MaxWorkflows { get; set; } = 10;
    public int CurrentStorageUsedMb { get; set; }
    public int CurrentMonthlyEmails { get; set; }
    public int CurrentMonthlySms { get; set; }
    public int CurrentMonthlyAiTokens { get; set; }
    public DateTime QuotaResetDate { get; set; } = DateTime.UtcNow.AddMonths(1);
}
