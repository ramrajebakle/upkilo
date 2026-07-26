using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Upkilo.API.Jobs;

/// <summary>
/// Hangfire job to send a sequence of onboarding emails to new users.
/// Runs daily to find users at specific milestones (Day 1, Day 3, Day 7).
/// </summary>
public class OnboardingEmailDripJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OnboardingEmailDripJob> _logger;

    public OnboardingEmailDripJob(AppDbContext context, IEmailService emailService, IConfiguration configuration, ILogger<OnboardingEmailDripJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting OnboardingEmailDripJob...");

        var now = DateTime.UtcNow;
        var todayStart = now.Date;

        // Day 1: Welcome and profile setup
        var appUrl = (_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
        var day1Start = todayStart.AddDays(-1);
        var day1End = day1Start.AddDays(1);
        await SendDripAsync(day1Start, day1End, "Day 1 Drip", "Welcome to Upkilo! Let's get your profile set up.", 
            $"Hi {{0}},<br/><br/>Welcome to Upkilo! The first step to scaling your business is completing your profile and setting up your first service. <a href='{appUrl}/settings'>Click here to get started!</a>");

        // Day 3: Calendar sync
        var day3Start = todayStart.AddDays(-3);
        var day3End = day3Start.AddDays(1);
        await SendDripAsync(day3Start, day3End, "Day 3 Drip", "Sync your calendar to prevent double bookings",
            $"Hi {{0}},<br/><br/>Did you know you can sync your Google or Outlook calendar? It's the best way to prevent double bookings and keep your schedule clean. <a href='{appUrl}/calendar/sync'>Connect your calendar now.</a>");

        // Day 7: Check-in / Need help?
        var day7Start = todayStart.AddDays(-7);
        var day7End = day7Start.AddDays(1);
        await SendDripAsync(day7Start, day7End, "Day 7 Drip", "How is your first week going?",
            "Hi {0},<br/><br/>It's been a week since you joined Upkilo! How are things going? If you ever need help, feel free to reply to this email or use our live chat inside the app.");

        // Day 14: AI spotlight — show value of AI Copilot
        var day14Start = todayStart.AddDays(-14);
        var day14End = day14Start.AddDays(1);
        await SendDripAsync(day14Start, day14End, "Day 14 Drip",
            "Did you know Upkilo's AI Copilot can write client messages for you?",
            $"Hi {{0}},<br/><br/>Quick tip: our AI Copilot can draft follow-up messages, promotional texts, and re-engagement emails in seconds — based on each client's history with you.<br/><br/>" +
            $"Owners using AI Copilot save <strong>2–3 hours per week</strong> on client communication.<br/><br/>" +
            $"<a href='{appUrl}/ai'>Try AI Copilot now →</a><br/><br/>The Upkilo Team");

        // Day 21: AI workflows — proactive revenue nudge
        var day21Start = todayStart.AddDays(-21);
        var day21End = day21Start.AddDays(1);
        await SendDripAsync(day21Start, day21End, "Day 21 Drip",
            "Your AI is quietly recovering lost revenue — here's how",
            $"Hi {{0}},<br/><br/>Every week, Upkilo's AI scans your client list for people who haven't booked in 45+ days and automatically drafts a personalized win-back message.<br/><br/>" +
            $"On average, businesses recover <strong>12% of lapsed clients</strong> in their first month of using this feature.<br/><br/>" +
            $"Make sure your AI retention is switched on: <a href='{appUrl}/ai/automations'>Enable AI Automations →</a><br/><br/>The Upkilo Team");

        // Day 28: Upgrade nudge — annual billing savings
        var day28Start = todayStart.AddDays(-28);
        var day28End = day28Start.AddDays(1);
        await SendDripAsync(day28Start, day28End, "Day 28 Drip",
            "Save 21% by switching to annual billing today",
            $"Hi {{0}},<br/><br/>You've been using Upkilo for almost a month — amazing! If you're happy with the results, switching to annual billing saves you 21% compared to paying monthly.<br/><br/>" +
            $"That's up to <strong>$238 back in your pocket</strong> every year.<br/><br/>" +
            $"<a href='{appUrl}/settings/billing?upgrade=annual'>Switch to Annual →</a><br/><br/>" +
            $"P.S. You also get priority support and 3 months of our AI Workflow add-on free when you switch this week.<br/><br/>The Upkilo Team");

        _logger.LogInformation("Completed OnboardingEmailDripJob.");
    }

    private async Task SendDripAsync(DateTime start, DateTime end, string dripName, string subject, string bodyTemplate)
    {
        var newUsers = await _context.Users
            .Where(u => u.CreatedAt >= start && u.CreatedAt < end && !u.IsDeleted)
            .Select(u => new { u.Id, u.FirstName, u.Email })
            .ToListAsync();

        foreach (var user in newUsers)
        {
            try
            {
                var body = string.Format(bodyTemplate, user.FirstName);
                await _emailService.SendSystemEmailAsync(user.Email, subject, body);
                _logger.LogInformation("Sent {DripName} to {Email}", dripName, user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {DripName} to {Email}", dripName, user.Email);
            }
        }
    }
}
