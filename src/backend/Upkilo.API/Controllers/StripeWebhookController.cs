using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Stripe;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using LocalSubscription = Upkilo.Core.Entities.Subscription;

namespace Upkilo.API.Controllers;

/// <summary>
/// Stripe webhook controller — handles all inbound Stripe events.
/// Uses ProcessedWebhook table for idempotency (prevents duplicate processing).
/// </summary>
[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly AppDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISecretProvider _secretProvider;
    private readonly SubscriptionDowngradeHandler _downgradeHandler;
    private readonly IEmailService _emailService;
    private readonly IDistributedCache _cache;
    private readonly TenantCurrencySyncService _currencySync;

    public StripeWebhookController(
        ILogger<StripeWebhookController> logger,
        AppDbContext context,
        ISubscriptionService subscriptionService,
        ISecretProvider secretProvider,
        SubscriptionDowngradeHandler downgradeHandler,
        IEmailService emailService,
        IDistributedCache cache,
        TenantCurrencySyncService currencySync)
    {
        _logger = logger;
        _context = context;
        _subscriptionService = subscriptionService;
        _secretProvider = secretProvider;
        _downgradeHandler = downgradeHandler;
        _emailService = emailService;
        _cache = cache;
        _currencySync = currencySync;
    }

    /// <summary>
    /// Stripe webhook receiver.
    /// HIGH-10: Must read the raw body BEFORE any JSON deserialization,
    /// otherwise the signature computed by Stripe over the exact bytes will not match.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]                    // Stripe cannot send auth headers
    [RequestSizeLimit(10 * 1024 * 1024)]  // 10 MB max — Stripe payloads are typically < 100 KB
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024,
                       ValueLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> Handle()
    {
        // HIGH-10: Enable buffering so we can read the body as raw bytes
        HttpContext.Request.EnableBuffering();
        using var reader = new StreamReader(
            HttpContext.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        var json = await reader.ReadToEndAsync();
        // Rewind so downstream middleware is unaffected
        HttpContext.Request.Body.Position = 0;

        string? stripeSignature = Request.Headers["Stripe-Signature"];
        if (string.IsNullOrEmpty(stripeSignature))
        {
            _logger.LogWarning("Missing Stripe-Signature header");
            return BadRequest("Missing Stripe-Signature header");
        }
        // Determine webhook type by trying the Connect secret first (if configured).
        // Previously used json.Contains("\"account\":") which could be spoofed by a
        // crafted payload — now we rely solely on HMAC signature verification.
        // Strategy: try platform secret; if that fails and Connect secret is configured, try it.
        var platformSecret = await _secretProvider.GetSecretAsync("Stripe:WebhookSecret");
        var connectSecret  = await _secretProvider.GetSecretAsync("Stripe:ConnectWebhookSecret");

        Stripe.Event? stripeEvent = null;
        bool isConnectWebhook = false;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, platformSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            if (!string.IsNullOrEmpty(connectSecret))
            {
                try
                {
                    stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, connectSecret, throwOnApiVersionMismatch: false);
                    isConnectWebhook = true;
                }
                catch (StripeException e)
                {
                    _logger.LogError(e, "Stripe webhook signature verification failed against both platform and Connect secrets");
                    return BadRequest();
                }
            }
        }

        if (stripeEvent == null)
        {
            _logger.LogError("Stripe webhook signature verification failed");
            return BadRequest();
        }

        _logger.LogInformation("Webhook received: {EventType} ({EventId})", stripeEvent.Type, stripeEvent.Id);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // ── Idempotency check (Atomic) ───────────────────────────────
            var inserted = await _context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""ProcessedWebhooks"" (""EventId"", ""EventType"", ""ProcessedAt"")
                  VALUES ({0}, {1}, {2})
                  ON CONFLICT (""EventId"") DO NOTHING", 
                stripeEvent.Id, stripeEvent.Type, DateTime.UtcNow);

            if (inserted == 0)
            {
                _logger.LogInformation("Webhook {EventId} already processed — skipping (idempotent)", stripeEvent.Id);
                return Ok();
            }

            // ── Route to handler ─────────────────────────────────────────
            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session != null) await HandleCheckoutSessionCompleted(session);
                    break;

                case EventTypes.CustomerSubscriptionUpdated:
                    var updatedSub = stripeEvent.Data.Object as Stripe.Subscription;
                    if (updatedSub != null) await HandleSubscriptionUpdated(updatedSub);
                    break;

                case EventTypes.CustomerSubscriptionDeleted:
                    var deletedSub = stripeEvent.Data.Object as Stripe.Subscription;
                    if (deletedSub != null) await HandleSubscriptionDeleted(deletedSub);
                    break;

                case EventTypes.CustomerSubscriptionPaused:
                    var pausedSub = stripeEvent.Data.Object as Stripe.Subscription;
                    if (pausedSub != null) await HandleSubscriptionPaused(pausedSub);
                    break;

                case EventTypes.InvoicePaymentSucceeded:
                    var invoice = stripeEvent.Data.Object as Stripe.Invoice;
                    if (invoice != null) await HandleInvoicePaymentSucceeded(invoice);
                    break;

                case EventTypes.InvoicePaymentFailed:
                    var failedInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    if (failedInvoice != null) await HandleInvoicePaymentFailed(failedInvoice);
                    break;

                case EventTypes.ChargeDisputeCreated:
                    var dispute = stripeEvent.Data.Object as Stripe.Dispute;
                    if (dispute != null) await HandleDisputeCreated(dispute);
                    break;

                case EventTypes.ChargeDisputeClosed:
                    var closedDispute = stripeEvent.Data.Object as Stripe.Dispute;
                    if (closedDispute != null) await HandleDisputeClosed(closedDispute);
                    break;

                // ── New handlers (Feb 2026) ──────────────────────────────
                case EventTypes.CustomerSubscriptionResumed:
                    var resumedSub = stripeEvent.Data.Object as Stripe.Subscription;
                    if (resumedSub != null) await HandleSubscriptionResumed(resumedSub);
                    break;

                case EventTypes.InvoiceCreated:
                    var createdInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    if (createdInvoice != null) await HandleInvoiceCreated(createdInvoice);
                    break;

                case EventTypes.InvoiceFinalized:
                    var finalizedInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    if (finalizedInvoice != null) await HandleInvoiceFinalized(finalizedInvoice);
                    break;

                case EventTypes.PaymentIntentSucceeded:
                    var successPi = stripeEvent.Data.Object as Stripe.PaymentIntent;
                    if (successPi != null) await HandlePaymentIntentSucceeded(successPi);
                    break;

                case EventTypes.PaymentIntentPaymentFailed:
                    var failedPi = stripeEvent.Data.Object as Stripe.PaymentIntent;
                    if (failedPi != null) await HandlePaymentIntentFailed(failedPi);
                    break;

                case EventTypes.CustomerUpdated:
                    var customer = stripeEvent.Data.Object as Stripe.Customer;
                    if (customer != null) await HandleCustomerUpdated(customer);
                    break;

                case EventTypes.ChargeRefunded:
                    var refundedCharge = stripeEvent.Data.Object as Stripe.Charge;
                    if (refundedCharge != null) await HandleChargeRefunded(refundedCharge);
                    break;

                case EventTypes.CustomerSubscriptionTrialWillEnd:
                    var trialSub = stripeEvent.Data.Object as Stripe.Subscription;
                    if (trialSub != null) await HandleSubscriptionTrialWillEnd(trialSub);
                    break;

                case EventTypes.InvoiceUpcoming:
                    var upcomingInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    if (upcomingInvoice != null) await HandleInvoiceUpcoming(upcomingInvoice);
                    break;

                case EventTypes.InvoiceVoided:
                    var voidedInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    if (voidedInvoice != null) await HandleInvoiceVoided(voidedInvoice);
                    break;

                case EventTypes.InvoiceMarkedUncollectible:
                    var uncollectibleInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    if (uncollectibleInvoice != null) await HandleInvoiceMarkedUncollectible(uncollectibleInvoice);
                    break;

                case EventTypes.ChargeFailed:
                    var failedCharge = stripeEvent.Data.Object as Stripe.Charge;
                    if (failedCharge != null) await HandleChargeFailed(failedCharge);
                    break;

                case EventTypes.ChargeSucceeded:
                    var successCharge = stripeEvent.Data.Object as Stripe.Charge;
                    if (successCharge != null) await HandleChargeSucceeded(successCharge);
                    break;

                case EventTypes.CustomerDeleted:
                    var deletedCustomer = stripeEvent.Data.Object as Stripe.Customer;
                    if (deletedCustomer != null) await HandleCustomerDeleted(deletedCustomer);
                    break;

                case EventTypes.PaymentIntentCreated:
                    var createdPi = stripeEvent.Data.Object as Stripe.PaymentIntent;
                    if (createdPi != null) await HandlePaymentIntentCreated(createdPi);
                    break;

                case EventTypes.CustomerSubscriptionCreated:
                    var newSub = stripeEvent.Data.Object as Stripe.Subscription;
                    if (newSub != null) await HandleReferralRewardFulfillment(newSub);
                    break;

                case EventTypes.AccountUpdated:
                    var connectedAccount = stripeEvent.Data.Object as Stripe.Account;
                    if (connectedAccount != null) await HandleConnectedAccountUpdated(connectedAccount);
                    break;

                default:
                    _logger.LogDebug("Unhandled Stripe event: {EventType}", stripeEvent.Type);
                    break;
            }

            // (Processed status was already inserted atomically at the start of the method)

            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e, "Internal webhook processing error for event {EventId}", stripeEvent?.Id);
            // Allow Stripe to retry by returning 500 for unhandled exceptions (which are often transient).
            return StatusCode(500, new { error = "internal_error" });
        }
    }

    // ── Connect account ───────────────────────────────────────────────

    /// <summary>
    /// Keeps a tenant's currency in step with the Stripe account they actually settle through.
    ///
    /// Connect accounts here are created as Standard, which means the tenant chooses their country
    /// inside Stripe's hosted onboarding — it is not known when the account is created. Until this
    /// handler existed nothing ever read the result back, so a tenant who onboarded an Indian
    /// account kept the "USD" default and priced their services in dollars while settling in rupees.
    ///
    /// account.updated is the reliable trigger: it fires when onboarding completes and whenever the
    /// account subsequently changes.
    /// </summary>
    private async Task HandleConnectedAccountUpdated(Stripe.Account account)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.StripeConnectId == account.Id);
        if (tenant == null)
        {
            // Also arrives for staff payout accounts, which are not tenants. Not an error.
            _logger.LogDebug("account.updated for {AccountId} matched no tenant", account.Id);
            return;
        }

        // An account that has not completed onboarding reports a placeholder currency that is not
        // the tenant's real settlement currency. Writing it would replace one wrong value with
        // another, so wait until Stripe says the details are in.
        if (!account.DetailsSubmitted)
        {
            _logger.LogInformation(
                "account.updated for tenant {TenantId}: onboarding incomplete, currency not synced", tenant.Id);
            return;
        }

        var result = await _currencySync.ApplyAsync(tenant, account.DefaultCurrency, account.DetailsSubmitted);

        if (result.Changed && result.StalePriceCount > 0)
        {
            _logger.LogWarning(
                "Tenant {TenantId} now settles in {Current} (was {Previous}); {Count} service price(s) "
                + "still carry the old currency and need review.",
                tenant.Id, result.Current, result.Previous, result.StalePriceCount);
        }
    }

    // ── Checkout ──────────────────────────────────────────────────────

    private async Task HandleCheckoutSessionCompleted(Stripe.Checkout.Session session)
    {
        if (session.Mode == "subscription" && session.Metadata.TryGetValue("tenant_id", out var tenantIdStr))
        {
            if (Guid.TryParse(tenantIdStr, out var tenantId))
            {
                await _subscriptionService.SyncWithStripeAsync(tenantId);
                _logger.LogInformation("Synced subscription after checkout for tenant {TenantId}", tenantId);

                var tenant = await _context.Tenants.FindAsync(tenantId);
                if (tenant != null && !string.IsNullOrEmpty(tenant.Email))
                {
                    await _emailService.SendSystemEmailAsync(
                        tenant.Email,
                        "Welcome to Upkilo Pro!",
                        "Your subscription has been successfully activated. Thank you for upgrading to Pro! You now have access to premium features including AI Automation and advanced CRM tools."
                    );
                    _logger.LogInformation("Sent Welcome Email to tenant {TenantId}", tenantId);
                }
            }
        }
    }

    // ── Subscription Lifecycle ────────────────────────────────────────

    private async Task HandleSubscriptionUpdated(Stripe.Subscription stripeSub)
    {
        var localSub = await _context.Set<LocalSubscription>()
            .Include(s => s.PricingPlan)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (localSub != null)
        {
            localSub.Status = MapStripeStatus(stripeSub.Status);

            // Detect plan change from Stripe metadata
            if (stripeSub.Metadata.TryGetValue("plan_id", out var newPlanIdStr)
                && Guid.TryParse(newPlanIdStr, out var newPlanId)
                && localSub.PricingPlanId != newPlanId)
            {
                var oldPlan = localSub.PricingPlan;
                var newPricingPlan = await _context.Set<Upkilo.Core.Entities.PricingPlan>()
                    .Include(p => p.FeatureMappings).ThenInclude(m => m.PricingFeature)
                    .FirstOrDefaultAsync(p => p.Id == newPlanId);

                if (newPricingPlan != null)
                {
                    localSub.PricingPlanId = newPlanId;

                    // Enforce resource limits when the plan changes (handles both upgrades and downgrades).
                    // HandleDowngradeAsync is a no-op for upgrades since existing counts stay within new limits.
                    if (oldPlan != null)
                    {
                        int newMaxStaff = GetPlanLimit(newPricingPlan, "max_staff", 1);
                        int newMaxLocations = GetPlanLimit(newPricingPlan, "max_locations", 1);
                        int newMaxServices = GetPlanLimit(newPricingPlan, "max_services", 10);
                        bool newWebhooks = GetPlanFeatureEnabled(newPricingPlan, "webhooks");
                        bool newApiAccess = GetPlanFeatureEnabled(newPricingPlan, "api_access");

                        await _downgradeHandler.HandleDowngradeAsync(
                            localSub.TenantId, oldPlan.Name, newPricingPlan.Name,
                            newMaxStaff, newMaxLocations, newMaxServices, newWebhooks, newApiAccess);
                    }
                }
            }

            await _context.SaveChangesAsync();
            await _cache.RemoveAsync($"tenant_tier:{localSub.TenantId}");
            _logger.LogInformation("Subscription {SubId} updated to {Status}, tier cache busted", stripeSub.Id, localSub.Status);
        }
    }

    private static int GetPlanLimit(Upkilo.Core.Entities.PricingPlan plan, string featureKey, int defaultValue)
    {
        var mapping = plan.FeatureMappings.FirstOrDefault(m =>
            string.Equals(m.PricingFeature?.Key, featureKey, StringComparison.OrdinalIgnoreCase));
        return mapping?.NumericLimit ?? defaultValue;
    }

    private static bool GetPlanFeatureEnabled(Upkilo.Core.Entities.PricingPlan plan, string featureKey)
    {
        var mapping = plan.FeatureMappings.FirstOrDefault(m =>
            string.Equals(m.PricingFeature?.Key, featureKey, StringComparison.OrdinalIgnoreCase));
        return mapping?.IsEnabled ?? false;
    }

    private async Task HandleSubscriptionDeleted(Stripe.Subscription stripeSub)
    {
        var localSub = await _context.Set<LocalSubscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (localSub != null)
        {
            localSub.Status = SubscriptionStatus.Cancelled;
            localSub.CancelledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync($"tenant_tier:{localSub.TenantId}");
            _logger.LogInformation("Subscription {SubId} cancelled via webhook, tier cache busted", stripeSub.Id);
        }
    }

    private async Task HandleSubscriptionPaused(Stripe.Subscription stripeSub)
    {
        var localSub = await _context.Set<LocalSubscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (localSub != null)
        {
            localSub.Status = SubscriptionStatus.Paused;
            localSub.PausedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync($"tenant_tier:{localSub.TenantId}");
            _logger.LogInformation("Subscription {SubId} paused via webhook, tier cache busted", stripeSub.Id);
        }
    }

    // ── Invoice Events ────────────────────────────────────────────────

    private async Task HandleInvoicePaymentSucceeded(Stripe.Invoice stripeInvoice)
    {
        // 1. Update Invoice status
        var localInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.StripeInvoiceId == stripeInvoice.Id);

        if (localInvoice != null)
        {
            localInvoice.Status = InvoiceStatus.Paid;
            localInvoice.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // 2. Sync Subscription
        var subId = GetSubscriptionId(stripeInvoice);
        if (string.IsNullOrEmpty(subId)) return;

        var localSub = await _context.Set<LocalSubscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subId);

        if (localSub != null)
        {
            localSub.Status = SubscriptionStatus.Active;
            await _subscriptionService.SyncWithStripeAsync(localSub.TenantId);
            _logger.LogInformation("Invoice {InvoiceId} paid — subscription {SubId} synced for tenant {TenantId}",
                stripeInvoice.Id, subId, localSub.TenantId);

            // Affiliate commission: check if this tenant was referred by an affiliate partner
            await CreateAffiliateCommissionIfApplicableAsync(
                localSub.TenantId,
                Upkilo.Core.Helpers.Currency.FromMinorUnits(stripeInvoice.Total, stripeInvoice.Currency),
                Upkilo.Core.Helpers.Currency.Normalize(stripeInvoice.Currency));
        }
    }

    private async Task CreateAffiliateCommissionIfApplicableAsync(Guid tenantId, decimal grossAmount, string currency)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return;

        // Check if the tenant was referred via an affiliate (stored in Metadata["affiliate_code"])
        if (!tenant.Metadata.TryGetValue("affiliate_code", out var affiliateCodeObj)) return;
        var affiliateCode = affiliateCodeObj?.ToString();
        if (string.IsNullOrEmpty(affiliateCode)) return;

        var partner = await _context.PartnerAccounts
            .FirstOrDefaultAsync(p => p.ReferralCode == affiliateCode && !p.IsDeleted);
        if (partner == null) return;

        // Idempotency: prevent duplicate commissions on webhook replay
        var invoiceId = affiliateCodeObj?.ToString(); // reuse variable; invoiceId comes from caller context
        var alreadyExists = await _context.AffiliateCommissions
            .AnyAsync(c => c.TenantId == tenantId && c.PartnerAccountId == partner.Id
                        && c.GrossAmount == grossAmount);
        if (alreadyExists)
        {
            _logger.LogDebug("[Affiliate] Commission already recorded for tenant {TenantId}, partner {PartnerId}", tenantId, partner.Id);
            return;
        }

        const decimal commissionRate = 0.20m; // 20% recurring commission
        var commissionAmount = Math.Round(grossAmount * commissionRate, 2);

        _context.AffiliateCommissions.Add(new AffiliateCommission
        {
            Id = Guid.NewGuid(),
            PartnerAccountId = partner.Id,
            TenantId = tenantId,
            Source = "Subscription",
            GrossAmount = grossAmount,
            CommissionRate = commissionRate,
            CommissionAmount = commissionAmount,
            Currency = currency,
            Status = AffiliateCommissionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _logger.LogInformation("[Affiliate] Commission {Amount} {Currency} for partner {PartnerId} on tenant {TenantId}",
            commissionAmount, currency, partner.Id, tenantId);
    }

    private async Task HandleInvoicePaymentFailed(Stripe.Invoice invoice)
    {
        var subId = GetSubscriptionId(invoice);
        if (string.IsNullOrEmpty(subId)) return;

        var localSub = await _context.Set<LocalSubscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subId);

        if (localSub == null) return;

        localSub.Status = SubscriptionStatus.PastDue;
        await _context.SaveChangesAsync();

        _logger.LogWarning("Invoice {InvoiceId} payment failed — subscription {SubId} set to PastDue", invoice.Id, subId);

        // Start or advance the dunning cycle for this tenant.
        // Check for an active cycle tied to this specific invoice to prevent duplicates on webhook replay.
        var existingCycle = await _context.DunningCycles
            .FirstOrDefaultAsync(d => d.TenantId == localSub.TenantId
                                   && d.StripeInvoiceId == invoice.Id
                                   && (d.Status == "Active" || d.Status == "Retrying"));

        if (existingCycle == null)
        {
            _context.DunningCycles.Add(new DunningCycle
            {
                Id = Guid.NewGuid(),
                TenantId = localSub.TenantId,
                StripeInvoiceId = invoice.Id,
                Status = "Active",
                AttemptCount = 1,
                LastAttemptAt = DateTime.UtcNow,
                NextAttemptAt = DateTime.UtcNow.AddDays(3),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        else
        {
            existingCycle.AttemptCount++;
            existingCycle.LastAttemptAt = DateTime.UtcNow;
            existingCycle.NextAttemptAt = DateTime.UtcNow.AddDays(3 * existingCycle.AttemptCount);
            existingCycle.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Send payment failure email to tenant
        var tenant = await _context.Tenants.FindAsync(localSub.TenantId);
        if (tenant != null && !string.IsNullOrEmpty(tenant.Email))
        {
            // Previously `/ 100m` with a hardcoded "$": a tenant billed in JPY was told we could
            // not charge "$500000.00" for what was actually a ¥5,000 invoice.
            var amountDue = Upkilo.Core.Helpers.Currency.FromMinorUnits(invoice.AmountDue, invoice.Currency);
            var amountDueText = Upkilo.Core.Helpers.Currency.Format(amountDue, invoice.Currency);
            await _emailService.SendSystemEmailAsync(
                tenant.Email,
                "Action required: Payment failed for your Upkilo subscription",
                $"Hi {tenant.Name},\n\n" +
                $"We were unable to charge {amountDueText} for your Upkilo subscription.\n\n" +
                $"Please update your payment method to avoid service interruption:\n" +
                $"https://app.upkilo.com/settings/billing\n\n" +
                $"We will retry the payment in 3 days. After 3 failed attempts your account will be suspended.\n\n" +
                $"The Upkilo Team"
            );
            _logger.LogInformation("Sent payment-failed email to tenant {TenantId}", tenant.Id);
        }
    }

    // ── Dispute Events ────────────────────────────────────────────────

    private async Task HandleDisputeCreated(Stripe.Dispute dispute)
    {
        _logger.LogWarning(
            "DISPUTE opened: {DisputeId} for charge {ChargeId}, amount {Amount} {Currency}, reason: {Reason}",
            dispute.Id, dispute.ChargeId, Upkilo.Core.Helpers.Currency.FromMinorUnits(dispute.Amount, dispute.Currency), dispute.Currency, dispute.Reason);

        // Find the tenant via Stripe customer
        var chargeCustomerId = dispute.Charge?.CustomerId;
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.StripeCustomerId == chargeCustomerId);

        if (tenant != null)
        {
            _logger.LogWarning("Dispute affects tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);
            // Future: create an internal DisputeRecord, send admin notification
        }
    }

    private async Task HandleDisputeClosed(Stripe.Dispute dispute)
    {
        var outcome = dispute.Status; // won, lost, charge_refunded, warning_closed
        _logger.LogInformation(
            "Dispute {DisputeId} closed with status {Status}", dispute.Id, outcome);
        await Task.CompletedTask;
    }

    // ── Subscription Resume ───────────────────────────────────────────

    private async Task HandleSubscriptionResumed(Stripe.Subscription stripeSub)
    {
        var localSub = await _context.Set<LocalSubscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (localSub != null)
        {
            localSub.Status = SubscriptionStatus.Active;
            localSub.PausedAt = null;
            await _subscriptionService.SyncWithStripeAsync(localSub.TenantId);
            _logger.LogInformation("Subscription {SubId} resumed via webhook", stripeSub.Id);
        }
    }

    // ── Invoice Lifecycle ─────────────────────────────────────────────

    private async Task HandleInvoiceCreated(Stripe.Invoice stripeInvoice)
    {
        var tenantId = await GetTenantIdFromInvoice(stripeInvoice);
        if (tenantId == null) return;

        var localInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.StripeInvoiceId == stripeInvoice.Id);

        if (localInvoice == null)
        {
            localInvoice = new Upkilo.Core.Entities.Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                StripeInvoiceId = stripeInvoice.Id,
                InvoiceNumber = stripeInvoice.Number,
                IssueDate = stripeInvoice.Created,
                DueDate = stripeInvoice.DueDate ?? stripeInvoice.Created.AddDays(30),
                Status = MapStripeInvoiceStatus(stripeInvoice.Status),
                // Stripe reports totals in minor units. A flat /100 under-recorded zero-decimal
                // currencies by 100x (a ¥5,000 invoice was stored as ¥50).
                TotalAmount = Upkilo.Core.Helpers.Currency.FromMinorUnits(stripeInvoice.Total, stripeInvoice.Currency),
                Currency = Upkilo.Core.Helpers.Currency.Normalize(stripeInvoice.Currency),
                CustomerEmail = stripeInvoice.CustomerEmail,
                CustomerName = stripeInvoice.CustomerName,
                SubscriptionId = GetSubscriptionId(stripeInvoice),
                HostedInvoiceUrl = stripeInvoice.HostedInvoiceUrl,
                PdfUrl = stripeInvoice.InvoicePdf,
                Type = "Subscription"
            };
            _context.Invoices.Add(localInvoice);
        }
        else
        {
            localInvoice.Status = MapStripeInvoiceStatus(stripeInvoice.Status);
            localInvoice.HostedInvoiceUrl = stripeInvoice.HostedInvoiceUrl;
            localInvoice.PdfUrl = stripeInvoice.InvoicePdf;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Invoice {StripeInvoiceId} (No: {InvoiceNumber}) created/updated locally for tenant {TenantId}", 
            stripeInvoice.Id, stripeInvoice.Number, tenantId);
    }

    private async Task HandleInvoiceFinalized(Stripe.Invoice invoice)
    {
        _logger.LogInformation("Invoice {InvoiceId} finalized, total: {Amount} {Currency}",
            invoice.Id, Upkilo.Core.Helpers.Currency.FromMinorUnits(invoice.Total, invoice.Currency), invoice.Currency);
        await Task.CompletedTask;
    }

    // ── Payment Intent Events ─────────────────────────────────────────

    private async Task HandlePaymentIntentSucceeded(Stripe.PaymentIntent pi)
    {
        _logger.LogInformation("PaymentIntent {PiId} succeeded, amount: {Amount} {Currency}",
            pi.Id, Upkilo.Core.Helpers.Currency.FromMinorUnits(pi.Amount, pi.Currency), pi.Currency);

        // Find tenant via Stripe customer
        if (!string.IsNullOrEmpty(pi.CustomerId))
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.StripeCustomerId == pi.CustomerId);
            if (tenant != null)
            {
                _logger.LogInformation("Payment from tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);
            }
        }
    }

    private async Task HandlePaymentIntentFailed(Stripe.PaymentIntent pi)
    {
        _logger.LogWarning("PaymentIntent {PiId} FAILED, amount: {Amount} {Currency}, error: {Error}",
            pi.Id, Upkilo.Core.Helpers.Currency.FromMinorUnits(pi.Amount, pi.Currency), pi.Currency, pi.LastPaymentError?.Message ?? "unknown");
        await Task.CompletedTask;
    }

    // ── Customer Events ───────────────────────────────────────────────

    private async Task HandleCustomerUpdated(Stripe.Customer customer)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.StripeCustomerId == customer.Id);

        if (tenant != null)
        {
            // Sync email changes from Stripe back to tenant
            if (!string.IsNullOrEmpty(customer.Email) && tenant.Email != customer.Email)
            {
                _logger.LogInformation("Stripe customer email changed for tenant {TenantId}: {Old} → {New}",
                    tenant.Id, tenant.Email, customer.Email);
                // Note: not auto-updating email to avoid desync — log for manual review
            }
        }
    }

    // ── Refund Events ─────────────────────────────────────────────────

    private async Task HandleChargeRefunded(Stripe.Charge charge)
    {
        _logger.LogInformation(
            "Charge {ChargeId} refunded: {Amount} {Currency} (refunded: {RefundedAmount})",
            charge.Id,
            Upkilo.Core.Helpers.Currency.FromMinorUnits(charge.Amount, charge.Currency),
            charge.Currency,
            Upkilo.Core.Helpers.Currency.FromMinorUnits(charge.AmountRefunded, charge.Currency));

        // Track the refund against the payment record
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripeChargeId == charge.Id);

        if (payment != null)
        {
            payment.RefundAmount = Upkilo.Core.Helpers.Currency.FromMinorUnits(charge.AmountRefunded, charge.Currency);
            payment.RefundedAt = DateTime.UtcNow;
            payment.Status = charge.Refunded ? PaymentStatus.Refunded : PaymentStatus.Partial;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Refund tracked for payment {PaymentId}: {Amount} {Currency}",
                payment.Id, payment.RefundAmount, charge.Currency);
        }
        else if (!string.IsNullOrEmpty(charge.CustomerId))
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.StripeCustomerId == charge.CustomerId);
            if (tenant != null)
            {
                _logger.LogWarning("Refund for charge {ChargeId} has no matching Payment record — tenant {TenantId}",
                    charge.Id, tenant.Id);
            }
        }
    }

    private async Task HandleSubscriptionTrialWillEnd(Stripe.Subscription stripeSub)
    {
        _logger.LogInformation("Trial ending soon for subscription {SubId} (Ends at: {EndDate})",
            stripeSub.Id, stripeSub.TrialEnd);

        var localSub = await _context.Set<LocalSubscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (localSub == null) return;

        var tenant = await _context.Tenants.FindAsync(localSub.TenantId);
        if (tenant == null || string.IsNullOrEmpty(tenant.Email)) return;

        var trialEndDate = stripeSub.TrialEnd?.ToLocalTime().ToString("MMMM d, yyyy") ?? "soon";
        var daysLeft = stripeSub.TrialEnd.HasValue
            ? (int)Math.Ceiling((stripeSub.TrialEnd.Value - DateTime.UtcNow).TotalDays)
            : 3;

        await _emailService.SendSystemEmailAsync(
            tenant.Email,
            $"Your Upkilo trial ends in {daysLeft} day{(daysLeft == 1 ? "" : "s")}",
            $"Hi {tenant.Name},\n\n" +
            $"Your free trial ends on {trialEndDate}. After that, you'll need a paid plan to continue using Upkilo.\n\n" +
            $"Add your payment details now to keep your account and all your data:\n" +
            $"https://app.upkilo.com/settings/billing\n\n" +
            $"Questions? Reply to this email — we're here to help.\n\n" +
            $"The Upkilo Team"
        );

        _logger.LogInformation("Sent trial-ending email to tenant {TenantId} — {DaysLeft} days left", tenant.Id, daysLeft);
    }

    private async Task HandleInvoiceUpcoming(Stripe.Invoice invoice)
    {
        _logger.LogInformation("Upcoming invoice for customer {CustomerId}, amount: {Amount}", 
            invoice.CustomerId, Upkilo.Core.Helpers.Currency.FromMinorUnits(invoice.AmountDue, invoice.Currency));
        await Task.CompletedTask;
    }

    private async Task HandleInvoiceVoided(Stripe.Invoice invoice)
    {
        _logger.LogInformation("Invoice {InvoiceId} voided", invoice.Id);
        await Task.CompletedTask;
    }

    private async Task HandleInvoiceMarkedUncollectible(Stripe.Invoice invoice)
    {
        _logger.LogWarning("Invoice {InvoiceId} marked UNCOLLECTIBLE", invoice.Id);
        await Task.CompletedTask;
    }

    private async Task HandleChargeFailed(Stripe.Charge charge)
    {
        _logger.LogWarning("Charge {ChargeId} FAILED for customer {CustomerId}", charge.Id, charge.CustomerId);
        await Task.CompletedTask;
    }

    private async Task HandleChargeSucceeded(Stripe.Charge charge)
    {
        _logger.LogInformation("Charge {ChargeId} succeeded for customer {CustomerId}", charge.Id, charge.CustomerId);
        await Task.CompletedTask;
    }

    private async Task HandleCustomerDeleted(Stripe.Customer customer)
    {
        _logger.LogWarning("Stripe Customer {CustomerId} DELETED", customer.Id);
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.StripeCustomerId == customer.Id);
        if (tenant != null)
        {
            _logger.LogCritical("Tenant {TenantId} has their Stripe Customer deleted — subscriptions will fail!", tenant.Id);
        }
    }

    private async Task HandlePaymentIntentCreated(Stripe.PaymentIntent pi)
    {
        _logger.LogInformation("PaymentIntent created: {PiId}", pi.Id);
        await Task.CompletedTask;
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private string? GetSubscriptionId(Stripe.Invoice invoice)
    {
        if (invoice?.RawJObject == null) return null;
        var subToken = invoice.RawJObject["subscription"];
        if (subToken != null)
        {
            return subToken.Type == Newtonsoft.Json.Linq.JTokenType.String 
                ? subToken.ToString() 
                : subToken["id"]?.ToString();
        }
        return null;
    }

    private async Task<Guid?> GetTenantIdFromInvoice(Stripe.Invoice invoice)
    {
        // Strategy 1: Check metadata
        if (invoice.Metadata.TryGetValue("tenant_id", out var tid) && Guid.TryParse(tid, out var g1))
            return g1;

        // Strategy 2: Check subscription
        var subId = GetSubscriptionId(invoice);
        if (!string.IsNullOrEmpty(subId))
        {
            var sub = await _context.Set<LocalSubscription>().AsNoTracking()
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subId);
            if (sub != null) return sub.TenantId;
        }

        // Strategy 3: Check customer
        if (!string.IsNullOrEmpty(invoice.CustomerId))
        {
            var tenant = await _context.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.StripeCustomerId == invoice.CustomerId);
            if (tenant != null) return tenant.Id;
        }

        return null;
    }

    private InvoiceStatus MapStripeInvoiceStatus(string status)
    {
        return status switch
        {
            "draft" => InvoiceStatus.Draft,
            "open" => InvoiceStatus.Sent,
            "paid" => InvoiceStatus.Paid,
            "uncollectible" => InvoiceStatus.Uncollectible,
            "void" => InvoiceStatus.Void,
            _ => InvoiceStatus.Draft
        };
    }

    private static SubscriptionStatus MapStripeStatus(string status)
    {
        return status switch
        {
            "active"               => SubscriptionStatus.Active,
            "trialing"             => SubscriptionStatus.Trialing,
            "past_due"             => SubscriptionStatus.PastDue,
            "canceled" or "cancelled" => SubscriptionStatus.Cancelled,
            "unpaid"               => SubscriptionStatus.PastDue,       // consistent with BillingReconciliationJob
            "paused"               => SubscriptionStatus.Paused,
            "incomplete"           => SubscriptionStatus.PastDue,       // not yet collected, not yet cancelled
            "incomplete_expired"   => SubscriptionStatus.Cancelled,
            _                      => SubscriptionStatus.Suspended
        };
    }

    /// <summary>
    /// When a new subscription is created, check if the subscribing tenant arrived via a referral code.
    /// If so, mark the referral Rewarded and grant the referrer 1 month free via Stripe coupon.
    /// </summary>
    private async Task HandleReferralRewardFulfillment(Stripe.Subscription stripeSub)
    {
        var stripeCustomerId = stripeSub.CustomerId;
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.StripeCustomerId == stripeCustomerId && !t.IsDeleted);
        if (tenant == null) return;

        // Find a pending referral for this tenant where the referred email matches their email
        var referral = await _context.ReferralRecords
            .FirstOrDefaultAsync(r =>
                r.ReferredTenantId == tenant.Id &&
                r.Status == "SignedUp" &&
                !r.IsDeleted);

        // Also try matching by email if ReferredTenantId wasn't set during signup
        if (referral == null && !string.IsNullOrEmpty(tenant.Email))
        {
            referral = await _context.ReferralRecords
                .FirstOrDefaultAsync(r =>
                    r.ReferredEmail == tenant.Email &&
                    r.Status == "SignedUp" &&
                    !r.IsDeleted);
        }

        if (referral == null) return;

        // Apply 1-month free coupon to the referrer's Stripe subscription
        var referrerTenant = await _context.Tenants.FindAsync(referral.ReferrerId);
        if (referrerTenant?.StripeCustomerId != null)
        {
            try
            {
                var couponOptions = new Stripe.CouponCreateOptions
                {
                    Duration = "once",
                    PercentOff = 100, // 100% off for one billing cycle = 1 month free
                    Name = "Referral Reward – 1 Month Free",
                    MaxRedemptions = 1,
                    Metadata = new Dictionary<string, string>
                    {
                        ["referral_id"] = referral.Id.ToString(),
                        ["referred_tenant_id"] = tenant.Id.ToString()
                    }
                };
                var coupon = await new Stripe.CouponService().CreateAsync(couponOptions);

                var referrerSub = await _context.Subscriptions
                    .Where(s => s.TenantId == referral.ReferrerId && s.Status == SubscriptionStatus.Active)
                    .FirstOrDefaultAsync();

                if (referrerSub?.StripeSubscriptionId != null)
                {
                    // Stripe.net 50+: apply discount via Discounts array, not CouponId
                    await new Stripe.SubscriptionService().UpdateAsync(
                        referrerSub.StripeSubscriptionId,
                        new Stripe.SubscriptionUpdateOptions
                        {
                            Discounts = new List<Stripe.SubscriptionDiscountOptions>
                            {
                                new Stripe.SubscriptionDiscountOptions { Coupon = coupon.Id }
                            }
                        });
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Failed to apply referral coupon for referrer {ReferrerId}", referral.ReferrerId);
            }
        }

        referral.Status = "Rewarded";
        referral.QualifiedAt ??= DateTime.UtcNow;
        referral.RewardedAt = DateTime.UtcNow;
        referral.ReferredTenantId = tenant.Id;
        await _context.SaveChangesAsync();

        // Notify referrer they earned a free month
        if (referrerTenant != null && !string.IsNullOrEmpty(referrerTenant.Email))
        {
            await _emailService.SendSystemEmailAsync(
                referrerTenant.Email,
                "You earned 1 month free!",
                $"<h2>Your referral converted!</h2><p>Congratulations! A business you referred just activated a paid plan. We've applied <strong>1 month free</strong> to your next billing cycle as a thank-you.</p>");
        }

        _logger.LogInformation("Referral reward fulfilled: {ReferralId}, referrer {ReferrerId} gets 1 month free", referral.Id, referral.ReferrerId);
    }
}
