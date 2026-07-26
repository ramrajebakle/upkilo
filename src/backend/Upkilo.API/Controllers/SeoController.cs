using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/seo")]
public class SeoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SeoController> _logger;

    public SeoController(AppDbContext context, ILogger<SeoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// GET /api/seo/slugs — all active tenant slugs for sitemap
    [HttpGet("slugs")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetAllSlugs()
    {
        try
        {
            var slugs = await _context.Tenants
                .Where(t => !string.IsNullOrEmpty(t.Slug))
                .Select(t => t.Slug)
                .ToListAsync();
            return Ok(new { slugs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tenant slugs");
            return Ok(new { slugs = Array.Empty<string>() });
        }
    }

    /// GET /api/seo/meta/{slug} — public metadata for generateMetadata + JSON-LD
    [HttpGet("meta/{slug}")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetTenantMeta(string slug)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Locations)
            .Include(t => t.Services.Where(s => s.IsActive))
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (tenant == null) return NotFound(new { error = "Tenant not found" });

        var primary = tenant.Locations.FirstOrDefault(l => l.IsPrimary) ?? tenant.Locations.FirstOrDefault();
        var reviews = await _context.ExternalReviews.Where(r => r.TenantId == tenant.Id).ToListAsync();
        var avgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0.0;

        tenant.Settings.TryGetValue("website", out var wsite);

        return Ok(new
        {
            name         = tenant.Name,
            slug         = tenant.Slug,
            description  = tenant.Description,
            logo         = tenant.LogoUrl,
            primaryColor = tenant.PrimaryColor,
            phone        = primary?.Phone ?? tenant.Phone,
            email        = primary?.Email ?? tenant.Email,
            website      = wsite?.ToString(),
            industry     = tenant.Industry,
            address = primary == null ? null : (object)new
            {
                line1      = primary.AddressLine1,
                city       = primary.City,
                state      = primary.State,
                postalCode = primary.PostalCode,
                country    = primary.Country,
            },
            services = tenant.Services.Take(10).Select(s => new { s.Name, s.Price, s.DurationMinutes }),
            reviews  = new { averageRating = avgRating, totalCount = reviews.Count },
        });
    }

    /// GET /api/seo/audit — authenticated, full SEO health score for the tenant
    [HttpGet("audit")]
    [Authorize]
    public async Task<IActionResult> GetAudit([FromServices] ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.GetTenantId();
        var tenant = await _context.Tenants
            .Include(t => t.Locations)
            .Include(t => t.Services.Where(s => s.IsActive))
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null) return NotFound();

        var primary   = tenant.Locations.FirstOrDefault(l => l.IsPrimary) ?? tenant.Locations.FirstOrDefault();
        var reviews   = await _context.ExternalReviews.Where(r => r.TenantId == tenantId).ToListAsync();
        var blogCount = await _context.Set<Upkilo.Core.Entities.BlogPost>()
            .CountAsync(p => p.TenantId == tenantId && p.Status == "Published");

        tenant.Metadata.TryGetValue("seo_keywords", out var kw);
        var keywords = kw?.ToString();

        var checks = new[]
        {
            new SeoCheck("booking_url",   "Booking URL set",           !string.IsNullOrEmpty(tenant.Slug),                                             10, "critical", "Your booking URL is how clients discover you online. Set it to your business name."),
            new SeoCheck("name",          "Business Name",             !string.IsNullOrEmpty(tenant.Name),                                             10, "critical", "Google shows this name in search results and Google Maps."),
            new SeoCheck("description",   "Description (50+ chars)",   !string.IsNullOrEmpty(tenant.Description) && tenant.Description!.Length >= 50, 15, "high",     "Write 50-160 chars describing your services and area. This is your Google search preview."),
            new SeoCheck("phone",         "Phone Number",              !string.IsNullOrEmpty(tenant.Phone ?? primary?.Phone),                          8,  "high",     "Google uses your phone number to verify and match your business listing."),
            new SeoCheck("address",       "Physical Address",          primary != null && !string.IsNullOrEmpty(primary.City),                         12, "high",     "Your address puts you in 'near me' searches. Biggest local SEO factor after reviews."),
            new SeoCheck("logo",          "Logo or Business Photo",    !string.IsNullOrEmpty(tenant.LogoUrl),                                          8,  "medium",   "Businesses with photos get 42% more direction requests and 35% more clicks."),
            new SeoCheck("keywords",      "SEO Keywords added",        !string.IsNullOrEmpty(keywords),                                                7,  "medium",   "Add keywords your clients type into Google to find your type of business."),
            new SeoCheck("services",      "3 or more Services listed", tenant.Services.Count >= 3,                                                     10, "high",     "Each service is a keyword Google can index and rank your booking page for."),
            new SeoCheck("reviews",       "3+ Customer Reviews",       reviews.Count >= 3,                                                             15, "critical", "Reviews are the #1 local SEO ranking signal. 3+ reviews dramatically improves visibility."),
            new SeoCheck("review_recent", "Review in last 30 days",    reviews.Any(r => r.ReviewDate >= DateTime.UtcNow.AddDays(-30)),                 5,  "medium",   "Recent reviews signal Google your business is open and active."),
            new SeoCheck("blog",          "Blog post published",       blogCount >= 1,                                                                 0,  "low",      "Blog posts help you rank for long-tail keywords like 'how much does a haircut cost in [city]'."),
        };

        var score = checks.Where(c => c.Passed).Sum(c => c.Weight);

        return Ok(new
        {
            score,
            grade  = score >= 85 ? "A" : score >= 70 ? "B" : score >= 50 ? "C" : "D",
            checks,
            summary = new
            {
                totalReviews      = reviews.Count,
                avgRating         = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0.0,
                publishedPosts    = blogCount,
                servicesListed    = tenant.Services.Count,
                descriptionLength = tenant.Description?.Length ?? 0,
                recentReviews     = reviews.Count(r => r.ReviewDate >= DateTime.UtcNow.AddDays(-30)),
            }
        });
    }

    /// GET /api/seo/keywords — keyword suggestions based on tenant services + city
    [HttpGet("keywords")]
    [Authorize]
    public async Task<IActionResult> GetKeywords([FromServices] ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.GetTenantId();
        var tenant = await _context.Tenants
            .Include(t => t.Locations)
            .Include(t => t.Services.Where(s => s.IsActive))
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null) return NotFound();

        var city = tenant.Locations.FirstOrDefault(l => l.IsPrimary)?.City
                ?? tenant.Locations.FirstOrDefault()?.City
                ?? string.Empty;

        var suggestions = new List<object>();
        foreach (var svc in tenant.Services.Take(6))
        {
            var name = svc.Name.ToLower();
            suggestions.Add(new { keyword = name + " near me",          intent = "local",         volume = "High",   tip = "People searching right now for your service." });
            suggestions.Add(new { keyword = "book " + name + " online", intent = "transactional", volume = "Medium", tip = "High-intent searchers ready to book." });
            if (!string.IsNullOrEmpty(city))
            {
                suggestions.Add(new { keyword = name + " " + city.ToLower(),                 intent = "local",       volume = "High",   tip = "Your city + service is the highest value keyword." });
                suggestions.Add(new { keyword = "best " + name + " in " + city.ToLower(),    intent = "commercial",  volume = "Medium", tip = "Comparison shoppers looking for top-rated businesses." });
                suggestions.Add(new { keyword = name + " " + city.ToLower() + " prices",     intent = "commercial",  volume = "Low",    tip = "Price-conscious clients early in their search." });
            }
        }

        return Ok(new { city, suggestions });
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "ok", timestamp = DateTime.UtcNow });
}

internal record SeoCheck(string Id, string Label, bool Passed, int Weight, string Priority, string Tip);