using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Helpers;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public enum PricingIssueSeverity
{
    /// <summary>Pricing is broken in a way customers can see — a plan cannot be bought.</summary>
    Critical,
    /// <summary>Pricing works but something is inconsistent and will mislead or surprise.</summary>
    Warning
}

public sealed record PricingIssue(PricingIssueSeverity Severity, string Code, string Message);

/// <summary>
/// Validates the published pricing catalogue.
///
/// Pricing fails silently by nature: if the price rows disappear, the API still returns 200 with
/// null amounts and the marketing page quietly renders "Contact us" on every plan. Nothing errors,
/// nothing alerts, and the site simply stops selling. These checks turn that class of failure into
/// something a health probe reports.
///
/// Invariants are asserted here rather than in the seeder so they hold no matter how the data got
/// there — seeded, hand-edited, or restored from a backup.
/// </summary>
public class PricingIntegrityService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// The single currency Upkilo bills subscriptions in. Tenant-to-customer payments are
    /// deliberately unconstrained — those settle through each tenant's own connected Stripe
    /// account, in that account's currency.
    /// </summary>
    public const string BillingCurrency = "USD";

    public PricingIntegrityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PricingIssue>> ValidateAsync(CancellationToken ct = default)
    {
        var issues = new List<PricingIssue>();

        var plans = await _context.PricingPlans
            .Include(p => p.Prices)
            .AsNoTracking()
            .ToListAsync(ct);

        if (plans.Count == 0)
        {
            issues.Add(new(PricingIssueSeverity.Critical, "no_plans",
                "No pricing plans exist. The pricing page has nothing to display."));
            return issues;
        }

        // A plan is "purchasable" if it is active, not the custom/enterprise tier, and is not free.
        // Free and custom plans legitimately carry no price rows, so excluding them keeps the
        // checks below from firing on correct data.
        var purchasable = plans
            .Where(p => p.IsActive && !p.IsCustom && p.Prices.Count > 0)
            .ToList();

        var activeSellable = plans.Where(p => p.IsActive && !p.IsCustom).ToList();

        if (purchasable.Count == 0)
        {
            issues.Add(new(PricingIssueSeverity.Critical, "no_priced_plans",
                $"{activeSellable.Count} active plan(s) exist but none carry a price row. Every card "
                + "on the pricing page will render as \"Contact us\"."));
            return issues;
        }

        foreach (var plan in purchasable)
        {
            var prices = plan.Prices.ToList();

            // 1. Currency must match the single billing currency.
            foreach (var offending in prices.Where(x =>
                         !string.Equals(x.CurrencyCode, BillingCurrency, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new(PricingIssueSeverity.Critical, "unexpected_currency",
                    $"Plan '{plan.Name}' has a {offending.CurrencyCode} price row. Upkilo bills only in "
                    + $"{BillingCurrency}; a second currency reintroduces the drift this was consolidated to avoid."));
            }

            // 2. Exactly one row per (currency, cycle). Duplicates make the price returned
            //    depend on row order, which is how the pricing page previously showed
            //    inconsistent amounts.
            var duplicates = prices
                .GroupBy(x => (Currency: x.CurrencyCode.ToUpperInvariant(), x.Cycle))
                .Where(g => g.Count() > 1);

            foreach (var dup in duplicates)
            {
                issues.Add(new(PricingIssueSeverity.Critical, "duplicate_price",
                    $"Plan '{plan.Name}' has {dup.Count()} {dup.Key.Currency} {dup.Key.Cycle} price rows. "
                    + "Which one is charged depends on row order."));
            }

            var monthly = prices.FirstOrDefault(x => x.Cycle == BillingCycle.Monthly);
            var annual = prices.FirstOrDefault(x => x.Cycle == BillingCycle.Annual);

            // 3. Both cycles must exist — the pricing page has a monthly/annual toggle, and a
            //    missing cycle renders a blank card on one side of it.
            if (monthly is null)
                issues.Add(new(PricingIssueSeverity.Critical, "missing_monthly",
                    $"Plan '{plan.Name}' has no monthly price. The monthly view will show nothing."));

            if (annual is null)
                issues.Add(new(PricingIssueSeverity.Critical, "missing_annual",
                    $"Plan '{plan.Name}' has no annual price. The annual toggle will show nothing."));

            // 4. Amounts must be positive. A zero on a paid plan reads as free.
            foreach (var price in prices.Where(x => x.Amount <= 0))
                issues.Add(new(PricingIssueSeverity.Critical, "non_positive_amount",
                    $"Plan '{plan.Name}' has a {price.Cycle} price of {price.Amount}. A paid plan "
                    + "priced at or below zero is purchasable for nothing."));

            // 5. Annual should beat twelve months of monthly. If it does not, the "save X%"
            //    framing on the pricing page is a lie — a digit slip here overcharges by 10x
            //    and nothing else in the system would notice.
            if (monthly is not null && annual is not null && monthly.Amount > 0)
            {
                var twelveMonths = monthly.Amount * 12;
                if (annual.Amount >= twelveMonths)
                    issues.Add(new(PricingIssueSeverity.Critical, "annual_not_discounted",
                        $"Plan '{plan.Name}' annual ({annual.Amount}) is not cheaper than 12x monthly "
                        + $"({twelveMonths}). Annual billing is advertised as a saving."));
                else if (annual.Amount < twelveMonths * 0.5m)
                    issues.Add(new(PricingIssueSeverity.Warning, "annual_discount_suspicious",
                        $"Plan '{plan.Name}' annual ({annual.Amount}) is more than 50% below 12x monthly "
                        + $"({twelveMonths}). Verify this is intended and not a missing digit."));
            }
        }

        // 6. Plan ordering should be unambiguous — two plans at the same monthly price make the
        //    upgrade path meaningless.
        var monthlyByPlan = purchasable
            .Select(p => new
            {
                p.Name,
                Amount = p.Prices.FirstOrDefault(x => x.Cycle == BillingCycle.Monthly)?.Amount
            })
            .Where(x => x.Amount is > 0)
            .ToList();

        foreach (var collision in monthlyByPlan.GroupBy(x => x.Amount).Where(g => g.Count() > 1))
        {
            issues.Add(new(PricingIssueSeverity.Warning, "duplicate_plan_price",
                $"Plans {string.Join(", ", collision.Select(x => $"'{x.Name}'"))} share the same monthly "
                + $"price ({collision.Key}). The upgrade path between them is not meaningful."));
        }

        // 7. Rows synced to Stripe are immutable there. Stripe Price objects cannot have their
        //    amount changed, so editing an amount locally after a sync leaves the site advertising
        //    one figure while Stripe charges another. Flagged as a warning because detecting the
        //    actual divergence needs a live Stripe call, which this check deliberately avoids.
        var syncedCount = purchasable.SelectMany(p => p.Prices).Count(x => !string.IsNullOrEmpty(x.StripePriceId));
        var unsyncedCount = purchasable.SelectMany(p => p.Prices).Count(x => string.IsNullOrEmpty(x.StripePriceId));
        if (syncedCount > 0 && unsyncedCount > 0)
        {
            issues.Add(new(PricingIssueSeverity.Warning, "partial_stripe_sync",
                $"{syncedCount} price row(s) are linked to Stripe and {unsyncedCount} are not. "
                + "Checkout will fail for the unlinked ones — run the Stripe sync."));
        }

        return issues;
    }
}
