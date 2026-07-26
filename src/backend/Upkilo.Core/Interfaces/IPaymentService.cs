namespace Upkilo.Core.Interfaces;

/// <summary>
/// Payment service interface for Stripe integration
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Create a Stripe checkout session for subscription
    /// </summary>
    Task<CheckoutResult> CreateCheckoutSessionAsync(CreateCheckoutRequest request);

    /// <summary>
    /// Create a billing portal session for subscription management
    /// </summary>
    Task<string> CreateBillingPortalSessionAsync(Guid tenantId, string returnUrl);

    /// <summary>
    /// Create a payment intent for one-time payment
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentRequest request);

    /// <summary>
    /// Capture a previously authorized payment
    /// </summary>
    Task<bool> CapturePaymentAsync(string paymentIntentId, Guid tenantId);



    /// <summary>
    /// Process a refund with tenant ownership verification (prevents cross-tenant IDOR)
    /// </summary>
    Task<RefundResult> RefundPaymentAsync(RefundRequest request, Guid tenantId);

    /// <summary>
    /// Get customer payment methods
    /// </summary>
    Task<IEnumerable<PaymentMethodInfo>> GetPaymentMethodsAsync(Guid tenantId);

    /// <summary>
    /// Attach a payment method to customer
    /// </summary>
    Task<bool> AttachPaymentMethodAsync(Guid tenantId, string paymentMethodId);

    /// <summary>
    /// Create or get Stripe customer for tenant
    /// </summary>
    Task<string> EnsureCustomerAsync(Guid tenantId, string email, string name);

    /// <summary>
    /// Create a Stripe Connect account for seller payouts
    /// </summary>
    Task<string> CreateConnectAccountAsync(Guid tenantId, string email);

    /// <summary>
    /// Get Stripe Connect account link for onboarding
    /// </summary>
    Task<string> CreateConnectOnboardingLinkAsync(string connectId, string refreshUrl, string returnUrl);

    /// <summary>
    /// Read a connected account's settlement details. Returns null if the account cannot be
    /// retrieved (unknown id, revoked access, Stripe unavailable).
    ///
    /// Returns a plain record rather than Stripe.Account so the provider stays out of Core.
    /// </summary>
    Task<ConnectAccountInfo?> GetConnectAccountAsync(string connectId, CancellationToken ct = default);
}

/// <param name="AccountId">The Stripe account id.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country the account is registered in.</param>
/// <param name="DefaultCurrency">
/// The currency the account settles in. Determined by the account's country and fixed at
/// onboarding — this is why a tenant's currency is read from Stripe rather than chosen.
/// </param>
/// <param name="DetailsSubmitted">
/// False while onboarding is incomplete, when the reported currency is a placeholder rather than
/// the account's real settlement currency.
/// </param>
/// <param name="ChargesEnabled">Whether the account can currently accept charges.</param>
public record ConnectAccountInfo(
    string AccountId,
    string? Country,
    string? DefaultCurrency,
    bool DetailsSubmitted,
    bool ChargesEnabled);

public record CreateCheckoutRequest(
    Guid TenantId,
    string PriceId,
    string SuccessUrl,
    string CancelUrl,
    bool IsAnnual = false,
    string? PromotionCode = null,
    string? CustomerId = null,
    string? SubscriptionId = null
);

public record CheckoutResult(
    bool Success,
    string? SessionId,
    string? SessionUrl,
    string? Error
);

public record CreatePaymentRequest(
    Guid TenantId,
    Guid BookingId,
    decimal Amount,
    string Currency,
    string Description,
    bool CaptureImmediately = true,
    // Optional Stripe Connect platform fee (in smallest currency unit, e.g. cents)
    long? ApplicationFeeAmount = null,
    string? StripeConnectId = null
);

public record PaymentIntentResult(
    bool Success,
    string? PaymentIntentId,
    string? ClientSecret,
    string? Status,
    string? Error
);

public record RefundRequest(
    string PaymentIntentId,
    decimal? Amount = null, // null = full refund
    string? Reason = null
);

public record RefundResult(
    bool Success,
    string? RefundId,
    decimal AmountRefunded,
    string? Error
);

public record PaymentMethodInfo(
    string Id,
    string Type,
    string? Last4,
    string? Brand,
    int? ExpMonth,
    int? ExpYear,
    bool IsDefault
);
