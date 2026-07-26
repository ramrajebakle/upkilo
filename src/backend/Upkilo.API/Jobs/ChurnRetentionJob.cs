using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Jobs;

/// <summary>
/// Nightly job that wires ChurnPredictorAgent predictions to proactive retention campaigns.
///
/// Flow per tenant:
///   1. Find clients with no booking in 45–90 days (medium risk) or 90+ days (high risk).
///   2. Skip clients who already received a retention message in the last 30 days.
///   3. Call ChurnPredictorAgent.PredictChurnRiskAsync → get "high" | "medium" | "low".
///   4. For high/medium risk: generate a personalized message via ProactiveMessagingService.
///   5. Send immediately.
///
/// Only runs for tenants on Professional, Business, or Enterprise tiers where AI
/// features are enabled and AI credit quotas exist.
/// </summary>
public class ChurnRetentionJob
{
    private readonly AppDbContext _context;
    private readonly IChurnPredictorAgent _churnPredictor;
    private readonly IProactiveMessagingService _proactiveMessaging;
    private readonly ILogger<ChurnRetentionJob> _logger;

    // Thresholds for churn risk windows — must match ProactiveMessagingService.LapsedThresholdDays (60)
    private const int MediumRiskDays = 60;
    private const int HighRiskDays = 90;
    // Max clients processed per tenant per run (controls AI credit spend)
    private const int MaxClientsPerTenant = 10;
    // Suppression window — don't re-contact within 30 days
    private const int SuppressionDays = 30;

    public ChurnRetentionJob(
        AppDbContext context,
        IChurnPredictorAgent churnPredictor,
        IProactiveMessagingService proactiveMessaging,
        ILogger<ChurnRetentionJob> logger)
    {
        _context = context;
        _churnPredictor = churnPredictor;
        _proactiveMessaging = proactiveMessaging;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("ChurnRetentionJob started at {Time}", DateTime.UtcNow);

        // Only process AI-enabled tiers
        var eligibleTiers = new[] { SubscriptionTier.Professional, SubscriptionTier.Business, SubscriptionTier.Enterprise };

        var tenantIds = await _context.Tenants
            .Where(t => t.Status == TenantStatus.Active && eligibleTiers.Contains(t.SubscriptionTier))
            .Select(t => t.Id)
            .ToListAsync();

        _logger.LogInformation("Processing churn retention for {Count} eligible tenants", tenantIds.Count);

        int totalMessagesScheduled = 0;

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var sent = await ProcessTenantAsync(tenantId);
                totalMessagesScheduled += sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChurnRetentionJob failed for tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation(
            "ChurnRetentionJob completed. Messages scheduled: {Total}",
            totalMessagesScheduled);
    }

    private async Task<int> ProcessTenantAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var mediumRiskCutoff = now.AddDays(-MediumRiskDays);
        var suppressionCutoff = now.AddDays(-SuppressionDays);

        // Find clients at risk: last booking between 45–180 days ago, with marketing consent
        var atRiskClients = await _context.Clients
            .Where(c => c.TenantId == tenantId && c.MarketingConsent)
            .Join(_context.Bookings,
                c => c.Id,
                b => b.ClientId,
                (c, b) => new { Client = c, Booking = b })
            .GroupBy(x => x.Client.Id)
            .Where(g => g.Max(x => x.Booking.StartTime) < mediumRiskCutoff
                     && g.Max(x => x.Booking.StartTime) > now.AddDays(-180))
            .Select(g => g.Key)
            .Take(MaxClientsPerTenant * 3) // Fetch extra to account for suppression filtering
            .ToListAsync();

        if (!atRiskClients.Any())
            return 0;

        // Filter out clients who already received a retention message recently
        var recentlySent = await _context.CommunicationLogs
            .Where(c => c.TenantId == tenantId
                && c.CreatedAt >= suppressionCutoff
                && c.Subject != null && c.Subject.Contains("retention"))
            .Select(c => c.ClientId)
            .Distinct()
            .ToListAsync();

        var candidateIds = atRiskClients
            .Where(id => !recentlySent.Contains(id))
            .Take(MaxClientsPerTenant)
            .ToList();

        if (!candidateIds.Any())
            return 0;

        int sent = 0;
        // Limit concurrent AI calls to 3 to avoid hammering the AI service
        using var sem = new SemaphoreSlim(3, 3);

        var tasks = candidateIds.Select(async clientId =>
        {
            await sem.WaitAsync();
            try
            {
                // AI churn risk assessment
                var riskLevel = await _churnPredictor.PredictChurnRiskAsync(tenantId, clientId);

                // Only act on high or medium risk — skip low risk to avoid over-messaging
                if (!riskLevel.Contains("high", StringComparison.OrdinalIgnoreCase)
                    && !riskLevel.Contains("medium", StringComparison.OrdinalIgnoreCase))
                    return;

                // Generate AI-personalized retention message
                var message = await _proactiveMessaging.GenerateForClientAsync(
                    tenantId, clientId, "lapsed_client");

                if (message == null) return;

                // Accumulate logs — batch-saved after the loop
                lock (_context)
                {
                    _context.CommunicationLogs.Add(new CommunicationLog
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ClientId = clientId,
                        Type = CommunicationType.Email,
                        Direction = CommunicationDirection.Outbound,
                        Subject = $"[retention] {message.Subject}",
                        Body = message.Body,
                        Status = CommunicationStatus.Pending,
                        ReferenceId = $"churn_retention_{clientId}_{now:yyyyMMdd}",
                        CreatedAt = now
                    });
                }

                Interlocked.Increment(ref sent);

                _logger.LogInformation(
                    "Churn retention message queued for client {ClientId} in tenant {TenantId} (risk: {Risk})",
                    clientId, tenantId, riskLevel.Split('.')[0].Trim());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to process churn retention for client {ClientId} in tenant {TenantId}",
                    clientId, tenantId);
            }
            finally
            {
                sem.Release();
            }
        });

        await Task.WhenAll(tasks);
        // Single batch write after all AI calls complete
        if (sent > 0)
            await _context.SaveChangesAsync();

        // Send all queued messages for this tenant in one batch
        if (sent > 0)
        {
            try
            {
                await _proactiveMessaging.SendPendingMessagesAsync(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send retention messages for tenant {TenantId}", tenantId);
            }
        }

        return sent;
    }
}
