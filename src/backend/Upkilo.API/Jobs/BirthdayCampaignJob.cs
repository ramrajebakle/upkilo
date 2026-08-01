using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Daily job to send birthday greetings to clients
/// </summary>
public class BirthdayCampaignJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<BirthdayCampaignJob> _logger;

    private const string DefaultBirthdayEmailSubject = "🎂 Happy Birthday from {BusinessName}!";
    private const string DefaultBirthdayEmailContent = @"
<h1>Happy Birthday, {{first_name}}! 🎉</h1>
<p>On behalf of everyone at {{business_name}}, we want to wish you an amazing birthday!</p>
<p>As a special gift, enjoy <strong>20% off</strong> your next visit when you use code: <strong>BDAY{{year}}</strong></p>
<p>We hope to see you soon!</p>
<p>Warm wishes,<br/>The {{business_name}} Team</p>
";
    private const string DefaultBirthdaySmsContent = "🎂 Happy Birthday {{first_name}}! Enjoy 20% off your next visit at {{business_name}} with code BDAY{{year}}. See you soon!";

    public BirthdayCampaignJob(
        AppDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<BirthdayCampaignJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    /// <summary>
    /// Execute birthday campaign for all tenants
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting birthday campaign job");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Find all clients with birthday today (match month and day, not year)
        var birthdayClients = await _context.Clients
            .Include(c => c.Tenant)
            .Where(c => c.DateOfBirth.HasValue &&
                        c.DateOfBirth.Value.Month == today.Month &&
                        c.DateOfBirth.Value.Day == today.Day)
            .ToListAsync();

        _logger.LogInformation("Found {Count} clients with birthdays today", birthdayClients.Count);

        int emailsSent = 0, smsSent = 0, failed = 0;

        foreach (var client in birthdayClients)
        {
            try
            {
                var businessName = client.Tenant?.Name ?? "Our Business";
                var year = DateTime.UtcNow.Year.ToString();

                // Send Email if consent given
                if (client.MarketingConsent && !string.IsNullOrEmpty(client.Email))
                {
                    var subject = PersonalizeContent(DefaultBirthdayEmailSubject, client, businessName, year);
                    var content = PersonalizeContent(DefaultBirthdayEmailContent, client, businessName, year);

                    await _emailService.SendSystemEmailAsync(client.Email, subject, content);
                    await LogCommunicationAsync(client, content, CommunicationType.Email);
                    emailsSent++;
                }

                // Send SMS if consent given
                if (client.SmsConsent && !string.IsNullOrEmpty(client.Phone))
                {
                    var content = PersonalizeContent(DefaultBirthdaySmsContent, client, businessName, year);
                    var result = await _smsService.SendSmsAsync(client.TenantId, client.Phone, content, client.Id);

                    if (result.Success)
                    {
                        await LogCommunicationAsync(client, content, CommunicationType.SMS);
                        smsSent++;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send birthday SMS to {ClientId}: {Error}", client.Id, result.Error);
                        failed++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send birthday message to client {ClientId}", client.Id);
                failed++;
            }
        }

        _logger.LogInformation("Birthday campaign completed. Emails: {Emails}, SMS: {Sms}, Failed: {Failed}",
            emailsSent, smsSent, failed);
    }

    private string PersonalizeContent(string content, Client client, string businessName, string year)
    {
        return content
            .Replace("{{first_name}}", client.FirstName ?? "Valued Customer")
            .Replace("{{last_name}}", client.LastName ?? "")
            .Replace("{{business_name}}", businessName)
            .Replace("{BusinessName}", businessName)
            .Replace("{{year}}", year);
    }

    private async Task LogCommunicationAsync(Client client, string content, CommunicationType type)
    {
        var log = new CommunicationLog
        {
            Id = Guid.NewGuid(),
            TenantId = client.TenantId,
            ClientId = client.Id,
            Type = type,
            Direction = CommunicationDirection.Outbound,
            Subject = "Birthday Greeting",
            Body = content,
            Status = CommunicationStatus.Sent,
            ReferenceId = "birthday_campaign",
            CreatedAt = DateTime.UtcNow
        };

        _context.CommunicationLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
