using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

public class BusinessListing : TenantEntity
{
    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // /directory/hair-salon-nyc
    public string Category { get; set; } = string.Empty; // Hair Salon, Spa, Barber
    public string? Description { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; } = "US";
    public string? ZipCode { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public double PremiumScore { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsVerified { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? ServiceTags { get; set; } // Comma-separated: "haircut,color,blowout"
    public string? OperatingHours { get; set; } // JSON
    public bool IsActive { get; set; } = true;
}
