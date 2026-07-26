using System;

namespace Upkilo.Core.Entities;

public class LeadCapture : TenantEntity
{
    public Guid? LandingPageId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Source { get; set; } // utm_source
    public string? Medium { get; set; } // utm_medium
    public string? Campaign { get; set; } // utm_campaign
    public string? AdPlatform { get; set; }
    public string? FormData { get; set; } // JSON of custom form fields
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Converted

    // Navigation
    public virtual LandingPage? LandingPageRef { get; set; }
}
