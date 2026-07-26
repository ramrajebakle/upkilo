namespace Upkilo.Core.Entities;

public class MarketingTemplate : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "marketing"; // marketing, transactional, onboarding
    public string Type { get; set; } = "email"; // email, sms
    public string? ThumbnailUrl { get; set; }
    public bool IsSystem { get; set; } = false;
}
