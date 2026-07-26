namespace Upkilo.API.Controllers;

/// <summary>
/// Request DTOs for billing-related operations
/// </summary>
public class BillingCreateCheckoutRequest
{
    public string PlanId { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
}

public class ApplyPromoCodeRequest
{
    public string Code { get; set; } = string.Empty;
}

public class InvoiceSettingsRequest
{
    public string Prefix { get; set; } = string.Empty;
    public int NextNumber { get; set; } = 1;
    public string? CompanyName { get; set; }
    public string? CompanyAddress { get; set; }
    public string? TaxId { get; set; }
}
