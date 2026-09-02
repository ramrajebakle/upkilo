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

    public DiscoveryController(AppDbContext context, ILogger<DiscoveryController> logger)
    {
        _context = context;
        _logger = logger;
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
            new { slug = "spas", label = "Spas", keywords = new[] { "spa", "wellness" } },
            // Massage split out of "spas": it was only reachable as a spa keyword, so a business
            // that calls itself a massage clinic had no category of its own to rank in.
            new { slug = "massage", label = "Massage", keywords = new[] { "massage", "bodywork", "sports massage" } },
            // "fitness" (Fitness & Gyms — gym/fitness/yoga/pilates) removed: Upkilo no longer
            // serves that vertical. This list drives the public /book/{category}/{city} pages and
            // the discovery sitemap, so leaving it here would keep publishing landing pages for
            // businesses the product does not support. "personal-training" went with it — same
            // vertical, same reason.
            new { slug = "beauty", label = "Beauty & Aesthetic Clinics", keywords = new[] { "beauty", "makeup", "cosmetic", "aesthetic clinic" } },
            new { slug = "tattoo", label = "Tattoo & Piercing", keywords = new[] { "tattoo", "piercing", "ink" } },
            new { slug = "medical-aesthetics", label = "Med Spas & Medical Aesthetics", keywords = new[] { "med spa", "medspa", "botox", "filler", "laser", "aesthetics" } },
            // Physiotherapy and chiropractic are appointment-led clinical practices: recurring
            // visits, treatment plans and insurance pre-auth, all of which the medical vertical
            // already covers. They are listed separately rather than folded into "therapy",
            // which here means counselling and psychology.
            new { slug = "physiotherapy", label = "Physiotherapy", keywords = new[] { "physio", "physiotherapy", "physical therapy", "rehab" } },
            new { slug = "chiropractic", label = "Chiropractic", keywords = new[] { "chiro", "chiropractic", "chiropractor" } },
            new { slug = "therapy", label = "Therapy & Counselling", keywords = new[] { "therapy", "counselling", "psychology" } },
            new { slug = "dental", label = "Dental", keywords = new[] { "dental", "dentist", "orthodont" } },
            // Auto detailing is the one category here where the work is done on a vehicle rather
            // than a person. It earns its place on the same mechanics as the rest — long,
            // variable job durations, bay and technician scheduling, deposits on high-value work —
            // and is supported by the Vehicle record and per-vehicle-class pricing, so a quote
            // reflects an SUV taking longer than a coupe.
            new { slug = "auto-detailing", label = "Auto Detailing", keywords = new[] { "detailing", "car detailing", "auto detail", "ceramic coating", "paint correction", "car wash" } },
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
    // IAIService is injected per-action; see ServicesController for why a constructor
    // dependency here made every endpoint on this controller construct the AI stack.
    public async Task<IActionResult> GetBestOf(string city, [FromServices] IAIService aiService)
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

            var aiResult = await aiService.GenerateTextAsync(Guid.Empty, null, prompt);
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
        "spas" => new() { "spa", "wellness" },
        "massage" => new() { "massage", "bodywork", "sports massage" },
        // "fitness" and "personal-training" removed — see GetCategories. An unmatched slug falls
        // through to the empty default, so a stale /book/fitness/... URL now resolves to no
        // keywords and no results rather than silently continuing to serve the retired category.
        "beauty" => new() { "beauty", "makeup", "cosmetic", "aesthetic clinic" },
        "tattoo" => new() { "tattoo", "piercing" },
        "medical-aesthetics" => new() { "med spa", "medspa", "botox", "filler", "laser", "aesthetics" },
        "physiotherapy" => new() { "physio", "physiotherapy", "physical therapy", "rehab" },
        "chiropractic" => new() { "chiro", "chiropractic", "chiropractor" },
        "therapy" => new() { "therapy", "counselling", "psychology" },
        "dental" => new() { "dental", "dentist" },
        "auto-detailing" => new() { "detailing", "car detailing", "auto detail", "ceramic coating", "paint correction", "car wash" },
        _ => new()
    };

    /// <summary>
    /// Maps a tenant's own industry/business-type text onto the discovery categories it belongs in.
    /// </summary>
    /// <remarks>
    /// This decides which tenants a /book/{category}/{city} page can list, and therefore which of
    /// those pages the sitemap advertises at all. It previously covered only five of the ten
    /// categories GetCategories published: medical-aesthetics, therapy and dental had no mapping,
    /// so no tenant could ever be placed in them and those landing pages could only ever come out
    /// empty. The sitemap fails closed on empty categories, so the symptom was silent — the pages
    /// simply never earned any traffic.
    ///
    /// Med spa is tested before the generic spa check and excluded from it: "med spa" contains
    /// "spa", so without that the most clinical businesses on the platform would be filed under
    /// general wellness.
    /// </remarks>
    private static IEnumerable<string> GetCategorySlug(string? industry, string? businessType)
    {
        var combined = $"{industry} {businessType}".ToLowerInvariant();

        var isMedSpa = combined.Contains("med spa") || combined.Contains("medspa")
            || combined.Contains("medical spa") || combined.Contains("aesthetic")
            || combined.Contains("botox") || combined.Contains("filler") || combined.Contains("laser");

        if (combined.Contains("hair") || combined.Contains("salon") || combined.Contains("barber")) yield return "hair-salons";
        if (combined.Contains("nail") || combined.Contains("mani") || combined.Contains("pedi")) yield return "nail-salons";
        if (isMedSpa) yield return "medical-aesthetics";
        if (!isMedSpa && combined.Contains("spa")) yield return "spas";
        if (combined.Contains("massage") || combined.Contains("bodywork")) yield return "massage";
        if (combined.Contains("beauty") || combined.Contains("makeup") || combined.Contains("cosmetic")) yield return "beauty";
        if (combined.Contains("tattoo") || combined.Contains("piercing")) yield return "tattoo";
        if (combined.Contains("physio") || combined.Contains("physical therapy") || combined.Contains("rehab")) yield return "physiotherapy";
        if (combined.Contains("chiro")) yield return "chiropractic";
        if (combined.Contains("counsel") || combined.Contains("psycholog") || combined.Contains("therapist")) yield return "therapy";
        if (combined.Contains("dental") || combined.Contains("dentist") || combined.Contains("orthodont")) yield return "dental";
        if (combined.Contains("detail") || combined.Contains("car wash") || combined.Contains("ceramic coating")) yield return "auto-detailing";
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
