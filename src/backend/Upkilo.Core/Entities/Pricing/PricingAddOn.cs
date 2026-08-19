namespace Upkilo.Core.Entities;

/// <summary>
/// A purchasable add-on sold alongside the plan tiers, so customers scale without being
/// forced into a higher tier.
///
/// This exists because the add-on prices previously lived nowhere: the marketing pricing page
/// listed six add-ons with a billing cadence and no amount, while the amount actually charged
/// lived only in a Stripe Price ID configured per environment. There was no number in the
/// repository to render, and nothing tying what we advertise to what Stripe bills.
///
/// This table is now the published-price source of truth, in the same role PlanPrice plays for
/// the tiers. It does NOT perform the charge — SubscriptionService still drives Stripe by Price
/// ID — so <see cref="Amount"/> here and the matching Stripe price MUST be kept in step. That
/// duplication is deliberate but load-bearing: it is exactly how Business once advertised $149
/// while the database charged $199.
/// </summary>
public class PricingAddOn : BaseEntity
{
    /// <summary>Stable identifier, e.g. "extra_staff". Referenced by code, never displayed.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name, e.g. "Extra Staff".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable billing cadence, e.g. "per seat / month".</summary>
    public string BillingUnit { get; set; } = string.Empty;

    /// <summary>
    /// Published price in <see cref="CurrencyCode"/>. Null means no published price — the page
    /// renders a sales pointer rather than inventing a number.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Upkilo bills exclusively in USD (PricingIntegrityService.BillingCurrency). Stored rather
    /// than assumed so an add-on can never render an amount without the currency it is in.
    /// </summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// False for add-ons that are announced but not yet purchasable. The pricing page rendered
    /// all six identically, so roadmap items read as buyable today.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Display order on the pricing page.</summary>
    public int SortOrder { get; set; }
}
