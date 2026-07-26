using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Identifies at-risk clients (no visit in 30-90 days, declining booking frequency, high LTV)
/// and generates AI-personalized re-engagement messages.
/// </summary>
public class ClientRetentionService
{
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;
    private readonly ISmsService _smsService;
    private readonly ILogger<ClientRetentionService> _logger;

    public ClientRetentionService(
        AppDbContext context,
        IAIService aiService,
        ISmsService smsService,
        ILogger<ClientRetentionService> logger)
    {
        _context = context;
        _aiService = aiService;
        _smsService = smsService;
        _logger = logger;
    }

    /// <summary>
    /// Returns clients at risk of churning, sorted by LTV descending.
    /// At-risk = last visit was 30-120 days ago AND had ≥2 prior bookings.
    /// </summary>
    public async Task<IReadOnlyList<AtRiskClient>> GetAtRiskClientsAsync(Guid tenantId, int limit = 50)
    {
        var cutoff30 = DateTime.UtcNow.AddDays(-30);
        var cutoff120 = DateTime.UtcNow.AddDays(-120);

        var clients = await _context.Clients
            .Where(c =>
                c.TenantId == tenantId &&
                !c.IsDeleted &&
                c.IsActive &&
                c.TotalBookings >= 2 &&
                c.LastVisitAt != null &&
                c.LastVisitAt <= cutoff30 &&
                c.LastVisitAt >= cutoff120)
            .OrderByDescending(c => c.LifetimeValue)
            .Take(limit)
            .ToListAsync();

        return clients.Select(c =>
        {
            var daysSince = (int)(DateTime.UtcNow - c.LastVisitAt!.Value).TotalDays;
            var riskScore = CalculateRiskScore(c, daysSince);
            return new AtRiskClient
            {
                ClientId       = c.Id,
                FullName       = c.FullName,
                Email          = c.Email,
                Phone          = c.Phone,
                LifetimeValue  = c.LifetimeValue,
                TotalBookings  = c.TotalBookings,
                LastVisitAt    = c.LastVisitAt.Value,
                DaysSinceLastVisit = daysSince,
                RiskScore      = riskScore,
                RiskLabel      = riskScore >= 80 ? "High" : riskScore >= 50 ? "Medium" : "Low"
            };
        }).ToList();
    }

    /// <summary>
    /// Generates a personalized re-engagement SMS message for a client using AI.
    /// </summary>
    public async Task<string> GenerateReEngagementMessageAsync(
        Guid tenantId, AtRiskClient client, string businessName, string serviceType)
    {
        var prompt = $"""
            Write a friendly, personalized SMS re-engagement message for a {serviceType} business named "{businessName}".
            The client is {client.FullName}, who has been a customer {client.TotalBookings} times but hasn't visited in {client.DaysSinceLastVisit} days.
            Their lifetime value is ${client.LifetimeValue:F0}.

            Requirements:
            - Maximum 160 characters (one SMS)
            - Personal, warm tone — use their first name
            - Include a soft call-to-action (book now, we miss you, special offer)
            - Do NOT include any URLs or phone numbers
            - Output only the message text, nothing else
            """;

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        return result.Success
            ? result.Content?.Trim() ?? $"Hi {client.FullName.Split(' ')[0]}! We miss you at {businessName}. Book your next appointment today."
            : $"Hi {client.FullName.Split(' ')[0]}! We miss you at {businessName}. It's been a while — we'd love to see you again!";
    }

    /// <summary>
    /// Sends AI-generated re-engagement SMS to a list of at-risk clients.
    /// Skips clients without SMS consent or phone numbers.
    /// </summary>
    public async Task SendReEngagementCampaignAsync(
        Guid tenantId,
        IEnumerable<AtRiskClient> clients,
        string businessName,
        string serviceType)
    {
        foreach (var client in clients)
        {
            if (string.IsNullOrEmpty(client.Phone)) continue;

            try
            {
                var dbClient = await _context.Clients.FindAsync(client.ClientId);
                if (dbClient == null || !dbClient.SmsConsent) continue;

                var message = await GenerateReEngagementMessageAsync(tenantId, client, businessName, serviceType);
                await _smsService.SendSmsAsync(tenantId, client.Phone, message, client.ClientId);

                _logger.LogInformation("Re-engagement SMS sent to client {ClientId} (tenant {TenantId})", client.ClientId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send re-engagement SMS to client {ClientId}", client.ClientId);
            }
        }
    }

    private static int CalculateRiskScore(Client c, int daysSince)
    {
        // Higher score = more likely to churn
        int score = 0;

        // Days since last visit (0-50 points)
        score += daysSince switch
        {
            >= 90 => 50,
            >= 60 => 35,
            >= 30 => 20,
            _ => 0
        };

        // Booking frequency decline (0-30 points) — simple proxy: LTV per booking
        if (c.TotalBookings > 0)
        {
            var avgBookingValue = c.LifetimeValue / c.TotalBookings;
            if (avgBookingValue < 30) score += 30;
            else if (avgBookingValue < 60) score += 15;
        }

        // High LTV = more worth saving (adds urgency, not risk, cap at +20)
        if (c.LifetimeValue >= 500) score += 20;
        else if (c.LifetimeValue >= 200) score += 10;

        return Math.Min(100, score);
    }
}

public class AtRiskClient
{
    public Guid ClientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal LifetimeValue { get; set; }
    public int TotalBookings { get; set; }
    public DateTime LastVisitAt { get; set; }
    public int DaysSinceLastVisit { get; set; }
    public int RiskScore { get; set; }
    public string RiskLabel { get; set; } = "Low";
}
