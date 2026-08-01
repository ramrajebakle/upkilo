using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IMembershipService
{
    // Plan Management
    Task<MembershipPlan> CreatePlanAsync(Guid tenantId, MembershipPlan plan);
    Task<MembershipPlan?> GetPlanAsync(Guid planId, Guid tenantId);
    Task<IEnumerable<MembershipPlan>> GetPlansAsync(Guid tenantId);
    Task<bool> UpdatePlanAsync(Guid planId, Guid tenantId, MembershipPlan updatedPlan);
    Task<bool> DeletePlanAsync(Guid planId, Guid tenantId);

    // Subscription Management
    Task<ClientMembership> SubscribeClientAsync(Guid tenantId, Guid clientId, Guid planId);
    Task<ClientMembership?> GetSubscriptionAsync(Guid id, Guid tenantId);
    Task<IEnumerable<ClientMembership>> GetSubscriptionsAsync(Guid tenantId, string? status = null);
    Task<bool> CancelSubscriptionAsync(Guid id, Guid tenantId, bool immediately);
    Task<bool> PauseSubscriptionAsync(Guid id, Guid tenantId, DateTime? resumeDate);
    Task<bool> ResumeSubscriptionAsync(Guid id, Guid tenantId);
    Task<bool> RecordUsageAsync(Guid id, Guid tenantId, Guid serviceId);

    // Analytics
    Task<object> GetAnalyticsAsync(Guid tenantId);
}
