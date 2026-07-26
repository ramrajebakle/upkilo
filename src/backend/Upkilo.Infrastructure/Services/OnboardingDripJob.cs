using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Background job that sends onboarding drip emails to tenants
/// who haven't completed setup after 7 days.
/// Runs once daily, skips already-emailed tenants.
/// </summary>
public class OnboardingDripJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OnboardingDripJob> _logger;

    public OnboardingDripJob(IServiceScopeFactory scopeFactory, ILogger<OnboardingDripJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnboardingDripJob encountered an error");
            }

            // Run once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var fourteenDaysAgo = DateTime.UtcNow.AddDays(-14);

        // Find tenants created 7-14 days ago with incomplete onboarding who haven't been nudged
        var candidates = await context.Set<TenantOnboardingProgress>()
            .Include(p => p.Tenant)
            .Where(p =>
                p.CreatedAt >= fourteenDaysAgo &&
                p.CreatedAt <= sevenDaysAgo &&
                !p.IsDismissed &&
                p.DripEmailSentAt == null &&
                // At least one step incomplete
                (!p.BusinessProfileCompleted ||
                 !p.ServicesAdded ||
                 !p.StaffAdded ||
                 !p.FirstBookingCreated))
            .ToListAsync(ct);

        _logger.LogInformation("OnboardingDripJob: {Count} tenants to nudge", candidates.Count);

        foreach (var progress in candidates)
        {
            var tenant = progress.Tenant;
            if (tenant == null || string.IsNullOrEmpty(tenant.Email)) continue;

            var completedCount = CountCompleted(progress);
            var totalSteps = 8;
            var pct = (int)((completedCount / (double)totalSteps) * 100);

            var nextStepHint = GetNextStepHint(progress);

            try
            {
                await emailService.SendSystemEmailAsync(
                    tenant.Email,
                    "You're almost there! Finish setting up Upkilo",
                    $@"<h2>Hey {tenant.Name ?? "there"}! 👋</h2>
                       <p>You're <strong>{pct}% done</strong> setting up your Upkilo account — just a few steps left.</p>
                       <p>Your next step: <strong>{nextStepHint}</strong></p>
                       <p>Completing setup takes less than 5 minutes and unlocks your first bookings.</p>
                       <p><a href='https://app.upkilo.com/onboarding' style='background:#6366f1;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;'>Continue Setup →</a></p>
                       <p style='color:#6b7280;font-size:12px;margin-top:24px;'>You're receiving this because you signed up for Upkilo. <a href='https://app.upkilo.com/settings/notifications'>Manage preferences</a></p>");

                progress.DripEmailSentAt = DateTime.UtcNow;

                _logger.LogInformation("Drip email sent to tenant {TenantId}", progress.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send drip email to tenant {TenantId}", progress.TenantId);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private static int CountCompleted(TenantOnboardingProgress p) =>
        (p.BusinessProfileCompleted ? 1 : 0) +
        (p.WorkingHoursCompleted ? 1 : 0) +
        (p.ServicesAdded ? 1 : 0) +
        (p.StaffAdded ? 1 : 0) +
        (p.BookingPageCustomized ? 1 : 0) +
        (p.PaymentSetupCompleted ? 1 : 0) +
        (p.FirstBookingCreated ? 1 : 0) +
        (p.ClientsImported ? 1 : 0);

    private static string GetNextStepHint(TenantOnboardingProgress p)
    {
        if (!p.BusinessProfileCompleted) return "Complete your business profile";
        if (!p.ServicesAdded) return "Add your first service";
        if (!p.StaffAdded) return "Add a staff member";
        if (!p.WorkingHoursCompleted) return "Set your working hours";
        if (!p.BookingPageCustomized) return "Customize your booking page";
        if (!p.PaymentSetupCompleted) return "Connect Stripe to accept payments";
        if (!p.FirstBookingCreated) return "Create your first test booking";
        return "Import your existing clients";
    }
}
