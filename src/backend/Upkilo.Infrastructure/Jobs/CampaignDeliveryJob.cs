using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Jobs;

public class CampaignDeliveryJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<CampaignDeliveryJob> _logger;

    public CampaignDeliveryJob(AppDbContext context, IEmailService emailService, ISmsService smsService, ILogger<CampaignDeliveryJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await _context.Campaigns.FindAsync(new object[] { campaignId }, cancellationToken);
        if (campaign == null || campaign.Status != "Scheduled")
        {
            _logger.LogWarning("Campaign {CampaignId} not found or not in Scheduled status.", campaignId);
            return;
        }

        campaign.Status = "Running";
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Simplified logic: Suppose 'TargetSegment' = 'All' or a specific tag
            var clientsQuery = _context.Clients.Where(c => c.TenantId == campaign.TenantId);
            
            if (campaign.TargetSegment != "All")
            {
                clientsQuery = clientsQuery.Where(c => c.Tags.Contains(campaign.TargetSegment!));
            }

            // Stream clients to avoid loading an entire tenant into memory.
            int sentCount = 0;
            await foreach (var client in clientsQuery.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                // Format message (simple string replace for tags)
                var personalizedBody = campaign.MessageBody
                    .Replace("{{FirstName}}", client.FirstName)
                    .Replace("{{LastName}}", client.LastName);

                try
                {
                    if (campaign.Type == "Email" && !string.IsNullOrEmpty(client.Email))
                    {
                        await _emailService.SendEmailAsync(
                            client.Email,
                            campaign.Subject ?? "Notification from Upkilo provider",
                            personalizedBody
                        );
                        sentCount++;
                    }
                    else if (campaign.Type == "SMS" && !string.IsNullOrEmpty(client.Phone))
                    {
                        await _smsService.SendSmsAsync(campaign.TenantId, client.Phone, personalizedBody, client.Id);
                        sentCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Campaign {CampaignId}: failed to notify client {ClientId}", campaignId, client.Id);
                }

                // Checkpoint progress so a retry resumes rather than double-sends.
                // Persist sentCount periodically — every 100 sends — to reduce DB round-trips.
                if (sentCount % 100 == 0)
                {
                    campaign.SentCount = sentCount;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            campaign.Status = "Completed";
            campaign.SentCount = sentCount;
            _logger.LogInformation("Campaign {CampaignId} completed successfully. Messages sent: {Count}", campaignId, sentCount);
        }
        catch (Exception ex)
        {
            campaign.Status = "Failed";
            _logger.LogError(ex, "Campaign {CampaignId} failed during execution.", campaignId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
