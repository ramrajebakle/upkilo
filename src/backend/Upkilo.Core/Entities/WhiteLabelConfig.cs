using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// White-label configuration for agency/franchise tenants.
/// </summary>
public class WhiteLabelConfig : TenantEntity
{
    public string? CustomDomain { get; set; }
    public string? CustomLogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? CustomEmailDomain { get; set; }
    public bool RemovePoweredBy { get; set; }
    public string? CustomFavicon { get; set; }
    public string? CustomCss { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? DomainVerifiedAt { get; set; }
    // WL-12: persist email domain verification status
    public bool IsEmailVerified { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
}

/// <summary>
/// Multi-currency support configuration.
/// </summary>
public class CurrencyConfig : TenantEntity
{
    public string BaseCurrency { get; set; } = "USD";
    public string[] SupportedCurrencies { get; set; } = new[] { "USD" };
    public string DisplayFormat { get; set; } = "symbol"; // symbol, code
    public bool AutoConvert { get; set; }
}

/// <summary>
/// Service package/bundle for selling grouped services at a discount.
/// </summary>
public class ServicePackage : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal? OriginalPrice { get; set; } // Before discount
    public string ServiceIds { get; set; } = "[]"; // JSON array of service IDs
    public int SessionCount { get; set; } // Total sessions included
    public int SessionsUsed { get; set; }
    public int ValidityDays { get; set; } = 365;
    public bool IsActive { get; set; } = true;
}
