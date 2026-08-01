using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Filters;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Intelligence Layer (IL1-IL5) + Network Effects (NE1, NE2).
/// All endpoints are read-heavy analytics; routed to read replica via [ReadReplicaFilter].
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/intelligence")]
[Authorize]
[ReadReplicaFilter]
public class IntelligenceController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAIService _aiService;
    private readonly ILogger<IntelligenceController> _logger;

    public IntelligenceController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        IAIService aiService,
        ILogger<IntelligenceController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _aiService = aiService;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    // ------------------------------------------------------------------
    // IL1: Demand Forecasting
    // ------------------------------------------------------------------

    /// <summary>
    /// IL1: GET /intelligence/demand-forecast — predicts busy periods 4 weeks out.
    /// Uses historical booking distribution by day-of-week + hour to score upcoming slots.
    /// Also suggests staff scheduling adjustments.
    /// </summary>
    [HttpGet("demand-forecast")]
    [RequiresFeature("AiFeatures")]
    public async Task<IActionResult> GetDemandForecast()
    {
        var tenantId = GetTenantId();
        var now = DateTime.UtcNow;
        var historyStart = now.AddDays(-84); // 12 weeks of history

        // Aggregate booking counts by day-of-week and hour
        var historicalBookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.StartTime >= historyStart &&
                        b.Status != BookingStatus.Cancelled)
            .Select(b => new { b.StartTime })
            .ToListAsync();

        var heatmap = historicalBookings
            .GroupBy(b => new { DayOfWeek = b.StartTime.DayOfWeek, Hour = b.StartTime.Hour })
            .Select(g => new { g.Key.DayOfWeek, g.Key.Hour, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        var peakSlots = heatmap.Take(5).Select(x => new
        {
            day = x.DayOfWeek.ToString(),
            hour = $"{x.Hour:00}:00",
            demandScore = Math.Round((double)x.count / Math.Max(1, heatmap.Max(h => h.count)) * 100, 0)
        }).ToList();

        // 4-week forward projection by day-of-week pattern
        var forecastDays = Enumerable.Range(1, 28).Select(i =>
        {
            var date = now.AddDays(i).Date;
            var dow = date.DayOfWeek;
            var avgBookings = historicalBookings
                .Where(b => b.StartTime.DayOfWeek == dow)
                .GroupBy(b => b.StartTime.Date)
                .Select(g => g.Count())
                .DefaultIfEmpty(0)
                .Average();

            return new
            {
                date = date.ToString("yyyy-MM-dd"),
                dayOfWeek = dow.ToString(),
                predictedBookings = Math.Round(avgBookings, 1),
                demandLevel = avgBookings >= 10 ? "high" : avgBookings >= 5 ? "medium" : "low"
            };
        }).ToList();

        var busyDays = forecastDays.Where(d => d.demandLevel == "high")
            .Select(d => d.date).Take(5).ToList();

        return Ok(new
        {
            forecastWeeks = 4,
            peakSlots,
            forecast = forecastDays,
            staffingAdvice = busyDays.Any()
                ? $"Expect high demand on: {string.Join(", ", busyDays)}. Consider scheduling extra staff or blocking last-minute slots."
                : "Demand looks steady. No immediate staffing adjustments needed.",
            generatedAt = now
        });
    }

    // ------------------------------------------------------------------
    // IL2: Price Optimization AI
    // ------------------------------------------------------------------

    /// <summary>
    /// IL2: GET /intelligence/price-optimization — AI-suggested dynamic pricing.
    /// Compares current prices against booking conversion rates and time-slot demand.
    /// </summary>
    [HttpGet("price-optimization")]
    [RequiresFeature("AiFeatures")]
    public async Task<IActionResult> GetPriceOptimization()
    {
        var tenantId = GetTenantId();
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var services = await _context.Services
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .Select(s => new { s.Id, s.Name, s.Price, s.DurationMinutes })
            .ToListAsync();

        var bookingsByService = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.StartTime >= thirtyDaysAgo &&
                        b.ServiceId.HasValue)
            .GroupBy(b => b.ServiceId)
            .Select(g => new { serviceId = g.Key, bookings = g.Count(), cancellations = g.Count(b => b.Status == BookingStatus.Cancelled) })
            .ToListAsync();

        var suggestions = services.Select(s =>
        {
            var stats = bookingsByService.FirstOrDefault(b => b.serviceId == s.Id);
            var bookings = stats?.bookings ?? 0;
            var cancellations = stats?.cancellations ?? 0;
            var conversionRate = bookings > 0 ? (double)(bookings - cancellations) / bookings : 0;

            // High demand + high conversion → consider price increase
            // Low demand + low price → consider minor increase or promotion
            string action;
            decimal suggestedPrice;
            if (bookings >= 15 && conversionRate >= 0.8)
            {
                action = "increase";
                suggestedPrice = Math.Round(s.Price * 1.10m / 5) * 5; // +10%, rounded to $5
            }
            else if (bookings <= 3)
            {
                action = "promote";
                suggestedPrice = Math.Round(s.Price * 0.90m / 5) * 5; // -10% introductory
            }
            else
            {
                action = "maintain";
                suggestedPrice = s.Price;
            }

            return new
            {
                serviceId = s.Id,
                serviceName = s.Name,
                currentPrice = s.Price,
                suggestedPrice,
                action,
                bookingsLast30d = bookings,
                conversionRate = Math.Round(conversionRate * 100, 1),
                reasoning = action switch
                {
                    "increase" => $"{s.Name} is consistently fully booked at this price. A 10% increase should hold demand.",
                    "promote" => $"{s.Name} has low bookings. A limited-time 10% discount may attract new clients.",
                    _ => $"{s.Name} is performing well at current price. No change recommended."
                }
            };
        }).ToList();

        return Ok(new
        {
            tenantId,
            analyzedServices = suggestions.Count,
            recommendations = suggestions,
            potentialRevenueUplift = suggestions
                .Where(s => s.action == "increase")
                .Sum(s => (s.suggestedPrice - s.currentPrice) * s.bookingsLast30d),
            generatedAt = now
        });
    }

    // ------------------------------------------------------------------
    // IL3: Staff Retention Predictor
    // ------------------------------------------------------------------

    /// <summary>
    /// IL3: GET /intelligence/staff-retention — flags at-risk staff based on hours + tip trends.
    /// </summary>
    [HttpGet("staff-retention")]
    [Authorize(Roles = "Owner,Admin")]
    [RequiresFeature("AiFeatures")]
    public async Task<IActionResult> GetStaffRetentionRisk()
    {
        var tenantId = GetTenantId();
        var now = DateTime.UtcNow;
        var sixtyDaysAgo = now.AddDays(-60);

        var staff = await _context.StaffMembers
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToListAsync();

        var bookingsByStaff = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.StartTime >= sixtyDaysAgo &&
                        b.Status != BookingStatus.Cancelled)
            .GroupBy(b => b.StaffId)
            .Select(g => new
            {
                staffId = g.Key,
                totalBookings = g.Count(),
                recentBookings = g.Count(b => b.StartTime >= now.AddDays(-14)),
                avgTip = 0.0,       // tip data from Payments join — simplified for IL3
                avgTipRecent = 0.0
            })
            .ToListAsync();

        var riskFlags = staff.Select(s =>
        {
            var stats = bookingsByStaff.FirstOrDefault(b => b.staffId == s.Id);
            if (stats == null) return new { s.Id, s.Name, riskLevel = "unknown", riskScore = 0.0, signals = Array.Empty<string>() };

            var signals = new List<string>();
            double riskScore = 0;

            // Declining bookings
            var bookingDeclineRate = stats.totalBookings > 0
                ? 1.0 - (double)stats.recentBookings / Math.Max(1, stats.totalBookings / 4.0)
                : 0;
            if (bookingDeclineRate > 0.4)
            {
                signals.Add($"Booking volume dropped {Math.Round(bookingDeclineRate * 100)}% in last 2 weeks");
                riskScore += 0.4;
            }

            // Declining tips (proxy for engagement/satisfaction)
            if (stats.avgTip > 0 && stats.avgTipRecent < stats.avgTip * 0.7)
            {
                signals.Add($"Average tip declined from ${stats.avgTip:F2} to ${stats.avgTipRecent:F2}");
                riskScore += 0.3;
            }

            var name = s.Name;
            var riskLevel = riskScore >= 0.6 ? "high" : riskScore >= 0.3 ? "medium" : "low";
            return new { s.Id, Name = name, riskLevel, riskScore = Math.Round(riskScore, 2), signals = signals.ToArray() };
        }).ToList();

        var highRisk = riskFlags.Where(r => r.riskLevel == "high").ToList();

        return Ok(new
        {
            tenantId,
            analysisWindow = "60 days",
            staffAnalyzed = riskFlags.Count,
            highRiskCount = highRisk.Count,
            staff = riskFlags.OrderByDescending(r => r.riskScore),
            recommendation = highRisk.Any()
                ? $"{highRisk.Count} staff member(s) show retention risk signals. Schedule 1:1s: {string.Join(", ", highRisk.Select(r => r.Name))}"
                : "No significant retention risk signals detected.",

            generatedAt = now
        });
    }

    // ------------------------------------------------------------------
    // IL4: No-Show Scoring (booking-level)
    // ------------------------------------------------------------------

    /// <summary>
    /// IL4: GET /intelligence/no-show-risk — returns no-show risk scores for upcoming bookings.
    /// High-risk bookings (score >= 0.65) are flagged to prompt deposit request.
    /// </summary>
    [HttpGet("no-show-risk")]
    public async Task<IActionResult> GetNoShowRisk([FromQuery] int daysAhead = 7)
    {
        var tenantId = GetTenantId();
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(daysAhead);

        var upcomingBookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Where(b => b.TenantId == tenantId &&
                        b.StartTime >= now &&
                        b.StartTime <= cutoff &&
                        b.Status == BookingStatus.Confirmed)
            .ToListAsync();

        // Gather historical no-show rates per client
        var clientIds = upcomingBookings.Where(b => b.ClientId.HasValue).Select(b => b.ClientId!.Value).Distinct().ToList();
        var clientHistory = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.ClientId.HasValue && clientIds.Contains(b.ClientId!.Value))
            .GroupBy(b => b.ClientId)
            .Select(g => new
            {
                clientId = g.Key!.Value,
                total = g.Count(),
                noShows = g.Count(b => b.Status == BookingStatus.NoShow)
            })
            .ToListAsync();

        var scored = upcomingBookings.Select(b =>
        {
            double riskScore = 0.1; // base risk

            // Client history risk
            var hist = clientHistory.FirstOrDefault(h => h.clientId == b.ClientId);
            if (hist != null && hist.total > 0)
            {
                var noShowRate = (double)hist.noShows / hist.total;
                riskScore += noShowRate * 0.6;
            }

            // New client with no history
            if (b.ClientId == null || (clientHistory.All(h => h.clientId != b.ClientId)))
                riskScore += 0.2;

            // Short-notice bookings (booked < 24h before)
            if ((b.StartTime - b.CreatedAt).TotalHours < 24)
                riskScore += 0.15;

            riskScore = Math.Min(riskScore, 1.0);
            var riskLevel = riskScore >= 0.65 ? "high" : riskScore >= 0.35 ? "medium" : "low";

            return new
            {
                bookingId = b.Id,
                clientName = b.Client?.FullName ?? b.CustomerName ?? "Unknown",
                serviceName = b.Service?.Name ?? "Unknown",
                startTime = b.StartTime,
                riskScore = Math.Round(riskScore, 2),
                riskLevel,
                depositRequired = riskScore >= 0.65,
                signals = new[]
                {
                    hist?.noShows > 0 ? $"Client has {hist.noShows} prior no-show(s)" : null,
                    (b.StartTime - b.CreatedAt).TotalHours < 24 ? "Booked less than 24h in advance" : null,
                    b.ClientId == null ? "No client profile" : null
                }.Where(s => s != null).ToArray()
            };
        }).OrderByDescending(b => b.riskScore).ToList();

        return Ok(new
        {
            tenantId,
            upcomingBookings = scored.Count,
            highRisk = scored.Count(b => b.riskLevel == "high"),
            bookings = scored,
            note = "Bookings with riskScore >= 0.65 are flagged for deposit request.",
            generatedAt = now
        });
    }

    // ------------------------------------------------------------------
    // IL5: AI Competitor Monitoring
    // ------------------------------------------------------------------

    /// <summary>
    /// IL5: GET /intelligence/competitor-report — weekly AI-generated competitor snapshot.
    /// Uses marketplace data to surface competitor pricing, ratings, and review trends.
    /// </summary>
    [HttpGet("competitor-report")]
    [RequiresFeature("AiFeatures")]
    public async Task<IActionResult> GetCompetitorReport()
    {
        var tenantId = GetTenantId();
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return NotFound();

        // Find competitors: same city + similar industry, not the same tenant
        var competitors = await _context.Tenants
            .Where(t => t.Id != tenantId &&
                        !t.IsDeleted &&
                        t.City == tenant.City &&
                        t.Industry == tenant.Industry)
            .OrderByDescending(t => t.ReviewCount)
            .Take(10)
            .Select(t => new
            {
                t.Name,
                t.AverageRating,
                t.ReviewCount,
                isVerified = t.Settings.ContainsKey("verifiedBadge"),
                t.Industry
            })
            .ToListAsync();

        if (!competitors.Any())
            return Ok(new
            {
                tenantId,
                message = "No competitors found in your area. You may be first to market!",
                generatedAt = DateTime.UtcNow
            });

        var avgCompetitorRating = competitors.Average(c => (double)c.AverageRating);
        var avgCompetitorReviews = competitors.Average(c => c.ReviewCount);
        var verifiedCount = competitors.Count(c => c.isVerified);

        // AI narrative
        var prompt =
            $"You are a business intelligence analyst. Summarize the competitive landscape for a '{tenant.Industry}' business in {tenant.City}.\n" +
            $"Tenant's stats: Rating={tenant.AverageRating}, Reviews={tenant.ReviewCount}\n" +
            $"Competitors ({competitors.Count} total): Avg rating={avgCompetitorRating:F1}, Avg reviews={avgCompetitorReviews:F0}, Verified={verifiedCount}\n" +
            $"Top competitor: {competitors[0].Name} ({competitors[0].AverageRating}★, {competitors[0].ReviewCount} reviews)\n\n" +
            "Write 2-3 sentences on where this business stands relative to competitors and one specific action to gain advantage. Under 80 words.";

        var aiResult = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        var narrative = aiResult.Success ? aiResult.Content?.Trim() ?? "" : "Unable to generate narrative at this time.";

        return Ok(new
        {
            tenantId,
            yourStats = new { rating = tenant.AverageRating, reviewCount = tenant.ReviewCount },
            market = new
            {
                competitorCount = competitors.Count,
                avgRating = Math.Round(avgCompetitorRating, 1),
                avgReviewCount = (int)avgCompetitorReviews,
                verifiedCompetitors = verifiedCount
            },
            topCompetitors = competitors.Take(5),
            narrative,
            generatedAt = DateTime.UtcNow
        });
    }

    // ------------------------------------------------------------------
    // NE1: Client Data Network (cross-business profile pre-fill)
    // ------------------------------------------------------------------

    /// <summary>
    /// NE1: GET /intelligence/client-network/{email} — Returns a portable client profile.
    /// VULN-A03 FIX: Endpoint was [AllowAnonymous] with a non-validated "consent token" check
    /// (any non-empty string was accepted), enabling cross-tenant PII enumeration of all clients.
    /// The signed-JWT consent system is not yet production-ready; returning 501 until implemented.
    /// </summary>
    [HttpGet("client-network/{email}")]
    public IActionResult GetPortableClientProfile(string email, [FromQuery] string? consentToken = null)
    {
        // VULN-A03: The original code accepted any non-empty consentToken string without
        // cryptographic verification, then queried Clients with no tenant scope.
        // Returning 501 until the consent system is implemented with:
        //   1. Signed JWT issued only after email-verified consent flow
        //   2. Token scoped to (email + requesting_tenantId) with 10-min expiry
        //   3. Tenant-isolated client query
        return StatusCode(501, new
        {
            error = "not_implemented",
            message = "The client network consent system is not yet available.",
        });
    }

    /// <summary>
    /// NE2: GET /intelligence/benchmarks — Anonymous industry benchmarks.
    /// "Your revenue per staff is 23% below top-quartile salons in your city."
    /// </summary>
    [HttpGet("benchmarks")]
    [ReadReplicaFilter]
    public async Task<IActionResult> GetIndustryBenchmarks()
    {
        var tenantId = GetTenantId();
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return NotFound();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // Tenant's own metrics
        var myRevenue = await _context.Payments
            .Where(p => p.TenantId == tenantId && p.Status == PaymentStatus.Succeeded && p.CreatedAt >= thirtyDaysAgo)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        var myStaff = await _context.StaffMembers.CountAsync(s => s.TenantId == tenantId && s.IsActive);
        var myBookings = await _context.Bookings.CountAsync(b => b.TenantId == tenantId && b.StartTime >= thirtyDaysAgo && b.Status != BookingStatus.Cancelled);

        // Peer group: same city + industry
        var peerIds = await _context.Tenants
            .Where(t => t.Id != tenantId && !t.IsDeleted && t.City == tenant.City && t.Industry == tenant.Industry)
            .Select(t => t.Id)
            .ToListAsync();

        if (!peerIds.Any())
            return Ok(new { message = "Not enough peers in your area for benchmarking yet.", tenantId });

        var peerRevenues = await Task.WhenAll(peerIds.Select(async pid =>
            await _context.Payments
                .Where(p => p.TenantId == pid && p.Status == PaymentStatus.Succeeded && p.CreatedAt >= thirtyDaysAgo)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m));

        var peerStaffCounts = await _context.StaffMembers
            .Where(s => peerIds.Contains(s.TenantId) && s.IsActive)
            .GroupBy(s => s.TenantId)
            .Select(g => g.Count())
            .ToListAsync();

        var topQuartileRevenue = peerRevenues.OrderByDescending(r => r).Skip((int)(peerRevenues.Length * 0.25)).FirstOrDefault();
        var medianRevenue = peerRevenues.OrderBy(r => r).ElementAtOrDefault(peerRevenues.Length / 2);
        var myRevenuePerStaff = myStaff > 0 ? myRevenue / myStaff : 0;
        var peerAvgStaff = peerStaffCounts.Any() ? (double)peerStaffCounts.Average() : 1;
        var peerAvgRevenuePerStaff = peerAvgStaff > 0 ? (double)medianRevenue / peerAvgStaff : 0;

        var revenueVsMedian = medianRevenue > 0 ? (double)(myRevenue - medianRevenue) / (double)medianRevenue * 100 : 0;
        var revenuePerStaffVsPeer = peerAvgRevenuePerStaff > 0 ? ((double)myRevenuePerStaff - peerAvgRevenuePerStaff) / peerAvgRevenuePerStaff * 100 : 0;

        return Ok(new
        {
            tenantId,
            industry = tenant.Industry,
            city = tenant.City,
            peerCount = peerIds.Count,
            period = "last 30 days",
            yourMetrics = new
            {
                revenue = myRevenue,
                bookings = myBookings,
                staffCount = myStaff,
                revenuePerStaff = myRevenuePerStaff
            },
            benchmarks = new
            {
                medianRevenue,
                topQuartileRevenue,
                revenueVsMedian = $"{(revenueVsMedian >= 0 ? "+" : "")}{Math.Round(revenueVsMedian, 1)}%",
                revenuePerStaffVsPeer = $"{(revenuePerStaffVsPeer >= 0 ? "+" : "")}{Math.Round(revenuePerStaffVsPeer, 1)}%",
                insight = revenuePerStaffVsPeer < -15
                    ? $"Your revenue per staff is {Math.Abs(Math.Round(revenuePerStaffVsPeer, 0))}% below peer average — consider service price review or upsell training."
                    : revenueVsMedian > 20
                    ? "You're outperforming the median. Focus on maintaining quality to retain your advantage."
                    : "Your metrics are in line with peers. Growing review count is your highest-leverage action."
            },
            generatedAt = DateTime.UtcNow
        });
    }

    // NE3: Platform-Specific AI Fine-Tuning Data Export

    /// <summary>
    /// NE3: GET /intelligence/fine-tuning/export — Exports anonymized booking intent training data.
    /// Used to fine-tune booking intent models on platform-specific language (service names, industry terms).
    /// AI improves with every additional business that joins the platform — the data moat compounds.
    /// Admin/Owner only.
    /// </summary>
    [HttpGet("fine-tuning/export")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> ExportFineTuningData(
        [FromQuery] int limit = 1000,
        [FromQuery] string? industry = null)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;

        var bookingsQuery = _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.Status == BookingStatus.Completed);

        if (!string.IsNullOrWhiteSpace(industry))
        {
            var industryTenants = await _context.Tenants
                .Where(t => t.Industry != null && t.Industry.ToLower().Contains(industry.ToLower()))
                .Select(t => t.Id)
                .ToListAsync();
            bookingsQuery = bookingsQuery.Where(b => industryTenants.Contains(b.TenantId));
        }

        var samples = await bookingsQuery
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .Select(b => new
            {
                serviceName = b.Service != null ? b.Service.Name : "unknown",
                durationMinutes = (int)(b.EndTime - b.StartTime).TotalMinutes,
                source = b.Source.ToString(),
                dayOfWeek = b.StartTime.DayOfWeek.ToString(),
                hourOfDay = b.StartTime.Hour,
                category = b.Service != null ? b.Service.Category : null
            })
            .ToListAsync();

        // Format as JSONL fine-tuning records (OpenAI format)
        var fineTuningRecords = samples.Select(s => new
        {
            messages = new object[]
            {
                new { role = "system", content = "You are an AI booking assistant for a service business. Understand booking requests in natural language." },
                new { role = "user", content = $"I'd like to book a {s.serviceName}" },
                new { role = "assistant", content = $"I'd be happy to book a {s.serviceName} for you. It takes about {s.durationMinutes} minutes. What date and time works for you?" }
            }
        });

        _logger.LogInformation("[NE3] Fine-tuning data exported: {Count} samples for industry={Industry}",
            samples.Count, industry ?? "all");

        return Ok(new
        {
            format = "openai_jsonl",
            industry = industry ?? "all",
            sampleCount = samples.Count,
            exportedAt = DateTime.UtcNow,
            records = fineTuningRecords,
            instructions = new[]
            {
                "1. Download records as JSONL (one JSON object per line)",
                "2. Upload to Azure OpenAI fine-tuning API: POST /fine-tunes",
                "3. Use the resulting model ID in AzureOpenAIService._modelOverrides[industry]",
                "4. Re-export monthly as platform data grows"
            }
        });
    }
}
