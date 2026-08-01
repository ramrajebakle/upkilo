using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Programmatic SEO — public business discovery by category + city.
/// Powers upkilo.com/book/[category]/[city] SSG pages.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public class DiscoveryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DiscoveryController> _logger;
    private readonly IAIService _aiService;

    public DiscoveryController(AppDbContext context, ILogger<DiscoveryController> logger, IAIService aiService)
    {
        _context = context;
        _logger = logger;
        _aiService = aiService;
    }

    /// <summary>
    /// GET /api/v1/discovery/{category}/{city} — list businesses for SEO landing pages.
    /// Results are sorted by review count desc, then alphabetically.
    /// </summary>
    [HttpGet("{category}/{city}")]
    [ResponseCache(Duration = 3600)] // 1-hour cache for SEO pages
    public async Task<IActionResult> GetListings(
        string category,
        string city,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        // Map category slug to industry/business-type keywords
        var categoryKeywords = CategoryToKeywords(category);

        var query = _context.Tenants
            .Where(t => t.IsActive &&
                        !t.IsDeleted &&
                        t.City != null &&
                        EF.Functions.ILike(t.City, $"%{city.Replace("-", " ")}%"))
            .AsQueryable();

        if (categoryKeywords.Any())
        {
            query = query.Where(t =>
                categoryKeywords.Any(kw =>
                    EF.Functions.ILike(t.BusinessType ?? "", $"%{kw}%") ||
                    EF.Functions.ILike(t.Industry ?? "", $"%{kw}%")));
        }

        var total = await query.CountAsync();

        var listings = await query
            .OrderByDescending(t => t.ReviewCount)
            .ThenBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                slug = t.Slug,
                name = t.Name,
                city = t.City,
                industry = t.Industry,
                businessType = t.BusinessType,
                tagline = t.Tagline,
                logoUrl = t.LogoUrl,
                reviewCount = t.ReviewCount,
                averageRating = t.AverageRating,
                bookingUrl = $"/book/{t.Slug}"
            })
            .ToListAsync();

        _logger.LogInformation("[Discovery] Category={Category} City={City} Total={Total}", category, city, total);

        return Ok(new
        {
            category,
            city,
            page,
            pageSize,
            totalCount = total,
            totalPages = (int)Math.Ceiling((double)total / pageSize),
            listings,
            // SEO metadata for the page
            seo = new
            {
                title = $"Book {FormatCategory(category)} in {FormatCity(city)} | Upkilo",
                description = $"Find and book top {FormatCategory(category).ToLower()} in {FormatCity(city)}. " +
                              $"Instant online booking, real reviews. {total} businesses available.",
                canonicalUrl = $"/book/{category}/{city}"
            }
        });
    }

    /// <summary>
    /// GET /api/v1/discovery/categories — list all available categories with listing counts.
    /// Used to generate sitemap and navigation.
    /// </summary>
    [HttpGet("categories")]
    [ResponseCache(Duration = 86400)] // 24-hour cache
    public async Task<IActionResult> GetCategories()
    {
        var categories = new[]
        {
            new { slug = "hair-salons", label = "Hair Salons", keywords = new[] { "hair", "salon", "barber" } },
            new { slug = "nail-salons", label = "Nail Salons", keywords = new[] { "nail", "manicure", "pedicure" } },
            new { slug = "spas", label = "Spas & Massage", keywords = new[] { "spa", "massage", "wellness" } },
            new { slug = "fitness", label = "Fitness & Gyms", keywords = new[] { "gym", "fitness", "yoga", "pilates" } },
            new { slug = "beauty", label = "Beauty & Makeup", keywords = new[] { "beauty", "makeup", "cosmetic" } },
            new { slug = "tattoo", label = "Tattoo & Piercing", keywords = new[] { "tattoo", "piercing", "ink" } },
            new { slug = "medical-aesthetics", label = "Medical Aesthetics", keywords = new[] { "botox", "filler", "laser", "aesthetics" } },
            new { slug = "personal-training", label = "Personal Training", keywords = new[] { "personal trainer", "training", "coaching" } },
            new { slug = "therapy", label = "Therapy & Counselling", keywords = new[] { "therapy", "counselling", "psychology" } },
            new { slug = "dental", label = "Dental", keywords = new[] { "dental", "dentist", "orthodont" } },
        };

        return Ok(new { categories });
    }

    /// <summary>
    /// GET /api/v1/discovery/sitemap — returns all active slug combinations for sitemap generation.
    /// </summary>
    [HttpGet("sitemap")]
    [ResponseCache(Duration = 86400)]
    public async Task<IActionResult> GetSitemapEntries()
    {
        var activeTenants = await _context.Tenants
            .Where(t => t.IsActive && !t.IsDeleted && t.Slug != null)
            .Select(t => new { t.Slug, t.City, t.Industry, t.BusinessType })
            .ToListAsync();

        var entries = activeTenants
            .Where(t => t.City != null)
            .SelectMany(t => GetCategorySlug(t.Industry, t.BusinessType)
                .Select(cat => new
                {
                    url = $"/book/{cat}/{t.City!.ToLower().Replace(" ", "-")}",
                    lastMod = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                    changeFreq = "weekly",
                    priority = "0.8"
                }))
            .DistinctBy(e => e.url)
            .ToList();

        // Also add individual booking pages
        var bookingPages = activeTenants
            .Where(t => t.Slug != null)
            .Select(t => new
            {
                url = $"/book/{t.Slug}",
                lastMod = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                changeFreq = "daily",
                priority = "0.9"
            })
            .ToList();

        return Ok(new { entries, bookingPages, total = entries.Count + bookingPages.Count });
    }

    /// <summary>
    /// S5: GET /api/v1/discovery/{city}/{category}/near-me
    /// Geo-aware listing — sorts businesses by distance from the caller's lat/lng.
    /// Falls back to review-count sort if no location provided.
    /// Uses Haversine formula in-memory (move to PostGIS ST_Distance for scale).
    /// </summary>
    [HttpGet("{city}/{category}/near-me")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> NearMe(
        string city,
        string category,
        [FromQuery] double? lat = null,
        [FromQuery] double? lng = null,
        [FromQuery] double radiusKm = 25,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        var categoryKeywords = CategoryToKeywords(category);

        // Join through Location for geo coords
        var query = _context.Tenants
            .Where(t => t.IsActive && !t.IsDeleted)
            .Join(
                _context.Locations.Where(l => l.IsActive && l.Latitude.HasValue && l.Longitude.HasValue),
                t => t.Id,
                l => l.TenantId,
                (t, l) => new { Tenant = t, Location = l })
            .Where(x =>
                x.Location.City != null &&
                EF.Functions.ILike(x.Location.City, $"%{city.Replace("-", " ")}%"))
            .AsQueryable();

        if (categoryKeywords.Any())
            query = query.Where(x =>
                categoryKeywords.Any(kw =>
                    EF.Functions.ILike(x.Tenant.BusinessType ?? "", $"%{kw}%") ||
                    EF.Functions.ILike(x.Tenant.Industry ?? "", $"%{kw}%")));

        var raw = await query
            .Select(x => new
            {
                slug = x.Tenant.Slug,
                name = x.Tenant.Name,
                city = x.Location.City,
                latitude = x.Location.Latitude!.Value,
                longitude = x.Location.Longitude!.Value,
                tagline = x.Tenant.Tagline,
                logoUrl = x.Tenant.LogoUrl,
                reviewCount = x.Tenant.ReviewCount,
                averageRating = x.Tenant.AverageRating
            })
            .ToListAsync();

        // Haversine sort + radius filter
        var results = raw
            .Select(r =>
            {
                double distKm = lat.HasValue && lng.HasValue
                    ? Haversine(lat.Value, lng.Value, r.latitude, r.longitude)
                    : double.MaxValue;
                return new { r.slug, r.name, r.city, r.tagline, r.logoUrl, r.reviewCount, r.averageRating, distKm };
            })
            .Where(r => !lat.HasValue || r.distKm <= radiusKm)
            .OrderBy(r => r.distKm)
            .ThenByDescending(r => r.reviewCount)
            .ToList();

        var paged = results.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            city = FormatCity(city),
            category = FormatCategory(category),
            userLat = lat,
            userLng = lng,
            radiusKm,
            page,
            pageSize,
            totalCount = results.Count,
            listings = paged.Select(r => new
            {
                r.slug,
                r.name,
                r.city,
                r.tagline,
                r.logoUrl,
                r.reviewCount,
                r.averageRating,
                distanceKm = r.distKm < double.MaxValue ? Math.Round(r.distKm, 1) : (double?)null,
                bookingUrl = $"/book/{r.slug}"
            })
        });
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>
    /// MK3: GET /api/v1/discovery/best-of/{city} — AI-curated "Best of [City]" editorial page.
    /// Shows top-ranked businesses across categories with AI-generated intro copy.
    /// Designed for SEO landing pages and organic traffic.
    /// </summary>
    [HttpGet("best-of/{city}")]
    [ResponseCache(Duration = 21600)] // 6-hour cache
    public async Task<IActionResult> GetBestOf(string city)
    {
        var cityFormatted = FormatCity(city);

        // Top businesses by category for this city
        var topByCategory = await _context.Tenants
            .Where(t => t.IsActive && !t.IsDeleted &&
                        t.City != null && EF.Functions.ILike(t.City, $"%{city.Replace("-", " ")}%") &&
                        t.ReviewCount >= 3)
            .OrderByDescending(t => t.AverageRating * 0.5m + t.ReviewCount * 0.1m)
            .Take(20)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.Tagline,
                t.City,
                t.Industry,
                t.BusinessType,
                t.AverageRating,
                t.ReviewCount,
                t.LogoUrl,
                t.Settings,
                bookingUrl = $"/book/{t.Slug}",
                isVerified = t.Settings.ContainsKey("verifiedBadge")
            })
            .ToListAsync();

        // Group by industry
        var grouped = topByCategory
            .GroupBy(t => t.Industry ?? "Other")
            .Select(g => new
            {
                category = g.Key,
                businesses = g.Take(3).Select(b => new
                {
                    b.Name,
                    b.Slug,
                    b.Tagline,
                    b.AverageRating,
                    b.ReviewCount,
                    b.LogoUrl,
                    b.bookingUrl,
                    b.isVerified
                })
            })
            .ToList();

        // AI-generated editorial intro
        string editorial;
        try
        {
            var prompt =
                $"Write a 2-sentence engaging editorial intro for a 'Best of {cityFormatted}' service business guide.\n" +
                $"Top categories: {string.Join(", ", grouped.Select(g => g.category).Take(5))}.\n" +
                "Be warm, local, and specific. Don't mention Upkilo. Under 60 words.";

            var aiResult = await _aiService.GenerateTextAsync(Guid.Empty, null, prompt);
            editorial = aiResult.Success ? aiResult.Content?.Trim() ?? "" : "";
        }
        catch
        {
            editorial = "";
        }

        if (string.IsNullOrEmpty(editorial))
            editorial = $"Discover the best-reviewed service businesses in {cityFormatted}. "
                      + "Book appointments instantly with verified local providers.";

        return Ok(new
        {
            city = cityFormatted,
            citySlug = city.ToLower(),
            editorial,
            totalListings = topByCategory.Count,
            categories = grouped,
            seo = new
            {
                title = $"Best Service Businesses in {cityFormatted} {DateTime.UtcNow.Year} | Upkilo",
                description = $"Find the top-rated hair salons, spas, fitness studios, and more in {cityFormatted}. Verified reviews, instant booking.",
                canonicalUrl = $"/discover/best-of/{city.ToLower()}"
            },
            generatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// MK6: GET /api/v1/discovery/consumer/passport — Consumer loyalty passport showing cross-business points.
    /// Returns a consumer's accumulated points across all Upkilo businesses they've booked.
    /// </summary>
    [HttpGet("consumer/passport")]
    [Authorize]
    public async Task<IActionResult> GetLoyaltyPassport()
    {
        // Consumer identified by their email (JWT sub)
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                 ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(email))
            return Unauthorized(new { error = "Consumer email not in token." });

        // Find all completed bookings for this email across all businesses
        var bookings = await _context.Bookings
            .Include(b => b.Tenant)
            .Include(b => b.Service)
            .Where(b => b.CustomerEmail == email && b.Status == BookingStatus.Completed)
            .OrderByDescending(b => b.StartTime)
            .Select(b => new
            {
                tenantName = b.Tenant!.Name,
                tenantSlug = b.Tenant.Slug,
                serviceName = b.Service!.Name,
                b.StartTime,
                amountPaid = b.Price ?? 0m,
                points = (int)(b.Price ?? 0m / 10), // 1 point per $10 spent
            })
            .ToListAsync();

        var totalPoints = bookings.Sum(b => b.points);
        var businessesVisited = bookings.Select(b => b.tenantSlug).Distinct().Count();

        // Tier calculation
        var tier = totalPoints switch
        {
            >= 500 => "Gold",
            >= 200 => "Silver",
            >= 50 => "Bronze",
            _ => "Starter"
        };

        return Ok(new
        {
            email,
            totalPoints,
            tier,
            businessesVisited,
            nextTierPoints = totalPoints switch
            {
                >= 500 => null as int?,
                >= 200 => 500 - totalPoints,
                >= 50 => 200 - totalPoints,
                _ => 50 - totalPoints
            },
            recentVisits = bookings.Take(10),
            perks = tier switch
            {
                "Gold" => new[] { "15% off next booking at any Upkilo business", "Priority booking windows", "Free cancellation up to 2h prior" },
                "Silver" => new[] { "10% off next booking", "Skip the queue on popular time slots" },
                "Bronze" => new[] { "5% off next booking" },
                _ => new[] { "Earn 1 point per $10 spent. Reach Bronze (50 pts) for your first reward!" }
            },
            message = $"You're a {tier} member! {totalPoints} points earned across {businessesVisited} businesses."
        });
    }

    /// <summary>
    /// POST /api/v1/discovery/widget-click — log a "Powered by Upkilo" badge click.
    /// Called by the frontend signup interceptor page to attribute widget-driven signups.
    /// Logged as structured events; query from your log sink (Seq/Loki) for CTR analytics.
    /// </summary>
    [HttpPost("widget-click")]
    public IActionResult TrackWidgetClick([FromBody] WidgetClickRequest request)
    {
        _logger.LogInformation(
            "[WidgetCTR] source={SourceSlug} referrer={ReferrerUrl} timestamp={Timestamp}",
            request.SourceSlug?.Trim().ToLowerInvariant(),
            request.ReferrerUrl,
            DateTime.UtcNow.ToString("O"));

        return Ok(new { tracked = true });
    }

    private static List<string> CategoryToKeywords(string categorySlug) => categorySlug switch
    {
        "hair-salons" => new() { "hair", "salon", "barber" },
        "nail-salons" => new() { "nail", "manicure", "pedicure" },
        "spas" => new() { "spa", "massage", "wellness" },
        "fitness" => new() { "gym", "fitness", "yoga", "pilates" },
        "beauty" => new() { "beauty", "makeup", "cosmetic" },
        "tattoo" => new() { "tattoo", "piercing" },
        "medical-aesthetics" => new() { "botox", "filler", "laser", "aesthetics" },
        "personal-training" => new() { "personal trainer", "training", "coaching" },
        "therapy" => new() { "therapy", "counselling", "psychology" },
        "dental" => new() { "dental", "dentist" },
        _ => new()
    };

    private static IEnumerable<string> GetCategorySlug(string? industry, string? businessType)
    {
        var combined = $"{industry} {businessType}".ToLowerInvariant();
        if (combined.Contains("hair") || combined.Contains("salon") || combined.Contains("barber")) yield return "hair-salons";
        if (combined.Contains("nail") || combined.Contains("mani")) yield return "nail-salons";
        if (combined.Contains("spa") || combined.Contains("massage")) yield return "spas";
        if (combined.Contains("gym") || combined.Contains("fitness") || combined.Contains("yoga")) yield return "fitness";
        if (combined.Contains("beauty") || combined.Contains("makeup")) yield return "beauty";
        if (combined.Contains("tattoo") || combined.Contains("piercing")) yield return "tattoo";
    }

    private static string FormatCategory(string slug) =>
        string.Join(" ", slug.Split('-').Select(w => char.ToUpper(w[0]) + w[1..]));

    private static string FormatCity(string slug) =>
        string.Join(" ", slug.Split('-').Select(w => char.ToUpper(w[0]) + w[1..]));
}

public class WidgetClickRequest
{
    public string? SourceSlug { get; set; }    // the tenant whose booking widget was clicked
    public string? ReferrerUrl { get; set; }    // the page URL the user was on
    public string? IpHash { get; set; }         // SHA-256 hash of IP (privacy-safe deduplication)
}
