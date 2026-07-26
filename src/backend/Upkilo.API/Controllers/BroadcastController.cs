using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Attributes;
using Upkilo.API.Middleware;
using System.Text.Json;

namespace Upkilo.API.Controllers;

/// <summary>
/// Broadcast campaign controller — DB-persisted multi-channel campaigns with smart segmentation
/// and revenue attribution.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/broadcast")]
[Authorize]
[FeatureGuard("sms_reminders")]
public class BroadcastController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<BroadcastController> _logger;

    public BroadcastController(
        AppDbContext db,
        ITenantProvider tenantProvider,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<BroadcastController> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    // ─── GET /broadcast/campaigns ─────────────────────────────────────────────
    [HttpGet("campaigns")]
    public async Task<IActionResult> GetCampaigns(
        [FromQuery] string? status = null,
        [FromQuery] string? channel = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Campaigns
            .Where(c => c.TenantId == tenantId.Value && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(channel))
            query = query.Where(c => c.Type == channel);

        var total = await query.CountAsync();

        var campaigns = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CampaignSummaryDto
            {
                Id            = c.Id,
                Name          = c.Name,
                Channel       = c.Type,
                Subject       = c.Subject,
                Status        = c.Status,
                TargetSegment = c.TargetSegment ?? "all",
                ScheduledAt   = c.ScheduledAt,
                SentAt        = c.SentAt,
                SentCount     = c.SentCount,
                CreatedAt     = c.CreatedAt,
            })
            .ToListAsync();

        // Attach analytics for sent campaigns
        var sentIds = campaigns.Where(c => c.Status == "sent").Select(c => c.Id).ToList();
        var analytics = sentIds.Count > 0
            ? await _db.CampaignAnalytics
                .Where(a => a.TenantId == tenantId.Value && sentIds.Contains(a.CampaignId))
                .ToDictionaryAsync(a => a.CampaignId)
            : new Dictionary<Guid, CampaignAnalytics>();

        foreach (var c in campaigns.Where(c => analytics.ContainsKey(c.Id)))
        {
            var a = analytics[c.Id];
            c.Delivered    = a.DeliveredCount;
            c.Opened       = a.OpenedCount;
            c.Clicked      = a.ClickedCount;
            c.Unsubscribed = a.UnsubscribedCount;
            c.Revenue      = a.RevenueGenerated;
        }

        return Ok(ApiResponse<object>.Ok(new { data = campaigns, total, page, pageSize }));
    }

    // ─── GET /broadcast/campaigns/{id} ────────────────────────────────────────
    [HttpGet("campaigns/{id:guid}")]
    public async Task<IActionResult> GetCampaign(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (campaign is null) return NotFound(ApiResponse.Fail("Campaign not found."));

        var analytics = await _db.CampaignAnalytics
            .FirstOrDefaultAsync(a => a.CampaignId == id && a.TenantId == tenantId.Value);

        return Ok(ApiResponse<object>.Ok(new { campaign, analytics }));
    }

    // ─── POST /broadcast/campaigns ────────────────────────────────────────────
    [HttpPost("campaigns")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateBroadcastRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse.Fail("Campaign name is required."));

        if (request.Channel != "email" && request.Channel != "sms")
            return BadRequest(ApiResponse.Fail("Channel must be 'email' or 'sms'."));

        var campaign = new Campaign
        {
            Id             = Guid.NewGuid(),
            TenantId       = tenantId.Value,
            Name           = request.Name,
            Type           = request.Channel,
            Subject        = request.Subject,
            Content        = request.Channel == "email" ? request.Body : null,
            MessageBody    = request.Channel == "sms"   ? request.Body : null,
            Status         = "draft",
            TargetSegment  = request.TargetSegment ?? "all",
            AudienceFilters = request.SmartFilter is not null
                ? JsonSerializer.Serialize(request.SmartFilter)
                : null,
            ScheduledAt    = request.ScheduledAt,
            CreatedAt      = DateTime.UtcNow,
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Broadcast campaign created: {Id} - {Name}", campaign.Id, campaign.Name);
        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, ApiResponse<Campaign>.Ok(campaign, "Campaign created."));
    }

    // ─── PUT /broadcast/campaigns/{id} ────────────────────────────────────────
    [HttpPut("campaigns/{id:guid}")]
    public async Task<IActionResult> UpdateCampaign(Guid id, [FromBody] UpdateBroadcastRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (campaign is null) return NotFound(ApiResponse.Fail("Campaign not found."));
        if (campaign.Status is "sent" or "sending")
            return BadRequest(ApiResponse.Fail("Cannot edit a campaign that has been sent."));

        if (!string.IsNullOrWhiteSpace(request.Name))    campaign.Name    = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Subject)) campaign.Subject = request.Subject;
        if (request.Body is not null)
        {
            if (campaign.Type == "email") campaign.Content    = request.Body;
            else                          campaign.MessageBody = request.Body;
        }
        if (!string.IsNullOrWhiteSpace(request.TargetSegment)) campaign.TargetSegment = request.TargetSegment;
        if (request.ScheduledAt.HasValue) campaign.ScheduledAt = request.ScheduledAt;
        if (request.SmartFilter is not null)
            campaign.AudienceFilters = JsonSerializer.Serialize(request.SmartFilter);

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<Campaign>.Ok(campaign));
    }

    // ─── POST /broadcast/campaigns/{id}/send ──────────────────────────────────
    [HttpPost("campaigns/{id:guid}/send")]
    public async Task<IActionResult> SendCampaign(Guid id, [FromBody] SendBroadcastRequest? request = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (campaign is null) return NotFound(ApiResponse.Fail("Campaign not found."));
        if (campaign.Status is "sending" or "sent")
            return BadRequest(ApiResponse.Fail($"Campaign is already '{campaign.Status}'."));
        if (campaign.Status == "cancelled")
            return BadRequest(ApiResponse.Fail("Cannot send a cancelled campaign."));

        // Schedule for later?
        var scheduledAt = request?.ScheduledAt ?? campaign.ScheduledAt;
        if (scheduledAt.HasValue && scheduledAt.Value > DateTime.UtcNow)
        {
            campaign.Status     = "scheduled";
            campaign.ScheduledAt = scheduledAt;
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { scheduled = true, scheduledAt }, "Campaign scheduled."));
        }

        // Immediate send
        campaign.Status = "sending";
        await _db.SaveChangesAsync();

        try
        {
            var clients = await BuildAudienceQuery(tenantId.Value, campaign).Take(500).ToListAsync();

            int delivered = 0, failed = 0;

            if (campaign.Type == "email")
            {
                var subject = campaign.Subject ?? campaign.Name;
                var body    = campaign.Content ?? string.Empty;

                foreach (var client in clients)
                {
                    if (string.IsNullOrWhiteSpace(client.Email)) continue;
                    try
                    {
                        await _emailService.SendEmailAsync(client.Email, subject, body, isHtml: true);
                        delivered++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Broadcast email failed: {Email} campaign={Id}", client.Email, id);
                        failed++;
                    }
                }
            }
            else // sms
            {
                var body = campaign.MessageBody ?? string.Empty;
                foreach (var client in clients)
                {
                    if (string.IsNullOrWhiteSpace(client.Phone)) continue;
                    try
                    {
                        await _smsService.SendSmsAsync(tenantId.Value, client.Phone, body, client.Id);
                        delivered++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Broadcast SMS failed: {Phone} campaign={Id}", client.Phone, id);
                        failed++;
                    }
                }
            }

            campaign.SentCount = clients.Count;
            campaign.Status    = "sent";
            campaign.SentAt    = DateTime.UtcNow;

            // Upsert analytics record
            var analytics = await _db.CampaignAnalytics
                .FirstOrDefaultAsync(a => a.CampaignId == id && a.TenantId == tenantId.Value);

            if (analytics is null)
            {
                analytics = new CampaignAnalytics { Id = Guid.NewGuid(), TenantId = tenantId.Value, CampaignId = id };
                _db.CampaignAnalytics.Add(analytics);
            }

            analytics.SentCount       = clients.Count;
            analytics.DeliveredCount  = delivered;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Broadcast {Id} sent: {Delivered}/{Total}", id, delivered, clients.Count);

            return Ok(ApiResponse<object>.Ok(new
            {
                sent       = true,
                sentAt     = campaign.SentAt,
                total      = clients.Count,
                delivered,
                failed,
            }, "Campaign sent successfully."));
        }
        catch (Exception ex)
        {
            campaign.Status = "draft";
            await _db.SaveChangesAsync();
            _logger.LogError(ex, "Broadcast send failed for campaign {Id}", id);
            return StatusCode(500, ApiResponse.Fail("An error occurred while sending."));
        }
    }

    // ─── POST /broadcast/campaigns/{id}/cancel ────────────────────────────────
    [HttpPost("campaigns/{id:guid}/cancel")]
    public async Task<IActionResult> CancelCampaign(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (campaign is null) return NotFound(ApiResponse.Fail("Campaign not found."));
        if (campaign.Status is "sent" or "sending")
            return BadRequest(ApiResponse.Fail($"Cannot cancel a '{campaign.Status}' campaign."));

        campaign.Status = "cancelled";
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { cancelled = true }, "Campaign cancelled."));
    }

    // ─── DELETE /broadcast/campaigns/{id} ─────────────────────────────────────
    [HttpDelete("campaigns/{id:guid}")]
    public async Task<IActionResult> DeleteCampaign(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (campaign is null) return NotFound(ApiResponse.Fail("Campaign not found."));

        campaign.IsDeleted = true;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    // ─── GET /broadcast/campaigns/{id}/recipients ─────────────────────────────
    [HttpGet("campaigns/{id:guid}/recipients")]
    public async Task<IActionResult> GetRecipients(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (campaign is null) return NotFound(ApiResponse.Fail("Campaign not found."));

        pageSize = Math.Clamp(pageSize, 1, 100);
        var audience = BuildAudienceQuery(tenantId.Value, campaign);

        var total = await audience.CountAsync();
        var recipients = await audience
            .OrderBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new { c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.LastVisitAt, c.LifetimeValue })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new { data = recipients, total, page, pageSize }));
    }

    // ─── GET /broadcast/campaigns/{id}/revenue ────────────────────────────────
    /// <summary>
    /// Revenue attribution — bookings and payments made by campaign recipients
    /// within 30 days of the send date.
    /// </summary>
    [HttpGet("campaigns/{id:guid}/revenue")]
    public async Task<IActionResult> GetRevenue(Guid id, [FromQuery] int attributionDays = 30)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (campaign is null) return NotFound(ApiResponse.Fail("Campaign not found."));
        if (campaign.SentAt is null)
            return BadRequest(ApiResponse.Fail("Campaign has not been sent yet."));

        attributionDays = Math.Clamp(attributionDays, 1, 90);
        var window = campaign.SentAt.Value.AddDays(attributionDays);

        // Get recipient client IDs
        var recipientIds = await BuildAudienceQuery(tenantId.Value, campaign)
            .Select(c => c.Id)
            .ToListAsync();

        // Bookings made by recipients after the send date within the attribution window
        var bookings = await _db.Bookings
            .Where(b => b.TenantId == tenantId.Value
                     && b.ClientId.HasValue
                     && recipientIds.Contains(b.ClientId.Value)
                     && b.StartTime >= campaign.SentAt.Value
                     && b.StartTime <= window
                     && b.Status != BookingStatus.Cancelled)
            .CountAsync();

        // Payments by recipients in the attribution window
        var revenueResult = await _db.Payments
            .Where(p => p.TenantId == tenantId.Value
                     && p.ClientId.HasValue
                     && recipientIds.Contains(p.ClientId.Value)
                     && p.CreatedAt >= campaign.SentAt.Value
                     && p.CreatedAt <= window
                     && p.Status == PaymentStatus.Succeeded)
            .GroupBy(_ => 1)
            .Select(g => new { TotalRevenue = g.Sum(p => p.Amount), Count = g.Count() })
            .FirstOrDefaultAsync();

        var totalRevenue = revenueResult?.TotalRevenue ?? 0m;
        var paymentCount = revenueResult?.Count ?? 0;
        var conversionRate = recipientIds.Count > 0
            ? Math.Round((double)bookings / recipientIds.Count * 100, 2)
            : 0;

        // Update analytics record
        var analytics = await _db.CampaignAnalytics
            .FirstOrDefaultAsync(a => a.CampaignId == id && a.TenantId == tenantId.Value);
        if (analytics is not null)
        {
            analytics.ConversionCount  = bookings;
            analytics.RevenueGenerated = totalRevenue;
            await _db.SaveChangesAsync();
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            campaignId       = id,
            sentAt           = campaign.SentAt,
            attributionDays,
            attributionWindow = window,
            totalRecipients  = recipientIds.Count,
            bookingsCreated  = bookings,
            paymentsCount    = paymentCount,
            totalRevenue,
            conversionRate,
            revenuePerRecipient = recipientIds.Count > 0
                ? Math.Round(totalRevenue / recipientIds.Count, 2)
                : 0,
        }));
    }

    // ─── GET /broadcast/stats ─────────────────────────────────────────────────
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        var analytics = await _db.CampaignAnalytics
            .Where(a => a.TenantId == tenantId.Value)
            .ToListAsync();

        var campaigns = await _db.Campaigns
            .Where(c => c.TenantId == tenantId.Value && !c.IsDeleted)
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int totalSent       = analytics.Sum(a => a.SentCount);
        int totalDelivered  = analytics.Sum(a => a.DeliveredCount);
        int totalOpened     = analytics.Sum(a => a.OpenedCount);
        int totalClicked    = analytics.Sum(a => a.ClickedCount);
        int totalUnsub      = analytics.Sum(a => a.UnsubscribedCount);
        decimal totalRevenue = analytics.Sum(a => a.RevenueGenerated);

        return Ok(ApiResponse<object>.Ok(new
        {
            campaignsByStatus = campaigns,
            totalSent,
            totalDelivered,
            totalOpened,
            totalClicked,
            totalUnsubscribed = totalUnsub,
            totalRevenue,
            openRate      = totalSent > 0 ? Math.Round((double)totalOpened  / totalSent * 100, 2) : 0,
            clickRate     = totalSent > 0 ? Math.Round((double)totalClicked / totalSent * 100, 2) : 0,
            deliveryRate  = totalSent > 0 ? Math.Round((double)totalDelivered / totalSent * 100, 2) : 0,
        }));
    }

    // ─── GET /broadcast/segments ──────────────────────────────────────────────
    /// <summary>
    /// Returns all available smart segment types with live audience counts.
    /// </summary>
    [HttpGet("segments")]
    public async Task<IActionResult> GetSegments()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        var baseQuery = _db.Clients.Where(c => c.TenantId == tenantId.Value && c.MarketingConsent);
        var now       = DateTime.UtcNow;

        var segments = new[]
        {
            new { id = "all",              name = "Everyone",           description = "All clients with marketing consent",                        count = await baseQuery.CountAsync() },
            new { id = "active",           name = "Active Clients",     description = "Visited within the last 90 days",                           count = await baseQuery.Where(c => c.LastVisitAt >= now.AddDays(-90)).CountAsync() },
            new { id = "inactive",         name = "Lapsed Clients",     description = "No visit in 90+ days — perfect for win-back campaigns",     count = await baseQuery.Where(c => c.LastVisitAt == null || c.LastVisitAt < now.AddDays(-90)).CountAsync() },
            new { id = "win_back",         name = "Win-Back (60d)",     description = "No visit in 60–180 days — time-sensitive re-engagement",    count = await baseQuery.Where(c => c.LastVisitAt < now.AddDays(-60) && c.LastVisitAt >= now.AddDays(-180)).CountAsync() },
            new { id = "vip",              name = "VIP Clients",        description = "Lifetime value over $1,000",                                count = await baseQuery.Where(c => c.LifetimeValue >= 1000).CountAsync() },
            new { id = "high_spenders",    name = "High Spenders",      description = "Lifetime value over $500",                                  count = await baseQuery.Where(c => c.LifetimeValue >= 500).CountAsync() },
            new { id = "new_clients",      name = "New Clients",        description = "First visit within the last 30 days — nurture them early",  count = await baseQuery.Where(c => c.LastVisitAt >= now.AddDays(-30)).CountAsync() },
            new { id = "never_returned",   name = "One-Time Visitors",  description = "Visited exactly once, never came back",                     count = await baseQuery.Where(c => _db.Bookings.Count(b => b.TenantId == tenantId.Value && b.ClientId == c.Id) == 1 && c.LastVisitAt < now.AddDays(-30)).CountAsync() },
            new { id = "birthday_month",   name = "Birthday This Month", description = "Clients whose birthday is this month — offer a special gift", count = await baseQuery.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Month == now.Month).CountAsync() },
        };

        return Ok(ApiResponse<object>.Ok(new { segments }));
    }

    // ─── POST /broadcast/test-send ────────────────────────────────────────────
    [HttpPost("test-send")]
    public async Task<IActionResult> TestSend([FromBody] BroadcastTestSendRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.To))
            return BadRequest(ApiResponse.Fail("Recipient address is required."));

        try
        {
            if (request.Channel == "sms")
                await _smsService.SendSmsAsync(tenantId.Value, request.To, request.Body ?? "(empty)", null);
            else
                await _emailService.SendEmailAsync(request.To, request.Subject ?? "[Test] Broadcast Preview", request.Body ?? "(empty)", isHtml: true);

            return Ok(ApiResponse<object>.Ok(new { sent = true, to = request.To }, "Test message sent."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test send failed");
            return StatusCode(500, ApiResponse.Fail("Failed to send test message."));
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private IQueryable<Client> BuildAudienceQuery(Guid tenantId, Campaign campaign)
    {
        var now   = DateTime.UtcNow;
        var query = _db.Clients.Where(c => c.TenantId == tenantId && c.MarketingConsent);

        query = campaign.TargetSegment switch
        {
            "active"         => query.Where(c => c.LastVisitAt >= now.AddDays(-90)),
            "inactive"       => query.Where(c => c.LastVisitAt == null || c.LastVisitAt < now.AddDays(-90)),
            "win_back"       => query.Where(c => c.LastVisitAt < now.AddDays(-60) && c.LastVisitAt >= now.AddDays(-180)),
            "vip"            => query.Where(c => c.LifetimeValue >= 1000),
            "high_spenders"  => query.Where(c => c.LifetimeValue >= 500),
            "new_clients"    => query.Where(c => c.LastVisitAt >= now.AddDays(-30)),
            "never_returned" => query.Where(c => _db.Bookings.Count(b => b.TenantId == tenantId && b.ClientId == c.Id) == 1 && c.LastVisitAt < now.AddDays(-30)),
            "birthday_month" => query.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Month == now.Month),
            _                => query, // "all"
        };

        // Apply smart JSON filters if present (minLifetimeValue, lastVisitDays)
        if (!string.IsNullOrWhiteSpace(campaign.AudienceFilters))
        {
            try
            {
                var filter = JsonSerializer.Deserialize<SmartAudienceFilter>(campaign.AudienceFilters);
                if (filter is not null)
                {
                    if (filter.MinLifetimeValue.HasValue)
                        query = query.Where(c => c.LifetimeValue >= filter.MinLifetimeValue.Value);
                    if (filter.LastVisitDays.HasValue)
                        query = query.Where(c => c.LastVisitAt >= now.AddDays(-filter.LastVisitDays.Value));
                    if (filter.MaxLastVisitDays.HasValue)
                        query = query.Where(c => c.LastVisitAt == null || c.LastVisitAt < now.AddDays(-filter.MaxLastVisitDays.Value));
                }
            }
            catch { /* Ignore malformed filter JSON */ }
        }

        return query;
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public class CampaignSummaryDto
{
    public Guid      Id            { get; set; }
    public string    Name          { get; set; } = string.Empty;
    public string    Channel       { get; set; } = "email";
    public string?   Subject       { get; set; }
    public string    Status        { get; set; } = "draft";
    public string    TargetSegment { get; set; } = "all";
    public DateTime? ScheduledAt   { get; set; }
    public DateTime? SentAt        { get; set; }
    public int       SentCount     { get; set; }
    public int       Delivered     { get; set; }
    public int       Opened        { get; set; }
    public int       Clicked       { get; set; }
    public int       Unsubscribed  { get; set; }
    public decimal   Revenue       { get; set; }
    public DateTime  CreatedAt     { get; set; }
}

public class SmartAudienceFilter
{
    public decimal?  MinLifetimeValue  { get; set; }
    public int?      LastVisitDays     { get; set; }  // active within N days
    public int?      MaxLastVisitDays  { get; set; }  // not visited in N days
}

public record CreateBroadcastRequest(
    string           Name,
    string           Channel,
    string?          Subject       = null,
    string?          Body          = null,
    string?          TargetSegment = null,
    DateTime?        ScheduledAt   = null,
    SmartAudienceFilter? SmartFilter = null
);

public record UpdateBroadcastRequest(
    string?          Name          = null,
    string?          Subject       = null,
    string?          Body          = null,
    string?          TargetSegment = null,
    DateTime?        ScheduledAt   = null,
    SmartAudienceFilter? SmartFilter = null
);

public record SendBroadcastRequest(DateTime? ScheduledAt = null);

public record BroadcastTestSendRequest(
    string  To,
    string  Channel = "email",
    string? Subject = null,
    string? Body    = null
);
