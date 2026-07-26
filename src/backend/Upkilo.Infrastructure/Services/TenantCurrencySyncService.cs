using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Helpers;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <param name="Changed">True when the tenant's currency was actually updated.</param>
/// <param name="Previous">Currency before the sync.</param>
/// <param name="Current">Currency after the sync.</param>
/// <param name="StalePriceCount">
/// Services still priced in <paramref name="Previous"/>. Non-zero means the tenant has prices that
/// no longer match the currency they settle in and should review them.
/// </param>
/// <param name="Reason">Why nothing changed, when <paramref name="Changed"/> is false.</param>
public sealed record CurrencySyncResult(
    bool Changed,
    string Previous,
    string Current,
    int StalePriceCount,
    string? Reason = null);

/// <summary>
/// Keeps a tenant's currency aligned with the Stripe account they settle through.
///
/// Connect accounts are created as Standard, so the tenant picks their country inside Stripe's
/// hosted onboarding — it is unknown at account-creation time. Nothing previously read it back,
/// which left every tenant on the "USD" entity default: a salon settling in rupees advertised
/// prices in dollars.
///
/// Stripe is treated as authoritative. The tenant is never asked to type a currency, because it is
/// a property of their connected account rather than a preference, and they can only get it wrong.
/// </summary>
public class TenantCurrencySyncService
{
    private readonly AppDbContext _context;
    private readonly IPaymentService _payments;
    private readonly ILogger<TenantCurrencySyncService> _logger;

    public TenantCurrencySyncService(
        AppDbContext context,
        IPaymentService payments,
        ILogger<TenantCurrencySyncService> logger)
    {
        _context = context;
        _payments = payments;
        _logger = logger;
    }

    /// <summary>
    /// Applies a Stripe account's settlement currency to a tenant.
    /// Used by the account.updated webhook, which already holds the account object.
    /// </summary>
    public async Task<CurrencySyncResult> ApplyAsync(
        Tenant tenant,
        string? accountCurrency,
        bool detailsSubmitted,
        CancellationToken ct = default)
    {
        var previous = tenant.Currency ?? Currency.Default;

        // An account mid-onboarding reports a placeholder that is not the tenant's real settlement
        // currency. Writing it swaps one wrong value for another.
        if (!detailsSubmitted)
            return new(false, previous, previous, 0, "onboarding_incomplete");

        if (string.IsNullOrWhiteSpace(accountCurrency))
            return new(false, previous, previous, 0, "account_has_no_currency");

        var resolved = Currency.Normalize(accountCurrency);
        if (string.Equals(resolved, previous, StringComparison.OrdinalIgnoreCase))
            return new(false, previous, previous, 0, "already_current");

        tenant.Currency = resolved;

        // Prices are never converted. The amounts are what the tenant chose to charge;
        // reinterpreting 500 from one currency to another silently rewrites their whole price
        // list. They are counted so the tenant can be prompted instead.
        var stale = await _context.Services
            .IgnoreQueryFilters()
            .CountAsync(s => s.TenantId == tenant.Id && s.Price > 0 && s.Currency == previous, ct);

        await _context.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Tenant {TenantId} currency {Previous} -> {Resolved} from connected Stripe account. "
            + "{StaleCount} service price(s) still in {Previous} and need review.",
            tenant.Id, previous, resolved, stale, previous);

        return new(true, previous, resolved, stale);
    }

    /// <summary>
    /// Fetches the tenant's connected account from Stripe and applies its currency.
    /// Used by the Connect return path and the backfill, which have no account object to hand.
    /// </summary>
    public async Task<CurrencySyncResult> SyncFromStripeAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant == null)
            return new(false, Currency.Default, Currency.Default, 0, "tenant_not_found");

        var previous = tenant.Currency ?? Currency.Default;

        if (string.IsNullOrEmpty(tenant.StripeConnectId))
            return new(false, previous, previous, 0, "no_connected_account");

        var account = await _payments.GetConnectAccountAsync(tenant.StripeConnectId, ct);
        if (account == null)
            return new(false, previous, previous, 0, "account_unavailable");

        return await ApplyAsync(tenant, account.DefaultCurrency, account.DetailsSubmitted, ct);
    }
}
