namespace Upkilo.Core.Entities;

public class EnterpriseLead : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? TeamSize { get; set; }
    public string? CurrentPlatform { get; set; }
    public string? UseCase { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Closed
}
