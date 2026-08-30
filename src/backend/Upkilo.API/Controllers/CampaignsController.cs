using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Jobs;
using Upkilo.API.Middleware;
using Hangfire;
using System.Text.Json;
using Upkilo.API.Attributes;

namespace Upkilo.API.Controllers;

/// <summary>
/// Marketing campaigns controller for email and promotional campaigns
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ILogger<CampaignsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEventService _eventService;
    private readonly ICampaignAnalyticsService _analyticsService;
    private readonly IEmailService _emailService;
    private readonly ICopywritingAgent _copywritingAgent;

    public CampaignsController(
        ILogger<CampaignsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IEventService eventService,
        ICampaignAnalyticsService analyticsService,
        IEmailService emailService,
        ICopywritingAgent copywritingAgent)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _eventService = eventService;
        _analyticsService = analyticsService;
        _emailService = emailService;
        _copywritingAgent = copywritingAgent;
    }

    /// <summary>
    /// Generate AI content for a campaign
    /// </summary>
    [HttpPost("generate-ai-content")]
    [RequiresFeature(FeatureKeys.AiCopilot)]
    public async Task<IActionResult> GenerateAIContent([FromBody] GenerateContentRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        try
        {
            string result;
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value);
            var businessName = tenant?.BusinessName ?? "Business";
            var serviceName = request.Topic;
            var targetAudience = request.Context ?? "General clients";
            var goal = request.Topic;

            switch (request.Type.ToLower())
            {
                case "email":
                    result = await _copywritingAgent.GenerateEmailContentAsync(tenantId.Value, businessName, serviceName, targetAudience, goal);
                    break;
                case "sms":
                    result = await _copywritingAgent.GenerateSmsContentAsync(tenantId.Value, businessName, serviceName, goal);
                    break;
                case "social":
                    var platform = request.Context ?? "Instagram";
                    result = await _copywritingAgent.GenerateSocialMediaPostAsync(tenantId.Value, platform, businessName, serviceName, request.Topic);
                    break;
                default:
                    return BadRequest("Invalid content type");
            }

            return Ok(new { content = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI content");
            return StatusCode(500, "AI generation failed");
        }
    }

    /// <summary>
    /// Get all campaigns
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCampaigns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Campaigns.Where(c => c.TenantId == tenantId).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(c => c.Type == type);

        var total = await query.CountAsync();
        var campaigns = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = campaigns,
            total,
            page,
            pageSize
        });
    }

    /// <summary>
    /// Get campaign by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCampaign(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaign = await _context.Campaigns.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (campaign == null) return NotFound();

        // Join with analytics if needed, but usually analytics is a separate endpoint
        return Ok(campaign);
    }

    /// <summary>
    /// Create a new campaign
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        string subject = request.Subject ?? string.Empty;
        string content = request.Content ?? string.Empty;

        if (request.TemplateId.HasValue)
        {
            var template = await _context.MarketingTemplates.FirstOrDefaultAsync(x => x.Id == request.TemplateId.Value);
            if (template != null)
            {
                subject = subject ?? template.Subject;
                content = content ?? template.Content;
            }
        }

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            Type = request.Type,
            Subject = subject,
            Content = content,
            TemplateId = request.TemplateId,
            Status = "draft",
            CreatedAt = DateTime.UtcNow
        };

        _context.Campaigns.Add(campaign);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Campaign created: {CampaignId} - {Name}", campaign.Id, campaign.Name);

        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
    }

    /// <summary>
    /// Update a campaign
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCampaign(Guid id, [FromBody] UpdateCampaignRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaign = await _context.Campaigns.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (campaign == null) return NotFound();

        if (request.Name != null) campaign.Name = request.Name;
        if (request.Subject != null) campaign.Subject = request.Subject;
        if (request.Preheader != null) campaign.Preheader = request.Preheader;
        if (request.Content != null) campaign.Content = request.Content;
        if (request.AudienceType != null) campaign.AudienceType = request.AudienceType;
        if (request.AudienceFilters != null)
            campaign.AudienceFilters = JsonSerializer.Serialize(request.AudienceFilters);

        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Campaign updated: {CampaignId}", id);
        return Ok(campaign);
    }

    /// <summary>
    /// Delete a campaign
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCampaign(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaign = await _context.Campaigns.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (campaign == null) return NotFound();

        _context.Campaigns.Remove(campaign);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Campaign deleted: {CampaignId}", id);
        return NoContent();
    }

    /// <summary>
    /// Send a campaign immediately
    /// </summary>
    [HttpPost("{id}/send")]
    public async Task<IActionResult> SendCampaign(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (campaign == null) return NotFound();

        var audienceCount = await _context.Clients
            .CountAsync(c => c.TenantId == tenantId && c.MarketingConsent);

        const int MaxAudiencePerSend = 5000;
        if (audienceCount > MaxAudiencePerSend)
        {
            _logger.LogWarning("Campaign {CampaignId} blocked: audience {Count} exceeds limit {Limit}", id, audienceCount, MaxAudiencePerSend);
            return BadRequest(new
            {
                error = "Audience too large",
                message = $"Campaign audience ({audienceCount:N0} contacts) exceeds the {MaxAudiencePerSend:N0} contact limit. Segment your audience or contact support.",
                audienceCount,
                limit = MaxAudiencePerSend
            });
        }

        campaign.Status = "sending";
        await _context.SaveChangesAsync();

        _logger.LogInformation("Campaign {CampaignId} queued for {Count} recipients", id, audienceCount);
        BackgroundJob.Enqueue<CampaignSendJob>(job => job.ExecuteAsync(id, tenantId.Value));

        return Ok(new
        {
            success = true,
            message = "Campaign is being processed",
            audienceCount
        });
    }

    /// <summary>
    /// Schedule a campaign
    /// </summary>
    [HttpPost("{id}/schedule")]
    public async Task<IActionResult> ScheduleCampaign(Guid id, [FromBody] ScheduleCampaignRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaign = await _context.Campaigns.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (campaign == null) return NotFound();

        campaign.Status = "scheduled";
        campaign.ScheduledAt = request.ScheduledAt;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Campaign scheduled: {CampaignId} for {ScheduledAt}", id, request.ScheduledAt);

        return Ok(new
        {
            success = true,
            scheduledAt = campaign.ScheduledAt
        });
    }

    /// <summary>
    /// Send test email
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> SendTestCampaign(
        Guid id,
        [FromBody] SendTestRequest request,
        [FromServices] IDistributedCache cache)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var rateLimitKey = $"test_email_rate:{tenantId}";
        var countBytes = await cache.GetAsync(rateLimitKey);
        var currentCount = countBytes != null ? int.Parse(Encoding.UTF8.GetString(countBytes)) : 0;
        if (currentCount >= 10)
            return StatusCode(429, new { error = "Too many test emails. Wait 10 minutes before sending another." });

        await cache.SetAsync(
            rateLimitKey,
            Encoding.UTF8.GetBytes((currentCount + 1).ToString()),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

        var campaign = await _context.Campaigns.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (campaign == null) return NotFound();

        _logger.LogInformation("Test campaign sent to {Email}", request.Email);

        await _emailService.SendSystemEmailAsync(
            request.Email,
            campaign.Subject ?? "Test Campaign",
            campaign.Content ?? "Test Content"
        );

        return Ok(new
        {
            success = true,
            message = $"Test email sent to {request.Email}"
        });
    }

    /// <summary>
    /// Duplicate a campaign
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> DuplicateCampaign(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var original = await _context.Campaigns.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (original == null) return NotFound();

        var duplicate = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = $"{original.Name} (Copy)",
            Type = original.Type,
            Subject = original.Subject,
            Preheader = original.Preheader,
            Content = original.Content,
            TemplateId = original.TemplateId,
            AudienceType = original.AudienceType,
            AudienceFilters = original.AudienceFilters,
            Status = "draft",
            CreatedAt = DateTime.UtcNow
        };

        _context.Campaigns.Add(duplicate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Campaign duplicated: {OriginalId} -> {NewId}", id, duplicate.Id);

        return Ok(duplicate);
    }

    /// <summary>
    /// Get campaign analytics
    /// </summary>
    [HttpGet("{id}/analytics")]
    public async Task<IActionResult> GetCampaignAnalytics(Guid id)
    {
        var analytics = await _analyticsService.GetAnalyticsAsync(id);
        if (analytics == null) return NotFound();

        return Ok(analytics);
    }

    /// <summary>
    /// Get campaign timeline data for charts
    /// </summary>
    [HttpGet("{id}/timeline")]
    public async Task<IActionResult> GetCampaignTimeline(Guid id, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var startDate = start ?? DateTime.UtcNow.AddDays(-7);
        var endDate = end ?? DateTime.UtcNow;

        var timeline = await _analyticsService.GetTimelineDataAsync(id, startDate, endDate);
        return Ok(timeline);
    }

    /// <summary>
    /// Get detailed analytics for a campaign
    /// </summary>
    [HttpGet("{id}/analytics/detailed")]
    public async Task<IActionResult> GetAnalyticsDetailed(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (campaign == null) return NotFound();

        var logs = await _context.CommunicationLogs
            .Where(l => l.ReferenceId == id.ToString() && l.TenantId == tenantId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(1000)
            .ToListAsync();

        var summary = await _analyticsService.GetAnalyticsAsync(id);

        return Ok(new
        {
            campaignName = campaign.Name,
            summary,
            recentLogs = logs.Select(l => new
            {
                l.Id,
                l.Status,
                l.CreatedAt,
                l.Type,
                l.ClientId,
                l.ErrorMessage
            })
        });
    }

    /// <summary>
    /// Get email templates
    /// </summary>
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] string? category = null, [FromQuery] string? type = null)
    {
        var query = _context.MarketingTemplates.AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.Type == type);

        var templates = await query
            .OrderBy(t => t.Name)
            .ToListAsync();

        return Ok(new { data = templates });
    }

    /// <summary>
    /// Get audience segments with real-time counts
    /// </summary>
    [HttpGet("audiences")]
    public async Task<IActionResult> GetAudiences()
    {
        var now = DateTime.UtcNow;
        var ninetyDaysAgo = now.AddDays(-90);
        var thirtyDaysAgo = now.AddDays(-30);

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var allCount = await _context.Clients.CountAsync(c => c.TenantId == tenantId);
        var activeCount = await _context.Clients.CountAsync(c => c.TenantId == tenantId && c.LastVisitAt >= ninetyDaysAgo);
        var vipCount = await _context.Clients.CountAsync(c => c.TenantId == tenantId && c.LifetimeValue >= 1000);
        var newCount = await _context.Clients.CountAsync(c => c.TenantId == tenantId && c.CreatedAt >= thirtyDaysAgo);
        var lapsedCount = await _context.Clients.CountAsync(c => c.TenantId == tenantId && (c.LastVisitAt == null || c.LastVisitAt < ninetyDaysAgo));
        var birthdayCount = await _context.Clients.CountAsync(c => c.TenantId == tenantId && c.DateOfBirth.HasValue && c.DateOfBirth.Value.Month == now.Month);

        return Ok(new
        {
            data = new[]
            {
                new { id = "all", name = "All Clients", count = allCount, description = "Total client base" },
                new { id = "active", name = "Active Clients", count = activeCount, description = "Visited in the last 90 days" },
                new { id = "vip", name = "VIP Clients", count = vipCount, description = "LTV >= $1,000" },
                new { id = "new", name = "New Clients", count = newCount, description = "Joined in the last 30 days" },
                new { id = "lapsed", name = "Lapsed Clients", count = lapsedCount, description = "No visit in 90+ days" },
                new { id = "birthday", name = "Birthday This Month", count = birthdayCount, description = "Clients celebrating birthdays" }
            }
        });
    }

    /// <summary>
    /// Get aggregate performance metrics for all campaigns in a period
    /// </summary>
    [HttpGet("performance-aggregate")]
    public async Task<IActionResult> GetPerformanceAggregate([FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var startDate = start ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = end ?? DateTime.UtcNow;

        var campaigns = await _context.Campaigns
            .Where(c => c.TenantId == tenantId && c.CreatedAt >= startDate && c.CreatedAt <= endDate && c.Status == "sent")
            .ToListAsync();

        int totalSent = 0;
        int totalOpened = 0;
        int totalClicked = 0;
        decimal totalRevenue = 0;

        foreach (var c in campaigns)
        {
            var stats = await _analyticsService.GetAnalyticsAsync(c.Id);
            if (stats != null)
            {
                totalSent += stats.SentCount;
                totalOpened += stats.OpenedCount;
                totalClicked += stats.ClickedCount;
                totalRevenue += stats.RevenueGenerated;
            }
        }

        return Ok(new
        {
            startDate,
            endDate,
            campaignCount = campaigns.Count,
            totalSent,
            totalOpened,
            totalClicked,
            totalRevenue,
            openRate = totalSent > 0 ? Math.Round((double)totalOpened / totalSent * 100, 2) : 0,
            clickRate = totalSent > 0 ? Math.Round((double)totalClicked / totalSent * 100, 2) : 0
        });
    }

    /// <summary>
    /// Get all auto-responders
    /// </summary>
    [HttpGet("auto-responders")]
    public async Task<IActionResult> GetAutoResponders()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var responders = await _context.MarketingAutoResponders
            .Where(r => r.TenantId == tenantId)
            .ToListAsync();

        return Ok(new { data = responders });
    }

    /// <summary>
    /// Create or update an auto-responder
    /// </summary>
    [HttpPost("auto-responders")]
    public async Task<IActionResult> SaveAutoResponder([FromBody] SaveAutoResponderRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        MarketingAutoResponder? responder;
        if (request.Id.HasValue)
        {
            responder = await _context.MarketingAutoResponders.FirstOrDefaultAsync(r => r.Id == request.Id.Value && r.TenantId == tenantId);
            if (responder == null) return NotFound();
        }
        else
        {
            responder = new MarketingAutoResponder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.MarketingAutoResponders.Add(responder);
        }

        responder.Name = request.Name;
        responder.TriggerEvent = request.TriggerEvent;
        responder.EmailTemplateId = request.EmailTemplateId;
        responder.Subject = request.Subject;
        responder.Content = request.Content;
        responder.DelayMinutes = request.DelayMinutes;
        responder.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Auto-responder saved: {Id} - {Name}", responder.Id, responder.Name);

        return Ok(responder);
    }
}

// Request DTOs
public record CreateCampaignRequest(
    string Name,
    string Type = "email",
    string? Subject = null,
    string? Content = null,
    Guid? TemplateId = null
);

public record UpdateCampaignRequest(
    string? Name = null,
    string? Subject = null,
    string? Preheader = null,
    string? Content = null,
    string? AudienceType = null,
    object? AudienceFilters = null
);

public record ScheduleCampaignRequest(DateTime ScheduledAt);
public record SendTestRequest(string Email);
public record GenerateContentRequest(string Topic, string Type, string? Context = null);

public record SaveAutoResponderRequest(
    Guid? Id,
    string Name,
    string TriggerEvent,
    Guid? EmailTemplateId,
    string? Subject,
    string? Content,
    int DelayMinutes,
    bool IsActive
);

