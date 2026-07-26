using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Jobs;

/// <summary>
/// Sends onboarding email drip sequences to new tenants
/// Day 0: Welcome, Day 1: Setup Guide, Day 3: First Booking, Day 7: Tips, Day 14: Trial Ending
/// </summary>
public class OnboardingDripJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OnboardingDripJob> _logger;

    public OnboardingDripJob(AppDbContext context, IEmailService emailService, IConfiguration configuration, ILogger<OnboardingDripJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        // Find tenants in trial that haven't completed onboarding
        var trialTenants = await _context.Tenants
            .Where(t => t.Status == Upkilo.Core.Entities.TenantStatus.Active && t.TrialEndsAt > now)
            .ToListAsync();

        foreach (var tenant in trialTenants)
        {
            var owner = await _context.Users
                .Where(u => u.TenantId == tenant.Id && u.Role == Upkilo.Core.Entities.UserRole.Owner)
                .FirstOrDefaultAsync();

            if (owner == null) continue;

            var daysSinceCreation = (now - tenant.CreatedAt).TotalDays;

            try
            {
                if (daysSinceCreation < 1 && !HasSentDrip(tenant.Id, "welcome"))
                {
                    var appUrl = (_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
                    await SendDripEmail(owner.Email, owner.FirstName, "welcome",
                        "Welcome to Upkilo! 🚀",
                        $"<h2>Welcome aboard, {owner.FirstName}!</h2><p>You're all set to grow your business. Let's get you started with a quick setup.</p><p><a href='{appUrl}/onboarding'>Start Setup →</a></p>");
                    await RecordDrip(tenant.Id, "welcome");
                }
                else if (daysSinceCreation >= 1 && daysSinceCreation < 2 && !HasSentDrip(tenant.Id, "setup_guide"))
                {
                    var appUrl = (_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
                    await SendDripEmail(owner.Email, owner.FirstName, "setup_guide",
                        "Complete Your Setup in 5 Minutes",
                        $"<h2>Almost there, {owner.FirstName}!</h2><p>Add your services and working hours to start accepting bookings.</p><p><a href='{appUrl}/settings/services'>Add Services →</a></p>");
                    await RecordDrip(tenant.Id, "setup_guide");
                }
                else if (daysSinceCreation >= 3 && daysSinceCreation < 4 && !HasSentDrip(tenant.Id, "first_booking"))
                {
                    var appUrl = (_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
                    await SendDripEmail(owner.Email, owner.FirstName, "first_booking",
                        "Ready for Your First Booking?",
                        $"<h2>Share your booking page, {owner.FirstName}!</h2><p>Your booking page is live. Share it with your clients and start receiving bookings.</p><p><a href='{appUrl}/widget'>Get Booking Link →</a></p>");
                    await RecordDrip(tenant.Id, "first_booking");
                }
                else if (daysSinceCreation >= 7 && daysSinceCreation < 8 && !HasSentDrip(tenant.Id, "week_tips"))
                {
                    await SendDripEmail(owner.Email, owner.FirstName, "week_tips",
                        "Pro Tips to Grow Faster",
                        $"<h2>One week in, {owner.FirstName}! Here are some tips:</h2><ul><li>Set up automated reminders to reduce no-shows</li><li>Create email campaigns to bring clients back</li><li>Enable online payments to get paid faster</li></ul>");
                    await RecordDrip(tenant.Id, "week_tips");
                }
                else if (daysSinceCreation >= 12 && daysSinceCreation < 13 && !HasSentDrip(tenant.Id, "trial_ending"))
                {
                    var appUrl = (_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
                    var daysLeft = (int)(tenant.TrialEndsAt!.Value - now).TotalDays;
                    await SendDripEmail(owner.Email, owner.FirstName, "trial_ending",
                        $"Your Trial Ends in {daysLeft} Days",
                        $"<h2>Don't lose your data, {owner.FirstName}!</h2><p>Your free trial ends in {daysLeft} days. Upgrade now to keep all your data and features.</p><p><a href='{appUrl}/settings/billing'>Upgrade Now →</a></p>");
                    await RecordDrip(tenant.Id, "trial_ending");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send drip email for tenant {TenantId}", tenant.Id);
            }
        }
    }

    private async Task SendDripEmail(string email, string name, string type, string subject, string body)
    {
        await _emailService.SendSystemEmailAsync(email, subject + " - Upkilo", body);
        _logger.LogInformation("Drip email '{Type}' sent to {Email}", type, email);
    }

    private bool HasSentDrip(Guid tenantId, string dripType)
    {
        // Check if we've already sent this drip via tenant metadata
        var tenant = _context.Tenants.Find(tenantId);
        if (tenant?.Metadata == null) return false;
        return tenant.Metadata.ContainsKey($"drip_{dripType}_sent");
    }

    private async Task RecordDrip(Guid tenantId, string dripType)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant != null)
        {
            tenant.Metadata ??= new Dictionary<string, object>();
            tenant.Metadata[$"drip_{dripType}_sent"] = DateTime.UtcNow.ToString("O");
            await _context.SaveChangesAsync();
        }
    }
}
