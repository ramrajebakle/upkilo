using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// The central orchestrator for Zero Human Intervention Marketing.
/// This job runs periodically to check which tenants have autonomous mode enabled
/// and triggers the appropriate AI agents (SEO, Content, Social, etc.) based on their schedules.
/// </summary>
public class MarketingAutomationOrchestratorJob
{
    private readonly AppDbContext _context;
    private readonly IMarketingAutomationService _marketingService;
    private readonly ILogger<MarketingAutomationOrchestratorJob> _logger;

    public MarketingAutomationOrchestratorJob(
        AppDbContext context,
        IMarketingAutomationService marketingService,
        ILogger<MarketingAutomationOrchestratorJob> logger)
    {
        _context = context;
        _marketingService = marketingService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting Marketing Automation Orchestrator Job");

        // 1. Find all tenants with Autonomous Mode enabled
        var autonomousConfigs = await _context.MarketingConfigs
            .Where(c => c.IsAutonomousMode && c.IsOnboarded)
            .ToListAsync();

        _logger.LogInformation("Found {Count} tenants with Autonomous Mode enabled", autonomousConfigs.Count);

        foreach (var config in autonomousConfigs)
        {
            try
            {
                await ProcessTenantAutomationAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process marketing automation for tenant {TenantId}", config.TenantId);
            }
        }

        _logger.LogInformation("Marketing Automation Orchestrator Job completed");
    }

    private async Task ProcessTenantAutomationAsync(MarketingConfig config)
    {
        var tenantId = config.TenantId;
        _logger.LogInformation("Processing automation for tenant {TenantId} ({BusinessUrl})", tenantId, config.BusinessUrl);

        // --- ANOMALY DETECTION (AGENT 6: Analytics) ---
        if (await DetectPerformanceAnomaliesAsync(tenantId))
        {
            _logger.LogWarning("Performance anomaly detected for tenant {TenantId}. Safety halt triggered.", tenantId);
            return;
        }

        // --- SAFETY CHECK ---
        if (!await CheckAutomationSafetyAsync(tenantId))
        {
            _logger.LogWarning("Safety check failed for tenant {TenantId}. Skipping automation cycle.", tenantId);
            return;
        }

        // --- AGENT 1: SEO Optimization (Weekly) ---
        if (ShouldRunAgent(config, "SEO", TimeSpan.FromDays(7)))
        {
            _logger.LogInformation("Triggering SEO Agent for tenant {TenantId}", tenantId);
            await _marketingService.AnalyzePageAsync(tenantId, config.BusinessUrl);
        }

        // --- AGENT 2: Content Generation (Bi-Weekly) ---
        if (ShouldRunAgent(config, "Content", TimeSpan.FromDays(3.5))) // Approx 2/week
        {
            _logger.LogInformation("Triggering Content Agent for tenant {TenantId}", tenantId);

            // Dynamic Topic Selection: Combine industry niche with trending keywords from discovery
            var latestDiscovery = await _context.AIDiscoveryReports
                .Where(r => r.TenantId == tenantId)
                .OrderByDescending(r => r.GeneratedAt)
                .FirstOrDefaultAsync();

            var keywords = latestDiscovery?.Keywords?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Take(3).Select(k => k.Trim()).ToArray() ?? new[] { "innovation", "tips" };

            var topic = $"How {config.IndustryNiche} is evolving with {string.Join(" and ", keywords)}";
            await _marketingService.GenerateBlogPostAsync(tenantId, topic, keywords);
        }

        // --- AGENT 3: Discovery & Indexing (Daily) ---
        if (ShouldRunAgent(config, "Discovery", TimeSpan.FromDays(1)))
        {
            _logger.LogInformation("Triggering Discovery Agent for tenant {TenantId}", tenantId);
            await _marketingService.PerformDiscoveryScanAsync(tenantId);
        }

        // --- AGENT 4: Distribution & Social (Daily) ---
        if (ShouldRunAgent(config, "Distribution", TimeSpan.FromDays(1)))
        {
            _logger.LogInformation("Triggering Distribution Agent for tenant {TenantId}", tenantId);
            var platforms = new[] { "LinkedIn", "Twitter", "Instagram" };
            var platform = platforms[Random.Shared.Next(platforms.Length)];
            await _marketingService.GenerateSocialPostAsync(tenantId, platform, $"Why you should care about {config.IndustryNiche}");
        }

        // --- AGENT 5: Lead & Conversion Optimization (Weekly) ---
        if (ShouldRunAgent(config, "LeadOptimizer", TimeSpan.FromDays(7)))
        {
            _logger.LogInformation("Triggering Lead Optimizer Agent for tenant {TenantId}", tenantId);
            await _marketingService.OptimizeConversionsAsync(tenantId);
        }

        // --- AGENT 6: Analytics & Forecasting (Daily) ---
        if (ShouldRunAgent(config, "Analytics", TimeSpan.FromDays(1)))
        {
            _logger.LogInformation("Triggering Analytics Agent for tenant {TenantId}", tenantId);

            // 1. Sync Real-World Analytics (GA4)
            await _marketingService.SyncAnalyticsFromExternalAsync(tenantId);

            // 2. Generate Forecasts
            await _marketingService.GetForecastsAsync(tenantId, 30);
        }
    }

    private bool ShouldRunAgent(MarketingConfig config, string agentName, TimeSpan interval)
    {
        // Check AgentActions table for the last time this agent ran for this tenant
        var lastRun = _context.AgentActions
            .Where(a => a.TenantId == config.TenantId && a.AgentName == agentName)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.CreatedAt)
            .FirstOrDefault();

        if (lastRun == default) return true;

        return (DateTime.UtcNow - lastRun) >= interval;
    }

    private async Task<bool> CheckAutomationSafetyAsync(Guid tenantId)
    {
        // 1. Traffic Drop Check (Safety Rule 5.4.1 in task.md)
        var now = DateTime.UtcNow;
        var last7Days = now.AddDays(-7);
        var prev7Days = now.AddDays(-14);

        var currentTraffic = await _context.PageAnalyticsRecords
            .Where(a => a.TenantId == tenantId && a.Timestamp >= last7Days)
            .CountAsync();

        var previousTraffic = await _context.PageAnalyticsRecords
            .Where(a => a.TenantId == tenantId && a.Timestamp >= prev7Days && a.Timestamp < last7Days)
            .CountAsync();

        if (previousTraffic > 100 && currentTraffic < previousTraffic * 0.8) // 20% drop
        {
            await LogSafetyAlertAsync(tenantId, "Traffic Drop Detection", $"Traffic dropped by {100 - (currentTraffic * 100 / previousTraffic)}%. Auto-pausing automation.");
            return false;
        }

        // 3. Duplicate Content Safety (Safety Rule 5.4.1)
        var recentHalts = await _context.AgentActions
            .Where(a => a.TenantId == tenantId && a.ActionType == "Halt" && a.CreatedAt >= now.AddHours(-48))
            .CountAsync();

        if (recentHalts >= 3)
        {
            await LogSafetyAlertAsync(tenantId, "Recursive Duplicate Detection", "AI has attempted to generate duplicate content 3+ times in 48h. Pausing to prevent spam.");
            return false;
        }

        return true;
    }

    private async Task<bool> DetectPerformanceAnomaliesAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);

        // 1. Conversion Rate Anomaly
        var analytics = await _context.PageAnalyticsRecords
            .Where(a => a.TenantId == tenantId && a.Timestamp >= last24Hours)
            .ToListAsync();

        if (analytics.Any())
        {
            var avgConvRate = analytics.Average(a => a.ConversionRate);
            if (avgConvRate < 0.5m && analytics.Sum(a => a.TotalViews) > 100) // If very low conv rate on decent traffic
            {
                await LogSafetyAlertAsync(tenantId, "Conversion Anomaly", $"Extremely low conversion rate ({avgConvRate:F2}%) detected in last 24h.");
                return true;
            }
        }

        return false;
    }

    private async Task LogSafetyAlertAsync(Guid tenantId, string alertType, string message)
    {
        _context.AgentActions.Add(new AgentAction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AgentName = "System Safety",
            ActionType = "Safety Halt",
            Description = $"{alertType}: {message}",
            RiskLevel = "Critical",
            RequiresReview = true,
            WasAutoApplied = true
        });
        await _context.SaveChangesAsync();
    }
}
