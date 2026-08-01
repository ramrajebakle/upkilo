using Upkilo.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Stripe payment service implementation
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly AppDbContext _context;
    private readonly ISecretProvider _secretProvider;
    private readonly string? _apiKey;
    private readonly bool _isConfigured;

    public PaymentService(
        IConfiguration configuration,
        ILogger<PaymentService> logger,
        AppDbContext context,
        ISecretProvider secretProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _secretProvider = secretProvider;

        _apiKey = _secretProvider.GetSecret("Stripe--SecretKey");
        _isConfigured = !string.IsNullOrEmpty(_apiKey);

        if (_isConfigured)
        {
            // H-4 FIX: Do NOT set the global static StripeConfiguration.ApiKey.
            // It is process-wide and not thread-safe for multi-tenant scenarios.
            // Instead, pass the API key per-request via RequestOptions.
            _logger.LogInformation("Stripe payment service initialized");
        }
        else
        {
            _logger.LogWarning("Stripe not configured - payments disabled");
        }
    }

    private async Task<RequestOptions> GetRequestOptionsAsync(Guid tenantId, string? idempotencyKey = null)
    {
        var options = new RequestOptions { ApiKey = _apiKey };
        if (!string.IsNullOrEmpty(idempotencyKey)) options.IdempotencyKey = idempotencyKey;

        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant != null && !string.IsNullOrEmpty(tenant.StripeConnectId))
        {
            options.StripeAccount = tenant.StripeConnectId;
        }
        return options;
    }

    public async Task<CheckoutResult> CreateCheckoutSessionAsync(CreateCheckoutRequest request)
    {
        if (!_isConfigured)
        {
            return new CheckoutResult(false, null, null, "Stripe not configured");
        }

        try
        {
            var customerId = await EnsureCustomerAsync(request.TenantId, "", "");

            var options = new SessionCreateOptions
            {
                Customer = customerId,
                Mode = "subscription",
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = request.PriceId,
                        Quantity = 1,
                    }
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = 14,
                },
                Metadata = new Dictionary<string, string>
                {
                    { "tenant_id", request.TenantId.ToString() }
                }
            };

            if (!string.IsNullOrEmpty(request.PromotionCode))
            {
                options.AllowPromotionCodes = true;
            }

            var service = new Stripe.Checkout.SessionService();
            var policy = ResiliencePolicies.GetGenericRetryPolicy();
            // H-7 FIX: Include PriceId in idempotency key so different checkouts on the same day don't collide
            var reqOptions = await GetRequestOptionsAsync(request.TenantId, $"checkout_{request.TenantId}_{request.PriceId}_{DateTime.UtcNow:yyyyMMddHH}");
            var session = await policy.ExecuteAsync(async (ct) =>
                await service.CreateAsync(options, reqOptions)
            );

            _logger.LogInformation(
                "Checkout session created: {SessionId} for tenant {TenantId}",
                session.Id, request.TenantId);

            return new CheckoutResult(true, session.Id, session.Url, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe checkout error for tenant {TenantId}", request.TenantId);
            return new CheckoutResult(false, null, null, ex.Message);
        }
    }

    public async Task<string> CreateBillingPortalSessionAsync(Guid tenantId, string returnUrl)
    {
        if (!_isConfigured)
        {
            throw new InvalidOperationException("Stripe not configured");
        }

        var customerId = await EnsureCustomerAsync(tenantId, "", "");

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = returnUrl,
        };

        var service = new Stripe.BillingPortal.SessionService();
        var reqOptions = await GetRequestOptionsAsync(tenantId);
        var session = await service.CreateAsync(options, reqOptions);

        return session.Url;
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentRequest request)
    {
        if (!_isConfigured)
        {
            return new PaymentIntentResult(false, null, null, null, "Stripe not configured");
        }

        try
        {
            var customerId = await EnsureCustomerAsync(request.TenantId, "", "");

            var options = new PaymentIntentCreateOptions
            {
                // Scaled by the currency exponent, not a flat *100. This is the main customer
                // payment path, and it is genuinely multi-currency: the amount settles through the
                // tenant's own connected Stripe account, whose currency follows that account's
                // country. A JPY intent built with *100 charges 100x.
                Amount = Upkilo.Core.Helpers.Currency.ToMinorUnits(request.Amount, request.Currency),
                Currency = Upkilo.Core.Helpers.Currency.Normalize(request.Currency).ToLowerInvariant(),
                Customer = customerId,
                Description = request.Description,
                CaptureMethod = request.CaptureImmediately ? "automatic" : "manual",
                Metadata = new Dictionary<string, string>
                {
                    { "tenant_id", request.TenantId.ToString() },
                    { "booking_id", request.BookingId.ToString() }
                }
            };

            var service = new PaymentIntentService();
            var policy = ResiliencePolicies.GetGenericRetryPolicy();
            var reqOptions = await GetRequestOptionsAsync(request.TenantId, $"pi_{request.BookingId}");
            var paymentIntent = await policy.ExecuteAsync(async (ct) =>
                await service.CreateAsync(options, reqOptions)
            );

            _logger.LogInformation(
                "Payment intent created: {PaymentIntentId} for booking {BookingId}",
                paymentIntent.Id, request.BookingId);

            return new PaymentIntentResult(
                true,
                paymentIntent.Id,
                paymentIntent.ClientSecret,
                paymentIntent.Status,
                null
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Payment intent creation failed for booking {BookingId}", request.BookingId);
            return new PaymentIntentResult(false, null, null, null, ex.Message);
        }
    }

    public async Task<bool> CapturePaymentAsync(string paymentIntentId, Guid tenantId)
    {
        if (!_isConfigured)
        {
            return false;
        }

        try
        {
            var service = new PaymentIntentService();
            // L-7 FIX: Use retry policy and idempotency key for captures
            var policy = ResiliencePolicies.GetGenericRetryPolicy();
            var reqOptions = await GetRequestOptionsAsync(tenantId, $"capture_{paymentIntentId}");
            await policy.ExecuteAsync(async (ct) =>
                await service.CaptureAsync(paymentIntentId, null, reqOptions)
            );

            _logger.LogInformation("Payment captured: {PaymentIntentId}", paymentIntentId);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Payment capture failed: {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(RefundRequest request, Guid tenantId)
    {
        if (!_isConfigured)
        {
            return new RefundResult(false, null, 0, "Stripe not configured");
        }

        try
        {
            var reqOptionsForGet = await GetRequestOptionsAsync(tenantId);
            // Verify ownership: retrieve PaymentIntent and check tenant_id metadata
            var piService = new PaymentIntentService();
            var paymentIntent = await piService.GetAsync(request.PaymentIntentId, null, reqOptionsForGet);

            var ownerTenantId = paymentIntent.Metadata?.GetValueOrDefault("tenant_id");
            if (ownerTenantId == null || !Guid.TryParse(ownerTenantId, out var parsedId) || parsedId != tenantId)
            {
                // Also check via Stripe customer → tenant mapping as fallback
                var tenant = await _context.Tenants.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant == null || tenant.StripeCustomerId != paymentIntent.CustomerId)
                {
                    _logger.LogWarning(
                        "SECURITY: Tenant {TenantId} attempted to refund PaymentIntent {PaymentIntentId} belonging to another tenant",
                        tenantId, request.PaymentIntentId);
                    return new RefundResult(false, null, 0, "Payment not found for this account");
                }
            }

            // Taken from the PaymentIntent already retrieved above for the ownership check, so a
            // partial refund is scaled by the same currency the charge was made in rather than an
            // assumed two-decimal one.
            var refundCurrency = paymentIntent.Currency;

            var options = new RefundCreateOptions
            {
                PaymentIntent = request.PaymentIntentId,
                Reason = request.Reason ?? "requested_by_customer"
            };

            if (request.Amount.HasValue)
            {
                // Partial refunds must use the same scaling as the original charge, or the
                // refunded amount is off by the currency exponent.
                options.Amount = Upkilo.Core.Helpers.Currency.ToMinorUnits(request.Amount.Value, refundCurrency);
            }

            var service = new RefundService();
            var policy = ResiliencePolicies.GetGenericRetryPolicy();
            // M-2 FIX: Use explicit 'full' string for null amounts to avoid collisions with Amount=0
            var amountKey = request.Amount.HasValue ? request.Amount.Value.ToString("F2") : "full";
            var reqOptions = await GetRequestOptionsAsync(tenantId, $"refund_{request.PaymentIntentId}_{amountKey}");
            var refund = await policy.ExecuteAsync(async (ct) =>
                await service.CreateAsync(options, reqOptions)
            );

            _logger.LogInformation(
                "Refund processed: {RefundId} for payment {PaymentIntentId}",
                refund.Id, request.PaymentIntentId);

            // Stripe returns the refund in minor units; the divisor depends on the currency.
            return new RefundResult(
                true,
                refund.Id,
                Upkilo.Core.Helpers.Currency.FromMinorUnits(refund.Amount, refund.Currency),
                null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Refund ownership check failed for payment {PaymentIntentId}", request.PaymentIntentId);
            return new RefundResult(false, null, 0, "Unable to process refund");
        }
    }

    public async Task<IEnumerable<PaymentMethodInfo>> GetPaymentMethodsAsync(Guid tenantId)
    {
        if (!_isConfigured)
        {
            return Enumerable.Empty<PaymentMethodInfo>();
        }

        var customerId = await EnsureCustomerAsync(tenantId, "", "");

        var options = new PaymentMethodListOptions
        {
            Customer = customerId,
            Type = "card",
        };

        var service = new PaymentMethodService();
        var reqOptions = await GetRequestOptionsAsync(tenantId);
        var paymentMethods = await service.ListAsync(options, reqOptions);

        // Get default payment method
        var customerService = new CustomerService();
        var customer = await customerService.GetAsync(customerId, null, reqOptions);
        var defaultPaymentMethodId = customer.InvoiceSettings?.DefaultPaymentMethodId;

        return paymentMethods.Data.Select(pm => new PaymentMethodInfo(
            pm.Id,
            pm.Type,
            pm.Card?.Last4,
            pm.Card?.Brand,
            (int?)pm.Card?.ExpMonth,
            (int?)pm.Card?.ExpYear,
            pm.Id == defaultPaymentMethodId
        ));
    }

    public async Task<bool> AttachPaymentMethodAsync(Guid tenantId, string paymentMethodId)
    {
        if (!_isConfigured)
        {
            return false;
        }

        try
        {
            var customerId = await EnsureCustomerAsync(tenantId, "", "");

            var service = new PaymentMethodService();
            var reqOptions = await GetRequestOptionsAsync(tenantId);
            await service.AttachAsync(paymentMethodId, new PaymentMethodAttachOptions
            {
                Customer = customerId
            }, reqOptions);

            _logger.LogInformation("Payment method attached: {PaymentMethodId} for tenant {TenantId}",
                paymentMethodId, tenantId);

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to attach payment method {PaymentMethodId}", paymentMethodId);
            return false;
        }
    }

    public async Task<string> EnsureCustomerAsync(Guid tenantId, string email, string name)
    {
        // C-1 FIX: Use SELECT ... FOR UPDATE to serialize concurrent access and prevent
        // duplicate Stripe customer creation (check-then-act race condition).
        Tenant? tenant;
        if (_context.Database.IsNpgsql())
        {
            tenant = await _context.Tenants
                .FromSqlRaw("SELECT * FROM \"Tenants\" WHERE \"Id\" = {0} FOR UPDATE", tenantId)
                .FirstOrDefaultAsync();
        }
        else
        {
            tenant = await _context.Tenants.FindAsync(tenantId);
        }
        if (tenant == null) throw new InvalidOperationException("Tenant not found");

        if (!string.IsNullOrEmpty(tenant.StripeCustomerId))
        {
            return tenant.StripeCustomerId;
        }

        if (!_isConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured. Cannot create real customers.");
        }

        var options = new CustomerCreateOptions
        {
            Email = !string.IsNullOrEmpty(email) ? email : tenant.Email,
            Name = !string.IsNullOrEmpty(name) ? name : tenant.Name,
            Metadata = new Dictionary<string, string>
            {
                { "tenant_id", tenantId.ToString() }
            }
        };

        var service = new CustomerService();
        var reqOptions = await GetRequestOptionsAsync(tenantId);
        var customer = await service.CreateAsync(options, reqOptions);

        tenant.StripeCustomerId = customer.Id;
        await _context.SaveChangesAsync();

        return customer.Id;
    }

    public async Task<string> CreateConnectAccountAsync(Guid tenantId, string email)
    {
        // C-3 FIX: Use SELECT ... FOR UPDATE to prevent duplicate Connect accounts.
        Tenant? tenant;
        if (_context.Database.IsNpgsql())
        {
            tenant = await _context.Tenants
                .FromSqlRaw("SELECT * FROM \"Tenants\" WHERE \"Id\" = {0} FOR UPDATE", tenantId)
                .FirstOrDefaultAsync();
        }
        else
        {
            tenant = await _context.Tenants.FindAsync(tenantId);
        }
        if (tenant == null) throw new InvalidOperationException("Tenant not found");

        if (!string.IsNullOrEmpty(tenant.StripeConnectId))
        {
            return tenant.StripeConnectId;
        }

        if (!_isConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured. Cannot create real Connect accounts.");
        }

        var options = new AccountCreateOptions
        {
            Type = "standard",
            Email = email,
            Metadata = new Dictionary<string, string>
            {
                { "tenant_id", tenantId.ToString() }
            }
        };

        var service = new AccountService();
        var account = await service.CreateAsync(options, new RequestOptions { ApiKey = _apiKey });

        tenant.StripeConnectId = account.Id;
        await _context.SaveChangesAsync();

        return account.Id;
    }

    public async Task<ConnectAccountInfo?> GetConnectAccountAsync(string connectId, CancellationToken ct = default)
    {
        if (!_isConfigured || string.IsNullOrWhiteSpace(connectId))
            return null;

        try
        {
            var account = await new AccountService()
                .GetAsync(connectId, requestOptions: new RequestOptions { ApiKey = _apiKey }, cancellationToken: ct);

            return new ConnectAccountInfo(
                account.Id,
                account.Country,
                account.DefaultCurrency,
                account.DetailsSubmitted,
                account.ChargesEnabled);
        }
        catch (StripeException ex)
        {
            // A tenant can revoke platform access, leaving a stored id that no longer resolves.
            // Returning null lets callers carry on with the currency they already have rather
            // than failing the request they were actually serving.
            _logger.LogWarning(ex, "Could not retrieve connected account {ConnectId}", connectId);
            return null;
        }
    }

    public async Task<string> CreateConnectOnboardingLinkAsync(string connectId, string refreshUrl, string returnUrl)
    {
        if (!_isConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured. Cannot create onboarding links.");
        }

        var options = new AccountLinkCreateOptions
        {
            Account = connectId,
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl,
            Type = "account_onboarding",
        };

        var service = new AccountLinkService();
        var accountLink = await service.CreateAsync(options, new RequestOptions { ApiKey = _apiKey });

        return accountLink.Url;
    }
}
