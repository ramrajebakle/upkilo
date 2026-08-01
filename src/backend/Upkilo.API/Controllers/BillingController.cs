using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Core.Interfaces;
using Stripe.Checkout;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Owner")]
public class BillingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ISubscriptionService _subscriptionService;
    private readonly UpsellTriggerService _upsellTrigger;
    private readonly ILogger<BillingController> _logger;
    private readonly IConfiguration _configuration;

    public BillingController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ISubscriptionService subscriptionService,
        UpsellTriggerService upsellTrigger,
        IConfiguration configuration,
        ILogger<BillingController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _subscriptionService = subscriptionService;
        _upsellTrigger = upsellTrigger;
        _configuration = configuration;
        _logger = logger;
    }
    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var sub = await _context.Set<Upkilo.Core.Entities.Subscription>()
            .Include(s => s.PricingPlan).ThenInclude(p => p!.Prices)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (sub == null) return Ok(new { status = "none" });

        return Ok(sub);
    }

    /// <summary>
    /// Currencies the platform supports. Public, and cached hard — this is a static catalogue.
    ///
    /// Exists so clients populate currency pickers from the same registry the server validates
    /// against. Previously the service form carried its own hardcoded list of four codes, which
    /// silently excluded currencies the platform bills and accepts payments in.
    /// </summary>
    [HttpGet("currencies")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult GetCurrencies() =>
        Ok(Upkilo.Core.Helpers.Currency.All.Select(c => new
        {
            code = c.Code,
            symbol = c.Symbol,
            name = c.Name,
            // Clients need the exponent to render and validate amounts: a JPY field must not
            // accept or display decimal places.
            decimals = c.Exponent
        }));

    /// <summary>
    /// Published pricing plans. Public — this backs the marketing pricing table.
    /// </summary>
    /// <param name="currency">
    /// ISO code to price in. Each plan carries a row per supported currency, so without this
    /// the response mixed them: one plan priced in INR, the next in AED, the next in CAD.
    /// Worse, `currency` was selected by a separate FirstOrDefault() with no cycle filter, so
    /// the code returned did not necessarily belong to the amount returned.
    /// </param>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans([FromQuery] string currency = "USD")
    {
        var requested = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();

        // Return new PricingPlan data if available; fall back to legacy SubscriptionPlan
        // AsSplitQuery: Prices and FeatureMappings are sibling collections on the same root, so a
        // single JOIN returns prices x featureMappings rows per plan (30 instead of 17 at current
        // catalogue size) rather than prices + featureMappings. Matches the pattern already used
        // in SubscriptionService and UpsellTriggerService for the same shape.
        var newPlans = await _context.Set<Upkilo.Core.Entities.PricingPlan>()
            .Include(p => p.Prices)
            .Include(p => p.FeatureMappings)
                .ThenInclude(m => m.PricingFeature)
            .Where(p => p.IsActive)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();

        // Resolve ONE currency for the whole response before mapping any plan.
        //
        // Resolving per-plan meant a single response could mix currencies: asking for JPY
        // returned "Free" labelled JPY (it carries no price rows at all, so the fallback kept
        // the requested code) next to "Agency" labelled USD (no JPY row exists, so it fell back).
        // A pricing table rendered from that shows two currencies side by side.
        var available = newPlans
            .SelectMany(p => p.Prices)
            .Select(x => x.CurrencyCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var responseCurrency =
            available.Contains(requested) ? requested
            : available.Contains("USD") ? "USD"
            : available.FirstOrDefault() ?? requested;

        var mapped = newPlans.Select(p =>
        {
            // Every plan is priced in responseCurrency. A plan with no row for it (the Free plan
            // has no prices) reports null amounts rather than borrowing another currency's.
            var priced = p.Prices.Where(x =>
                string.Equals(x.CurrencyCode, responseCurrency, StringComparison.OrdinalIgnoreCase)).ToList();

            return new
            {
                p.Id,
                p.Name,
                p.Description,
                p.TrialDays,
                p.IsCustom,
                monthlyPrice = priced.FirstOrDefault(x => x.Cycle == Upkilo.Core.Entities.BillingCycle.Monthly)?.Amount,
                annualPrice = priced.FirstOrDefault(x => x.Cycle == Upkilo.Core.Entities.BillingCycle.Annual)?.Amount,
                currency = responseCurrency,
                ctaLabel = p.IsCustom ? "Contact us" : "Get started",
                features = p.FeatureMappings.Select(m => new
                {
                    key = m.PricingFeature?.Key,
                    name = m.PricingFeature?.Name,
                    enabled = m.IsEnabled,
                    limit = m.NumericLimit
                })
            };
        });
        // currency is echoed at the response level so a client can tell that a requested currency
        // was unavailable and it is being shown a fallback, rather than inferring it per row.
        return Ok(new { data = mapped, currency = responseCurrency, requestedCurrency = requested });
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Invoices
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.IssueDate);

        var total = await query.CountAsync();
        var invoices = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { data = invoices, total });
    }

    /// <summary>
    /// Single invoice for the current tenant.
    /// </summary>
    [HttpGet("invoices/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var invoice = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (invoice == null) return NotFound(new { message = "Invoice not found." });
        return Ok(invoice);
    }

    /// <summary>
    /// Email the customer a payment reminder for an unpaid invoice.
    /// </summary>
    [HttpPost("invoices/{id:guid}/send-reminder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendInvoiceReminder(
        Guid id,
        [FromServices] Upkilo.Core.Interfaces.IEmailService emailService)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (invoice == null) return NotFound(new { message = "Invoice not found." });

        if (invoice.Status == Upkilo.Core.Entities.InvoiceStatus.Paid)
            return BadRequest(new { message = "Invoice is already paid." });

        if (string.IsNullOrWhiteSpace(invoice.CustomerEmail))
            return BadRequest(new { message = "Invoice has no customer email address." });

        var subject = $"Payment reminder — invoice {invoice.InvoiceNumber}";
        var body =
            $"<p>Hi {invoice.CustomerName},</p>" +
            $"<p>This is a reminder that invoice <strong>{invoice.InvoiceNumber}</strong> " +
            $"for <strong>{invoice.TotalAmount:0.00} {invoice.Currency}</strong> " +
            $"is due on {invoice.DueDate:yyyy-MM-dd}.</p>" +
            (string.IsNullOrWhiteSpace(invoice.HostedInvoiceUrl)
                ? string.Empty
                : $"<p><a href=\"{invoice.HostedInvoiceUrl}\">View and pay your invoice</a></p>") +
            "<p>Thank you.</p>";

        await emailService.SendEmailAsync(invoice.CustomerEmail, subject, body);

        _logger.LogInformation(
            "[Billing] Reminder sent for invoice {InvoiceNumber} (tenant {TenantId})",
            invoice.InvoiceNumber, tenantId);

        return Ok(new { message = "Reminder sent.", sentTo = invoice.CustomerEmail });
    }

    /// <summary>
    /// Mark an invoice as paid. Tenant-scoped equivalent of the platform-admin
    /// admin/billing/invoices/{id}/mark-paid route.
    /// </summary>
    [HttpPost("invoices/{id:guid}/mark-paid")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkInvoicePaid(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (invoice == null) return NotFound(new { message = "Invoice not found." });

        if (invoice.Status == Upkilo.Core.Entities.InvoiceStatus.Paid)
            return Ok(new { message = "Invoice already marked paid.", status = invoice.Status.ToString() });

        if (invoice.Status is Upkilo.Core.Entities.InvoiceStatus.Void
            or Upkilo.Core.Entities.InvoiceStatus.Refunded)
            return BadRequest(new { message = $"Cannot mark a {invoice.Status} invoice as paid." });

        invoice.Status = Upkilo.Core.Entities.InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[Billing] Invoice {InvoiceNumber} marked paid (tenant {TenantId})",
            invoice.InvoiceNumber, tenantId);

        return Ok(new { message = "Invoice marked paid.", status = invoice.Status.ToString(), paidAt = invoice.PaidAt });
    }

    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var usageSummary = await _subscriptionService.GetUsageAsync(tenantId.Value);

        var usage = new
        {
            staff = new
            {
                used = usageSummary.StaffCount,
                limit = usageSummary.StaffLimit
            },
            locations = new
            {
                used = usageSummary.LocationCount,
                limit = usageSummary.LocationLimit
            },
            bookings = new
            {
                used = usageSummary.BookingsUsed,
                limit = usageSummary.BookingsLimit
            },
            sms = new
            {
                used = usageSummary.SmsUsed,
                limit = usageSummary.SmsLimit
            },
            aiCredits = new
            {
                used = usageSummary.AiCreditsUsed,
                limit = usageSummary.AiCreditsLimit
            },
            storage = new
            {
                usedBytes = usageSummary.StorageUsedBytes,
                limitBytes = usageSummary.StorageLimitBytes
            },
            periodStart = usageSummary.PeriodStart,
            periodEnd = usageSummary.PeriodEnd,
            enabledFeatures = usageSummary.EnabledFeatures
        };

        return Ok(new { usage });
    }

    /// <summary>
    /// Usage summary with percent-of-limit values and nearLimit flags for upgrade nudge UI.
    /// </summary>
    [HttpGet("usage-summary")]
    public async Task<IActionResult> GetUsageSummary()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var u = await _subscriptionService.GetUsageAsync(tenantId.Value);
        static int pct(int used, int limit) =>
            limit <= 0 ? 0 : Math.Min(100, (int)Math.Round((double)used / limit * 100));

        var staff = pct(u.StaffCount, u.StaffLimit);
        var ai = pct(u.AiCreditsUsed, u.AiCreditsLimit);
        var sms = pct(u.SmsUsed, u.SmsLimit);
        var locations = pct(u.LocationCount, u.LocationLimit);
        var bookings = pct(u.BookingsUsed, u.BookingsLimit);

        return Ok(new
        {
            staff = new { used = u.StaffCount, limit = u.StaffLimit, percent = staff, nearLimit = staff >= 80 },
            aiActions = new { used = u.AiCreditsUsed, limit = u.AiCreditsLimit, percent = ai, nearLimit = ai >= 80 },
            sms = new { used = u.SmsUsed, limit = u.SmsLimit, percent = sms, nearLimit = sms >= 80 },
            locations = new { used = u.LocationCount, limit = u.LocationLimit, percent = locations, nearLimit = locations >= 80 },
            bookings = new { used = u.BookingsUsed, limit = u.BookingsLimit, percent = bookings, nearLimit = bookings >= 80 },
            periodStart = u.PeriodStart,
            periodEnd = u.PeriodEnd,
            enabledFeatures = u.EnabledFeatures,
            anyNearLimit = staff >= 80 || ai >= 80 || sms >= 80 || locations >= 80 || bookings >= 80
        });
    }

    /// <summary>
    /// Returns potential savings from switching to annual billing.
    /// Powers the in-app annual upgrade banner shown after 30+ days on monthly.
    /// </summary>
    [HttpGet("annual-savings")]
    public async Task<IActionResult> GetAnnualSavings()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var subscription = await _context.Set<Upkilo.Core.Entities.Subscription>()
            .Include(s => s.PricingPlan)
                .ThenInclude(p => p!.Prices)
            .Where(s => s.TenantId == tenantId.Value && s.Status == Upkilo.Core.Entities.SubscriptionStatus.Active)
            .FirstOrDefaultAsync();

        if (subscription == null)
            return Ok(new { eligible = false, reason = "No active subscription" });

        if (subscription.BillingInterval == Upkilo.Core.Entities.BillingInterval.Annual)
            return Ok(new { eligible = false, reason = "Already on annual billing" });

        var plan = subscription.PricingPlan;
        if (plan == null)
            return Ok(new { eligible = false, reason = "Plan pricing data unavailable" });

        var monthlyPrice = plan.Prices
            .FirstOrDefault(p => p.Cycle == Upkilo.Core.Entities.BillingCycle.Monthly && p.CurrencyCode == "USD");
        var annualPrice = plan.Prices
            .FirstOrDefault(p => p.Cycle == Upkilo.Core.Entities.BillingCycle.Annual && p.CurrencyCode == "USD");

        if (monthlyPrice == null || annualPrice == null)
            return Ok(new { eligible = false, reason = "Annual pricing not yet configured" });

        var annualIfMonthly = monthlyPrice.Amount * 12;
        var savingsAmount = annualIfMonthly - annualPrice.Amount;
        var savingsPercent = Math.Round(savingsAmount / annualIfMonthly * 100, 0);
        var monthsOnPlan = (DateTime.UtcNow - subscription.StartDate).TotalDays / 30.0;

        return Ok(new
        {
            eligible = true,
            planName = plan.Name,
            monthlyAmount = monthlyPrice.Amount,
            annualAmount = annualPrice.Amount,
            annualIfMonthly,
            savingsAmount,
            savingsPercent,
            currency = monthlyPrice.CurrencyCode,
            monthsOnCurrentPlan = Math.Round(monthsOnPlan, 1),
            showBanner = monthsOnPlan >= 30
        });
    }

    /// <summary>
    /// Returns contextual upsell triggers for the current tenant.
    /// Frontend reads this on page load and shows toast/banner notifications for active triggers.
    /// </summary>
    [HttpGet("upsell-triggers")]
    public async Task<IActionResult> GetUpsellTriggers()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var triggers = await _upsellTrigger.EvaluateTriggersAsync(tenantId.Value);

        return Ok(new
        {
            triggers = triggers.Select(t => new
            {
                type = t.Type,
                message = t.Message,
                priority = t.Priority
            }),
            hasActiveTriggers = triggers.Count > 0,
            criticalCount = triggers.Count(t => t.Priority == "Critical"),
            highCount = triggers.Count(t => t.Priority == "High")
        });
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] BillingCheckoutRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var result = await _subscriptionService.CreateCheckoutSessionAsync(
            tenantId.Value,
            request.PlanId,
            request.IsAnnual,
            request.PromoCode);

        if (!result.Success || string.IsNullOrEmpty(result.SessionUrl))
            return BadRequest(result.Error);

        return Ok(new { url = result.SessionUrl });
    }

    [HttpPost("portal")]
    public async Task<IActionResult> CreatePortal([FromBody] PortalRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (!IsAllowedRedirectUrl(request.ReturnUrl))
            return BadRequest("Invalid return URL");

        var url = await _subscriptionService.CreateBillingPortalSessionAsync(tenantId.Value, request.ReturnUrl);
        return Ok(new { url });
    }

    // SECURITY: POST /create-checkout-session was REMOVED (Audit C-2).
    // It accepted raw Stripe Price IDs from the client, bypassing all plan validation.
    // All checkout flows MUST go through POST /checkout → ISubscriptionService.

    [HttpPost("customer-portal")]
    public async Task<IActionResult> CreateCustomerPortalSession()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null || string.IsNullOrEmpty(tenant.StripeCustomerId))
            return BadRequest("Tenant does not have an active billing profile.");

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = tenant.StripeCustomerId,
            ReturnUrl = $"{(_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/')}/settings/billing",
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);

        return Ok(new { url = session.Url });
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

public record BillingCheckoutRequest(string PlanId, bool IsAnnual, string? PromoCode = null);
public record PortalRequest(string ReturnUrl);
