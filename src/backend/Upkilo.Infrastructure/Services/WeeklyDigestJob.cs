using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Sends a "Your week in review" digest email every Monday at 08:00 UTC to all active paid tenants.
/// Includes Business Health Score, revenue trend, top client, and one AI recommendation.
/// </summary>
public class WeeklyDigestJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WeeklyDigestJob> _logger;

    public WeeklyDigestJob(IServiceProvider services, ILogger<WeeklyDigestJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            // Schedule for next Monday 08:00 UTC
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0 && now.Hour >= 8) daysUntilMonday = 7; // Already past today's window
            var nextRun = now.Date.AddDays(daysUntilMonday).AddHours(8);
            var delay = nextRun - now;
            if (delay <= TimeSpan.Zero) delay = TimeSpan.FromHours(24);

            _logger.LogInformation("[WeeklyDigestJob] Next digest run scheduled at {NextRun}", nextRun);
            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
                await SendDigestsAsync(stoppingToken);
        }
    }

    internal async Task SendDigestsAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var healthService = scope.ServiceProvider.GetRequiredService<BusinessHealthService>();

        // Target: active tenants on paid plans who have an email address
        var tenants = await context.Tenants
            .Where(t => t.IsActive &&
                        !t.IsDeleted &&
                        t.Email != null &&
                        t.SubscriptionTier != Upkilo.Core.Entities.SubscriptionTier.Free)
            .ToListAsync(ct);

        _logger.LogInformation("[WeeklyDigestJob] Sending weekly digest to {Count} tenants", tenants.Count);

        int sent = 0, failed = 0;
        // Process up to 10 tenants concurrently to avoid serial AI + email latency at scale
        using var semaphore = new SemaphoreSlim(10, 10);
        var tasks = tenants.Select(async tenant =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var report = await healthService.GenerateReportAsync(tenant.Id);

                var revenueArrow = report.RevenueTrendPercent >= 0 ? "↑" : "↓";
                var trendColor = report.RevenueTrendPercent >= 0 ? "#16a34a" : "#dc2626";

                var html = $"""
                    <div style="font-family: sans-serif; max-width: 600px; margin: 0 auto;">
                        <h1 style="color: #6366f1;">Your Week in Review — {DateTime.UtcNow.AddDays(-7):MMM d} to {DateTime.UtcNow:MMM d}</h1>

                        <div style="background: #f8fafc; border-radius: 12px; padding: 24px; margin: 20px 0;">
                            <h2 style="margin: 0;">Business Health Score:
                                <span style="color: #6366f1;">{report.Score}/100</span>
                                <span style="background: #6366f1; color: white; padding: 2px 8px; border-radius: 4px; font-size: 14px;">{report.Grade}</span>
                            </h2>
                            <p style="color: #64748b;">{report.AiNarrative}</p>
                        </div>

                        <table style="width: 100%; border-collapse: collapse;">
                            <tr>
                                <td style="padding: 12px; border-bottom: 1px solid #e2e8f0;">
                                    <strong>This Week's Revenue</strong>
                                </td>
                                <td style="padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: right;">
                                    ${report.ThisWeekRevenue:F2}
                                    <span style="color: {trendColor}; margin-left: 8px;">{revenueArrow} {Math.Abs(report.RevenueTrendPercent):F0}%</span>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 12px; border-bottom: 1px solid #e2e8f0;">
                                    <strong>New Clients This Month</strong>
                                </td>
                                <td style="padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: right;">{report.NewClientsThisMonth}</td>
                            </tr>
                            <tr>
                                <td style="padding: 12px; border-bottom: 1px solid #e2e8f0;">
                                    <strong>Client Retention Rate</strong>
                                </td>
                                <td style="padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: right;">{report.RetentionRatePercent:F0}%</td>
                            </tr>
                            <tr>
                                <td style="padding: 12px;">
                                    <strong>Next 7-Day Calendar Fill</strong>
                                </td>
                                <td style="padding: 12px; text-align: right;">{report.CalendarFillRatePercent:F0}%</td>
                            </tr>
                        </table>

                        <div style="background: #eff6ff; border-left: 4px solid #6366f1; padding: 16px; margin: 20px 0; border-radius: 4px;">
                            <strong>💡 This Week's Action:</strong> {report.TopAction}
                        </div>

                        <p style="text-align: center; margin-top: 24px;">
                            <a href="https://app.upkilo.com/dashboard"
                               style="background: #6366f1; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px;">
                               View Full Dashboard
                            </a>
                        </p>

                        <p style="color: #94a3b8; font-size: 12px; text-align: center;">
                            Upkilo — AI-Powered Booking Platform<br/>
                            <a href="https://app.upkilo.com/settings/notifications" style="color: #94a3b8;">Manage email preferences</a>
                        </p>
                    </div>
                    """;

                await emailService.SendSystemEmailAsync(
                    tenant.Email!,
                    $"Your week in review — Health Score {report.Score}/100 {report.Grade}",
                    html
                );

                Interlocked.Increment(ref sent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WeeklyDigestJob] Failed to send digest to tenant {TenantId}", tenant.Id);
                Interlocked.Increment(ref failed);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("[WeeklyDigestJob] Digest complete — {Sent} sent, {Failed} failed", sent, failed);
    }
}
