using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Text.Json;

namespace Upkilo.API.Jobs;

/// <summary>
/// Background job for processing and sending marketing campaigns
/// </summary>
public class CampaignSendJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<CampaignSendJob> _logger;

    public CampaignSendJob(
        AppDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        IWhatsAppService whatsAppService,
        ILogger<CampaignSendJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    /// <summary>
    /// Process and send a campaign to all recipients
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid campaignId, Guid tenantId)
    {
        _logger.LogInformation("Starting campaign send job for campaign {CampaignId}", campaignId);

        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.TenantId == tenantId);

        if (campaign == null)
        {
            _logger.LogError("Campaign {CampaignId} not found", campaignId);
            return;
        }

        try
        {
            // Get recipients based on audience type
            var recipients = await GetRecipientsAsync(campaign, tenantId);
            _logger.LogInformation("Found {Count} recipients for campaign {CampaignId}", recipients.Count, campaignId);

            if (recipients.Count == 0)
            {
                campaign.Status = "sent";
                campaign.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return;
            }

            int sentCount = 0, failedCount = 0;

            foreach (var client in recipients)
            {
                try
                {
                    if (campaign.Type == "sms")
                    {
                        if (!string.IsNullOrEmpty(client.Phone))
                        {
                            var content = PersonalizeContent(campaign.Content ?? "", client);
                            var result = await _smsService.SendSmsAsync(tenantId, client.Phone, content, client.Id);
                            if (result.Success)
                            {
                                await LogCommunicationAsync(client, campaign, content, "sms", true);
                                sentCount++;
                            }
                            else
                            {
                                failedCount++;
                            }
                        }
                    }
                    else if (campaign.Type == "whatsapp")
                    {
                        if (!string.IsNullOrEmpty(client.Phone))
                        {
                            var content = PersonalizeContent(campaign.Content ?? "", client);
                            await _whatsAppService.SendWhatsAppAsync(tenantId, client.Phone, content, client.Id);
                            await LogCommunicationAsync(client, campaign, content, "whatsapp", true);
                            sentCount++;
                        }
                    }
                    else // email
                    {
                        if (!string.IsNullOrEmpty(client.Email))
                        {
                            var content = PersonalizeContent(campaign.Content ?? "", client);
                            var subject = PersonalizeContent(campaign.Subject ?? "Message from us", client);
                            await _emailService.SendSystemEmailAsync(client.Email, subject, content);
                            await LogCommunicationAsync(client, campaign, content, "email", true);
                            sentCount++;
                        }
                    }

                    // Rate limiting: small delay to avoid flooding
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send campaign to client {ClientId}", client.Id);
                    failedCount++;
                }
            }

            // Update campaign status
            campaign.Status = "sent";
            campaign.SentAt = DateTime.UtcNow;

            // Update or create analytics
            var analytics = await _context.Set<CampaignAnalytics>()
                .FirstOrDefaultAsync(a => a.CampaignId == campaignId);

            if (analytics == null)
            {
                analytics = new CampaignAnalytics
                {
                    Id = Guid.NewGuid(),
                    CampaignId = campaignId,
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Set<CampaignAnalytics>().Add(analytics);
            }

            analytics.SentCount = sentCount;
            analytics.DeliveredCount = sentCount; // Assume delivered for now
            analytics.BouncedCount = failedCount;
            analytics.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Campaign {CampaignId} completed. Sent: {Sent}, Failed: {Failed}",
                campaignId, sentCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Campaign {CampaignId} failed", campaignId);
            campaign.Status = "failed";
            await _context.SaveChangesAsync();
            throw; // Let Hangfire retry
        }
    }

    private async Task<List<Client>> GetRecipientsAsync(Campaign campaign, Guid tenantId)
    {
        var query = _context.Clients.Where(c => c.TenantId == tenantId);

        switch (campaign.AudienceType?.ToLower())
        {
            case "all_clients":
                // All clients with marketing consent
                query = query.Where(c => c.MarketingConsent == true);
                break;

            case "segment":
                // Apply segment filters from JSONB
                if (!string.IsNullOrEmpty(campaign.AudienceFilters))
                {
                    try
                    {
                        var filters = JsonSerializer.Deserialize<AudienceFilter>(campaign.AudienceFilters);
                        if (filters != null)
                        {
                            if (filters.MinSpend.HasValue)
                                query = query.Where(c => c.LifetimeValue >= filters.MinSpend.Value);
                            if (filters.MinBookings.HasValue)
                                query = query.Where(c => c.TotalBookings >= filters.MinBookings.Value);
                            if (!string.IsNullOrEmpty(filters.LoyaltyTier))
                                query = query.Where(c => c.LoyaltyTier == filters.LoyaltyTier);
                            if (filters.Tags != null && filters.Tags.Any())
                                query = query.Where(c => c.Tags != null && filters.Tags.Any(t => c.Tags.Contains(t)));
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse audience filters for campaign {CampaignId}", campaign.Id);
                    }
                }
                break;

            default:
                // Default to all with consent
                query = query.Where(c => c.MarketingConsent == true);
                break;
        }

        // Apply DaysSinceLastVisit filter if AudienceFilters present
        if (!string.IsNullOrEmpty(campaign.AudienceFilters))
        {
            try
            {
                var filters = JsonSerializer.Deserialize<AudienceFilter>(campaign.AudienceFilters);
                if (filters?.DaysSinceLastVisit.HasValue == true)
                {
                    var cutoff = DateTime.UtcNow.AddDays(-filters.DaysSinceLastVisit.Value);
                    query = query.Where(c => c.LastVisitAt == null || c.LastVisitAt < cutoff);
                }
            }
            catch (JsonException) { /* already handled above */ }
        }

        // For SMS/WhatsApp campaigns, must have phone
        if (campaign.Type == "sms" || campaign.Type == "whatsapp")
        {
            query = query.Where(c => !string.IsNullOrEmpty(c.Phone) && c.SmsConsent == true);
        }
        // For email campaigns, must have email
        else
        {
            query = query.Where(c => !string.IsNullOrEmpty(c.Email));
        }

        return await query.Take(10000).ToListAsync(); // Limit for safety
    }

    private string PersonalizeContent(string content, Client client)
    {
        return content
            .Replace("{{first_name}}", client.FirstName ?? "")
            .Replace("{{last_name}}", client.LastName ?? "")
            .Replace("{{email}}", client.Email ?? "")
            .Replace("{{phone}}", client.Phone ?? "")
            .Replace("{{loyalty_points}}", client.LoyaltyPoints.ToString())
            .Replace("{{loyalty_tier}}", client.LoyaltyTier ?? "Bronze");
    }

    private async Task LogCommunicationAsync(Client client, Campaign campaign, string content, string type, bool success)
    {
        var log = new CommunicationLog
        {
            Id = Guid.NewGuid(),
            TenantId = client.TenantId,
            ClientId = client.Id,
            Type = type == "sms" ? CommunicationType.SMS : CommunicationType.Email,
            Direction = CommunicationDirection.Outbound,
            Subject = campaign.Subject,
            Body = content,
            Status = success ? CommunicationStatus.Sent : CommunicationStatus.Failed,
            ReferenceId = campaign.Id.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.CommunicationLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// DTO for audience filter parsing
/// </summary>
public class AudienceFilter
{
    public decimal? MinSpend { get; set; }
    public int? MinBookings { get; set; }
    public string? LoyaltyTier { get; set; }
    public List<string>? Tags { get; set; }
    public int? DaysSinceLastVisit { get; set; }
}
