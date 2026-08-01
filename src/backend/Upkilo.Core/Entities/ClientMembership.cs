namespace Upkilo.Core.Entities;

public enum MembershipStatus
{
    Active,
    Paused,
    Cancelled,
    PastDue,
    Expired
}

public class ClientMembership : TenantEntity
{
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public Guid MembershipPlanId { get; set; }
    public MembershipPlan MembershipPlan { get; set; } = null!;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Guid PlanId { get => MembershipPlanId; set => MembershipPlanId = value; } // Alias
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public virtual MembershipPlan Plan { get => MembershipPlan; set => MembershipPlan = value; } // Alias

    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime NextBillingDate { get; set; }
    public int ServicesUsedThisPeriod { get; set; }

    // Stripe Integration
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
}
