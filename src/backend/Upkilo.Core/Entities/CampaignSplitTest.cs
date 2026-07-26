namespace Upkilo.Core.Entities;

public class CampaignSplitTest : TenantEntity
{
    public Guid CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public string TestName { get; set; } = string.Empty;
    public string VariantA_ConfigJson { get; set; } = "{}";
    public string VariantB_ConfigJson { get; set; } = "{}";
    
    public int MetricA_Opened { get; set; }
    public int MetricB_Opened { get; set; }
    public int MetricA_Clicked { get; set; }
    public int MetricB_Clicked { get; set; }

    public string Status { get; set; } = "Active"; // Active, Concluded
    public string? WinningVariant { get; set; }
    public DateTime? ConcludedAt { get; set; }
}
