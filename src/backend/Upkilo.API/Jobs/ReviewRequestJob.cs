using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Automated job to send review requests after completed bookings
/// </summary>
public class ReviewRequestJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<ReviewRequestJob> _logger;

    private const string DefaultEmailSubject = "How was your visit to {BusinessName}?";
    private const string DefaultEmailContent = @"
<h1>Hi {{first_name}}, thanks for visiting!</h1>
<p>We hope you had a great experience at {{business_name}}.</p>
<p>Would you mind taking a moment to leave us a review? Your feedback helps us improve and helps others discover us!</p>
<p style='margin: 20px 0;'>
    <a href='{{google_review_link}}' style='background: #4285f4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin-right: 10px;'>Review on Google</a>
    <a href='{{yelp_review_link}}' style='background: #d32323; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px;'>Review on Yelp</a>
</p>
<p>Thank you for your support!</p>
<p>The {{business_name}} Team</p>
";
    private const string DefaultSmsContent = "Thanks for visiting {{business_name}}! Would you leave us a quick review? {{google_review_link}} 🙏";

    public ReviewRequestJob(
        AppDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<ReviewRequestJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    /// <summary>
    /// Find completed bookings and send review requests
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting review request job");

        // Find bookings completed in the last 24-48 hours that haven't had review requests
        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(-48);
        var windowEnd = now.AddHours(-2); // At least 2 hours after completion

        // Get completed bookings
        var completedBookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Tenant)
            .Where(b => b.Status == BookingStatus.Completed &&
                        b.EndTime >= windowStart &&
                        b.EndTime <= windowEnd &&
                        b.ReviewRequestSentAt == null && // Skip if already sent (fast path)
                        b.Client != null &&
                        b.ClientId.HasValue)
            .ToListAsync();

        _logger.LogInformation("Found {Count} completed bookings for review requests", completedBookings.Count);

        int emailsSent = 0, smsSent = 0;

        foreach (var booking in completedBookings)
        {
            try
            {
                // Check if review request already sent for this booking
                var alreadySent = await _context.Set<CommunicationLog>()
                    .AnyAsync(c => c.ExternalReference == $"review_request_{booking.Id}");

                if (alreadySent) continue;

                var client = booking.Client!;
                var tenant = booking.Tenant;
                var businessName = tenant?.Name ?? "Our Business";

                // Get review links from tenant settings or use placeholders
                var googleReviewLink = tenant?.Settings.GetValueOrDefault("google_review_link")?.ToString()
                    ?? $"https://search.google.com/local/writereview?placeid=YOUR_PLACE_ID";
                var yelpReviewLink = tenant?.Settings.GetValueOrDefault("yelp_review_link")?.ToString()
                    ?? $"https://www.yelp.com/writeareview/biz/YOUR_BIZ_ID";

                // Send Email if consent given
                if (client.MarketingConsent && !string.IsNullOrEmpty(client.Email))
                {
                    var subject = PersonalizeContent(DefaultEmailSubject, client, businessName, googleReviewLink, yelpReviewLink);
                    var content = PersonalizeContent(DefaultEmailContent, client, businessName, googleReviewLink, yelpReviewLink);

                    await _emailService.SendSystemEmailAsync(client.Email, subject, content);
                    await LogCommunicationAsync(client, booking, content, CommunicationType.Email);
                    booking.ReviewRequestSentAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    emailsSent++;
                }
                // Send SMS as alternative if no email consent but SMS consent
                else if (client.SmsConsent && !string.IsNullOrEmpty(client.Phone))
                {
                    var content = PersonalizeContent(DefaultSmsContent, client, businessName, googleReviewLink, yelpReviewLink);

                    var result = await _smsService.SendSmsAsync(client.TenantId, client.Phone, content, client.Id);
                    if (result.Success)
                    {
                        await LogCommunicationAsync(client, booking, content, CommunicationType.SMS);
                        booking.ReviewRequestSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        smsSent++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send review request for booking {BookingId}", booking.Id);
            }
        }

        _logger.LogInformation("Review request job completed. Emails: {Emails}, SMS: {Sms}", emailsSent, smsSent);
    }

    private string PersonalizeContent(string content, Client client, string businessName, string googleLink, string yelpLink)
    {
        return content
            .Replace("{{first_name}}", client.FirstName ?? "Valued Customer")
            .Replace("{{last_name}}", client.LastName ?? "")
            .Replace("{{business_name}}", businessName)
            .Replace("{BusinessName}", businessName)
            .Replace("{{google_review_link}}", googleLink)
            .Replace("{{yelp_review_link}}", yelpLink);
    }

    private async Task LogCommunicationAsync(Client client, Booking booking, string content, CommunicationType type)
    {
        var log = new CommunicationLog
        {
            Id = Guid.NewGuid(),
            TenantId = client.TenantId,
            ClientId = client.Id,
            Type = type,
            Direction = CommunicationDirection.Outbound,
            Subject = "Review Request",
            Body = content,
            Status = CommunicationStatus.Sent,
            ReferenceId = $"review_request_{booking.Id}", // Use this to prevent duplicates
            CreatedAt = DateTime.UtcNow
        };

        _context.CommunicationLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
