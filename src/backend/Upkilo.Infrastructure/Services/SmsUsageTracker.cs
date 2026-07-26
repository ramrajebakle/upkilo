using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Wraps ISmsService to track SMS usage per tenant against plan limits.
/// Records each SMS to Subscription.SmsUsed and reports overage to Stripe metered billing.
/// Register as a decorator over the real TwilioSmsService in DI.
/// </summary>
public class SmsUsageTracker : ISmsService
{
    private readonly ISmsService _inner;
    private readonly AppDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SmsUsageTracker> _logger;

    public SmsUsageTracker(
        ISmsService inner,
        AppDbContext context,
        ISubscriptionService subscriptionService,
        ILogger<SmsUsageTracker> logger)
    {
        _inner = inner;
        _context = context;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<SmsResult> SendSmsAsync(Guid tenantId, string toPhoneNumber, string message, Guid? clientId = null)
    {
        var result = await _inner.SendSmsAsync(tenantId, toPhoneNumber, message, clientId);
        if (result.Success)
            await TrackSmsUsedAsync(tenantId);
        return result;
    }

    public async Task<SmsResult> SendBookingConfirmationAsync(Booking booking)
    {
        var result = await _inner.SendBookingConfirmationAsync(booking);
        if (result.Success)
            await TrackSmsUsedAsync(booking.TenantId);
        return result;
    }

    public async Task<SmsResult> SendBookingReminderAsync(Booking booking)
    {
        var result = await _inner.SendBookingReminderAsync(booking);
        if (result.Success)
            await TrackSmsUsedAsync(booking.TenantId);
        return result;
    }

    public async Task<SmsResult> SendBookingCancellationAsync(Booking booking)
    {
        var result = await _inner.SendBookingCancellationAsync(booking);
        if (result.Success)
            await TrackSmsUsedAsync(booking.TenantId);
        return result;
    }

    public async Task<SmsResult> SendVerificationCodeAsync(Guid tenantId, string phoneNumber, string code)
    {
        // Verification codes don't count against monthly SMS quota
        return await _inner.SendVerificationCodeAsync(tenantId, phoneNumber, code);
    }

    private async Task TrackSmsUsedAsync(Guid tenantId)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.PricingPlan).ThenInclude(p => p!.FeatureMappings).ThenInclude(m => m.PricingFeature)
                .AsSplitQuery()
                .Where(s => s.TenantId == tenantId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing || s.Status == SubscriptionStatus.PastDue))
                .FirstOrDefaultAsync();

            if (subscription == null) return;

            subscription.SmsUsed++;
            await _context.SaveChangesAsync();

            // Determine plan SMS limit
            var smsMapping = subscription.PricingPlan?.FeatureMappings
                .FirstOrDefault(m => m.PricingFeature.Key == "monthly_sms_tier");
            int smsLimit = smsMapping?.NumericLimit ?? 0;
            if (smsLimit <= 0) return; // Unlimited or no plan

            // Report overage to Stripe if over the tier limit
            if (subscription.SmsUsed > smsLimit)
            {
                var smsOveragePriceId = subscription.PricingPlan?.StripeSmsOveragePriceId;

                if (smsOveragePriceId != null)
                    await _subscriptionService.ReportUsageAsync(tenantId, smsOveragePriceId, 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track SMS usage for tenant {TenantId}", tenantId);
            // Non-fatal: SMS was already sent, don't throw
        }
    }
}
