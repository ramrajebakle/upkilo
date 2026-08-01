using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ITenantProvider tenantProvider,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    // SECURITY (L-1): Standardized tenant ID retrieval via ITenantProvider
    // instead of inline Guid.Parse which returned Guid.Empty on failure.
    private Guid? GetTenantId() => _tenantProvider.GetTenantId();

    /// <summary>
    /// Create a checkout session for subscription
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequestDto request)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        // SECURITY (H-2): Validate redirect URLs to prevent open redirect attacks
        if (!IsAllowedRedirectUrl(request.SuccessUrl) || !IsAllowedRedirectUrl(request.CancelUrl))
            return BadRequest(new { error = "Invalid redirect URL" });

        var result = await _paymentService.CreateCheckoutSessionAsync(new CreateCheckoutRequest(
            tenantId.Value,
            request.PriceId,
            request.SuccessUrl,
            request.CancelUrl,
            request.IsAnnual,
            request.PromotionCode,
            null,
            null
        ));

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { sessionId = result.SessionId, url = result.SessionUrl });
    }

    /// <summary>
    /// Create a billing portal session
    /// </summary>
    [HttpPost("portal")]
    public async Task<IActionResult> CreatePortal([FromBody] string returnUrl)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        // SECURITY (H-2): Validate return URL
        if (!IsAllowedRedirectUrl(returnUrl))
            return BadRequest(new { error = "Invalid return URL" });

        try
        {
            var url = await _paymentService.CreateBillingPortalSessionAsync(tenantId.Value, returnUrl);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            // SECURITY (M-6): Don't leak exception details to the client
            _logger.LogError(ex, "Failed to create billing portal session for tenant {TenantId}", tenantId);
            return BadRequest(new { error = "Failed to create billing portal session. Please try again." });
        }
    }

    /// <summary>
    /// Refund a payment
    /// </summary>
    [HttpPost("refund")]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Refund([FromBody] RefundRequestDto request)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        // SECURITY (C-3): Validate PaymentIntentId format
        if (string.IsNullOrWhiteSpace(request.PaymentIntentId) || !request.PaymentIntentId.StartsWith("pi_"))
            return BadRequest(new { error = "Invalid payment intent ID format" });

        // SECURITY (M-5): Validate refund amount
        if (request.Amount.HasValue && request.Amount.Value <= 0)
            return BadRequest(new { error = "Refund amount must be positive" });

        // SECURITY (C-3): Pass tenantId for ownership verification
        var result = await _paymentService.RefundPaymentAsync(new Upkilo.Core.Interfaces.RefundRequest(
            request.PaymentIntentId,
            request.Amount,
            request.Reason
        ), tenantId.Value);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        _logger.LogInformation(
            "Refund processed for tenant {TenantId}: PaymentIntent={PaymentIntentId}, Amount={Amount}",
            tenantId, request.PaymentIntentId, result.AmountRefunded);

        return Ok(new { success = true, refundId = result.RefundId, amount = result.AmountRefunded });
    }

    /// <summary>
    /// Create a Razorpay order for payment
    /// </summary>
    [HttpPost("razorpay/order")]
    [Authorize(Roles = "Owner,Admin")]  // SECURITY (C-4): Role-restricted
    public async Task<IActionResult> CreateRazorpayOrder([FromBody] RazorpayOrderRequest request)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        var _context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var booking = await _context.Bookings.Include(b => b.Tenant).FirstOrDefaultAsync(b => b.Id == request.BookingId && b.TenantId == tenantId);
        if (booking == null) return NotFound("Booking not found");

        if (!booking.Price.HasValue) return BadRequest(new { error = "Booking has no price" });
        decimal amount = booking.Price.Value;
        string currency = booking.Tenant?.Currency ?? "INR";

        // SECURITY (C-4): Validate amount bounds
        if (amount <= 0 || amount > 1_000_000)
            return BadRequest(new { error = "Amount must be between 0.01 and 1,000,000" });

        // SECURITY (C-4): Whitelist currencies
        var allowedCurrencies = new[] { "INR", "USD", "EUR", "GBP" };
        if (!allowedCurrencies.Contains(currency.ToUpper()))
            return BadRequest(new { error = "Unsupported currency" });

        // SECURITY (C-4): Validate receipt ID format
        if (string.IsNullOrWhiteSpace(request.ReceiptId) ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.ReceiptId, @"^[a-zA-Z0-9\-_]{1,40}$"))
            return BadRequest(new { error = "Invalid receipt ID format" });

        var razorpayService = HttpContext.RequestServices.GetRequiredService<RazorpayService>();
        var orderId = await razorpayService.CreateOrderAsync(
            amount, currency.ToUpper(), request.ReceiptId);

        if (orderId == null)
        {
            _logger.LogWarning("Razorpay order creation failed for tenant {TenantId}", tenantId);
            return BadRequest(new { error = "Failed to create payment order" });
        }

        _logger.LogInformation(
            "Razorpay order {OrderId} created for tenant {TenantId}, amount {Amount} {Currency}",
            orderId, tenantId, amount, currency);

        return Ok(new { orderId });
    }

    /// <summary>
    /// Verify and capture a Razorpay payment
    /// </summary>
    [HttpPost("razorpay/verify")]
    [Authorize(Roles = "Owner,Admin")]  // SECURITY (C-4): Role-restricted
    public async Task<IActionResult> VerifyRazorpayPayment([FromBody] RazorpayVerifyRequest request)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        // SECURITY (M-5): Validate required fields
        if (string.IsNullOrWhiteSpace(request.OrderId) ||
            string.IsNullOrWhiteSpace(request.PaymentId) ||
            string.IsNullOrWhiteSpace(request.Signature))
            return BadRequest(new { error = "OrderId, PaymentId, and Signature are required" });

        var _context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var booking = await _context.Bookings.Include(b => b.Tenant).FirstOrDefaultAsync(b => b.Id == request.BookingId && b.TenantId == tenantId);
        if (booking == null) return NotFound("Booking not found");

        if (!booking.Price.HasValue) return BadRequest(new { error = "Booking has no price" });
        decimal amount = booking.Price.Value;
        string currency = booking.Tenant?.Currency ?? "INR";

        var razorpayService = HttpContext.RequestServices.GetRequiredService<RazorpayService>();
        var isValid = razorpayService.VerifySignature(request.OrderId, request.PaymentId, request.Signature);

        if (!isValid)
        {
            _logger.LogWarning(
                "Invalid Razorpay signature for tenant {TenantId}, order {OrderId}",
                tenantId, request.OrderId);
            return BadRequest(new { error = "Payment verification failed" });
        }

        // Capture payment with server-calculated amount
        var captured = await razorpayService.CapturePaymentAsync(
            request.PaymentId, amount, currency);

        _logger.LogInformation(
            "Razorpay payment {PaymentId} verified and captured for tenant {TenantId}",
            request.PaymentId, tenantId);

        return Ok(new { success = captured });
    }

    /// <summary>
    /// GET /api/v1/payments/platform-fee — returns current platform fee settings.
    /// Enables tenants to opt into an optional processing fee (0.5-1%) that Upkilo collects via Stripe Connect.
    /// </summary>
    [HttpGet("platform-fee")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetPlatformFeeSettings()
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        var dbContext = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var tenant = await dbContext.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        var enabled = tenant.Settings.TryGetValue("platform_fee_enabled", out var enabledVal)
            && enabledVal is bool b && b;
        var percentage = tenant.Settings.TryGetValue("platform_fee_percent", out var pctVal)
            && pctVal is double d ? d : 0.5;

        return Ok(new
        {
            enabled,
            percentage,
            stripeConnectConfigured = !string.IsNullOrEmpty(tenant.StripeConnectId),
            description = "When enabled, Upkilo collects a small processing fee on each booking payment. This fee is passed to clients as a convenience charge.",
            minPercent = 0.5,
            maxPercent = 1.0
        });
    }

    /// <summary>
    /// POST /api/v1/payments/connect/start — begin (or resume) Stripe Connect onboarding.
    ///
    /// Creates the connected account if the tenant does not have one, then returns a Stripe-hosted
    /// onboarding URL to redirect the owner to. Safe to call repeatedly: account creation is
    /// idempotent on the stored id, and Stripe account links are single-use and short-lived, so a
    /// tenant who abandons onboarding resumes by calling this again.
    ///
    /// Redirect URLs are built from server configuration and are NOT accepted from the request.
    /// Stripe sends the user to whatever is passed here, so taking them from the client would make
    /// this an open redirect.
    /// </summary>
    [HttpPost("connect/start")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> StartConnectOnboarding(
        [FromServices] AppDbContext dbContext,
        [FromServices] IConfiguration configuration)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value);
        if (tenant == null) return NotFound();

        // Stripe requires an email for the account. Prefer the tenant's, fall back to the signed-in
        // owner's, so onboarding is not blocked by a business record that never had one filled in.
        var email = !string.IsNullOrWhiteSpace(tenant.Email)
            ? tenant.Email
            : User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new
            {
                error = "email_required",
                message = "Add a business email before connecting Stripe."
            });

        var appUrl = (configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');

        try
        {
            var connectId = await _paymentService.CreateConnectAccountAsync(tenantId.Value, email);

            // Redirects target /settings/billing because that route exists. They previously pointed
            // at /settings/payments, which does not — an owner finishing Stripe onboarding was
            // returned to a 404. The account and currency sync completed correctly regardless;
            // only the landing page was broken.
            var onboardingUrl = await _paymentService.CreateConnectOnboardingLinkAsync(
                connectId,
                // Stripe sends the user here if the link expired before they finished — the client
                // should call this endpoint again for a fresh one.
                refreshUrl: $"{appUrl}/settings/billing?connect=refresh",
                // On completion. The page should call GET connect/status, which syncs the currency
                // from the now-onboarded account.
                returnUrl: $"{appUrl}/settings/billing?connect=return");

            _logger.LogInformation("Connect onboarding started for tenant {TenantId} ({ConnectId})", tenantId, connectId);

            return Ok(new { connectId, onboardingUrl });
        }
        catch (InvalidOperationException ex)
        {
            // PaymentService throws this when Stripe is not configured. A 503 says "this
            // deployment cannot do it", which is true; a 500 would suggest a crash.
            _logger.LogWarning(ex, "Connect onboarding unavailable for tenant {TenantId}", tenantId);
            return StatusCode(503, new
            {
                error = "stripe_unavailable",
                message = "Payment processing is not configured on this environment."
            });
        }
        catch (Stripe.StripeException ex)
        {
            // A key that is present but rejected (placeholder, revoked, wrong mode) reaches here.
            // The exception message is deliberately NOT returned: Stripe includes the partially
            // masked API key in it, and echoing that to a caller leaks deployment configuration.
            _logger.LogError(ex, "Stripe rejected Connect onboarding for tenant {TenantId}", tenantId);
            return StatusCode(503, new
            {
                error = "stripe_unavailable",
                message = "Could not reach Stripe to start onboarding. Please try again shortly."
            });
        }
    }

    /// <summary>
    /// GET /api/v1/payments/connect/status — onboarding state of the tenant's connected account.
    ///
    /// Also syncs the tenant's currency from the account, so the page the owner lands on after
    /// onboarding reflects their real settlement currency without waiting on webhook delivery.
    /// </summary>
    [HttpGet("connect/status")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetConnectStatus(
        [FromServices] AppDbContext dbContext,
        [FromServices] TenantCurrencySyncService currencySync,
        CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value, ct);
        if (tenant == null) return NotFound();

        if (string.IsNullOrEmpty(tenant.StripeConnectId))
            return Ok(new
            {
                connected = false,
                detailsSubmitted = false,
                chargesEnabled = false,
                currency = (string?)null,
                country = (string?)null,
                message = "No Stripe account connected. Payments and payouts are unavailable."
            });

        var account = await _paymentService.GetConnectAccountAsync(tenant.StripeConnectId, ct);

        if (account == null)
            return Ok(new
            {
                connected = true,
                detailsSubmitted = false,
                chargesEnabled = false,
                currency = tenant.Currency,
                country = (string?)null,
                // The stored id no longer resolves — typically the tenant revoked platform access.
                message = "The connected Stripe account could not be reached. It may have been disconnected."
            });

        // ApplyAsync, not SyncFromStripeAsync: the tenant row and the Stripe account are both
        // already loaded above. SyncFromStripeAsync re-fetches both, which made every status
        // request cost two DB reads and two live Stripe calls where one of each suffices.
        var sync = await currencySync.ApplyAsync(tenant, account.DefaultCurrency, account.DetailsSubmitted, ct);

        return Ok(new
        {
            connected = true,
            account.DetailsSubmitted,
            account.ChargesEnabled,
            currency = sync.Current,
            account.Country,
            currencyChanged = sync.Changed,
            servicesNeedingReview = sync.StalePriceCount,
            message = !account.DetailsSubmitted
                ? "Stripe onboarding is incomplete. Finish it to start taking payments."
                : !account.ChargesEnabled
                    ? "Stripe is reviewing this account. Charges are not enabled yet."
                    : sync.Changed && sync.StalePriceCount > 0
                        ? $"Connected. Your currency is now {sync.Current}, and {sync.StalePriceCount} "
                          + $"service price(s) are still entered in {sync.Previous} — review them, as "
                          + "the amounts were not converted."
                        : "Connected and ready to take payments."
        });
    }

    /// <summary>
    /// POST /api/v1/payments/connect/sync-currency — re-read this tenant's settlement currency
    /// from their connected Stripe account.
    ///
    /// The account.updated webhook is the primary path and needs no user action. This exists as
    /// the manual fallback: for accounts connected before that handler existed, and for the case
    /// where a webhook was missed or the tenant reconnected a different account.
    /// </summary>
    [HttpPost("connect/sync-currency")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> SyncConnectCurrency(
        [FromServices] TenantCurrencySyncService currencySync,
        CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        var result = await currencySync.SyncFromStripeAsync(tenantId.Value, ct);

        if (!result.Changed && result.Reason == "no_connected_account")
            return BadRequest(new
            {
                error = "no_connected_account",
                message = "Connect a Stripe account before syncing your currency."
            });

        return Ok(new
        {
            changed = result.Changed,
            currency = result.Current,
            previousCurrency = result.Previous,
            reason = result.Reason,
            // Non-zero means the tenant has services priced in the currency they used to settle
            // in. Prices are never auto-converted, so these need a human decision.
            servicesNeedingReview = result.StalePriceCount,
            message = result.Changed && result.StalePriceCount > 0
                ? $"Currency updated to {result.Current}. {result.StalePriceCount} service price(s) are still "
                  + $"entered in {result.Previous} — review them, as the amounts were not converted."
                : result.Changed
                    ? $"Currency updated to {result.Current}."
                    : "Currency already matches your connected Stripe account."
        });
    }

    /// <summary>
    /// PATCH /api/v1/payments/platform-fee — enable/disable the platform processing fee.
    /// </summary>
    [HttpPatch("platform-fee")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> UpdatePlatformFeeSettings([FromBody] PlatformFeeRequest request)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.Percentage < 0.5 || request.Percentage > 1.0)
            return BadRequest(new { error = "Percentage must be between 0.5 and 1.0" });

        var dbContext = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var tenant = await dbContext.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        tenant.Settings["platform_fee_enabled"] = request.Enabled;
        tenant.Settings["platform_fee_percent"] = request.Percentage;
        tenant.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "[PlatformFee] Tenant {TenantId} set fee: enabled={Enabled}, percent={Pct}",
            tenantId, request.Enabled, request.Percentage);

        return Ok(new { enabled = request.Enabled, percentage = request.Percentage });
    }

    /// <summary>
    /// Payment history for the current tenant, newest first.
    /// </summary>
    /// <remarks>
    /// Backs the mobile Payments screen. Tenant-scoped via the global query filter plus an
    /// explicit TenantId predicate.
    /// </remarks>
    /// <response code="200">Paged payment history</response>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPaymentHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var tenantId = GetTenantId();
        if (tenantId == null) return Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var dbContext = HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        var query = dbContext.Payments
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var parsed))
        {
            query = query.Where(p => p.Status == parsed);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                id = p.Id,
                bookingId = p.BookingId,
                clientId = p.ClientId,
                clientName = p.Client != null ? p.Client.FirstName + " " + p.Client.LastName : null,
                amount = p.Amount,
                currency = p.Currency,
                status = p.Status.ToString(),
                paymentMethod = p.PaymentMethod,
                tipAmount = p.TipAmount,
                refundAmount = p.RefundAmount,
                refundedAt = p.RefundedAt,
                createdAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = items, page, pageSize, total });
    }

    private static bool IsAllowedRedirectUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        var allowed = new[] { "app.upkilo.com", "upkilo.com", "localhost" };
        return allowed.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));
    }
}

public record RazorpayOrderRequest(Guid BookingId, string ReceiptId);
public record RazorpayVerifyRequest(Guid BookingId, string OrderId, string PaymentId, string Signature);
public record PlatformFeeRequest(bool Enabled, double Percentage = 0.5);

public record CheckoutRequestDto(string PriceId, string SuccessUrl, string CancelUrl, bool IsAnnual = false, string? PromotionCode = null);
public record RefundRequestDto(string PaymentIntentId, decimal? Amount = null, string? Reason = null);
