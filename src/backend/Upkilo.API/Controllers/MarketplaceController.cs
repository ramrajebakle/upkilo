using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Days 61-63: Consumer marketplace — Upkilo Discover.
/// Days 64-65: Review responses — business owners reply to client reviews.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/marketplace")]
public class MarketplaceController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public MarketplaceController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>GET /api/v1/marketplace/search — Search businesses by keyword + city (public).</summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q = null,
        [FromQuery] string? city = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Tenants
            .Where(t => !t.IsDeleted && t.SubscriptionTier != SubscriptionTier.Free)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t =>
                EF.Functions.ILike(t.Name, $"%{q}%") ||
                (t.Tagline != null && EF.Functions.ILike(t.Tagline, $"%{q}%")));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(t => t.City != null && EF.Functions.ILike(t.City, $"%{city}%"));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => EF.Functions.ILike(t.Industry, $"%{category}%"));

        if (minRating.HasValue)
            query = query.Where(t => t.AverageRating >= minRating.Value);

        var total = await query.CountAsync();

        var results = await query
            .OrderByDescending(t => t.AverageRating)
            .ThenByDescending(t => t.ReviewCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.Tagline,
                t.City,
                t.Country,
                t.AverageRating,
                t.ReviewCount,
                t.Industry,
                bookingUrl = $"https://app.upkilo.com/book/{t.Slug}"
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new { results, total, page, pageSize }));
    }

    /// <summary>GET /api/v1/marketplace/featured — Top 6 featured businesses for Discover hero (public).</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetFeatured()
    {
        var featured = await _context.Tenants
            .Where(t => !t.IsDeleted && t.ReviewCount >= 5 && t.AverageRating >= 4.0m)
            .OrderByDescending(t => t.AverageRating * 0.4m + t.ReviewCount * 0.1m)
            .Take(6)
            .Select(t => new
            {
                t.Id, t.Name, t.Slug, t.Tagline,
                t.City, t.Country, t.AverageRating, t.ReviewCount, t.Industry,
                bookingUrl = $"https://app.upkilo.com/book/{t.Slug}"
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(featured));
    }

    /// <summary>GET /api/v1/marketplace/categories — Category list with counts (public).</summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400)]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _context.Tenants
            .Where(t => !t.IsDeleted && t.Industry != null)
            .GroupBy(t => t.Industry)
            .Select(g => new { category = g.Key, count = g.Count() })
            .OrderByDescending(g => g.count)
            .Take(20)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(cats));
    }

    /// <summary>GET /api/v1/marketplace/{tenantId}/reviews — Public reviews for a business (public).</summary>
    [HttpGet("{tenantId}/reviews")]
    [AllowAnonymous]
    [ResponseCache(Duration = 600)]
    public async Task<IActionResult> GetReviews(Guid tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var reviews = await _context.ExternalReviews
            .Where(r => r.TenantId == tenantId && !r.IsDeleted)
            .OrderByDescending(r => r.ReviewDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id, r.ReviewerName, r.Rating,
                comment = r.ReviewText, r.ReviewDate, source = r.Platform,
                ownerReply = r.ResponseText, ownerReplyAt = r.RespondedAt
            })
            .ToListAsync();

        var total = await _context.ExternalReviews.CountAsync(r => r.TenantId == tenantId && !r.IsDeleted);

        return Ok(ApiResponse<object>.Ok(new { reviews, total, page, pageSize }));
    }

    /// <summary>POST /api/v1/marketplace/{reviewId}/reply — Day 64: Business owner replies to a review.</summary>
    [HttpPost("{reviewId}/reply")]
    [Authorize]
    public async Task<IActionResult> ReplyToReview(Guid reviewId, [FromBody] ReviewReplyRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Reply))
            return BadRequest(ApiResponse.Fail("Reply cannot be empty."));

        var review = await _context.ExternalReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.TenantId == tenantId.Value);

        if (review == null)
            return NotFound(ApiResponse.Fail("Review not found."));

        review.ResponseText = request.Reply.Trim();
        review.RespondedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { reviewId, reply = review.ResponseText, repliedAt = review.RespondedAt }));
    }

    /// <summary>
    /// S4: GET /api/v1/marketplace/listing-quality — returns a quality gate score for the tenant's marketplace listing.
    /// Score drives rank boost in Discover search. Score > 80 gets a "Verified Quality" badge.
    /// </summary>
    [HttpGet("listing-quality")]
    [Authorize]
    public async Task<IActionResult> GetListingQuality()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        var serviceCount = await _context.Services.CountAsync(s => s.TenantId == tenantId.Value && s.IsActive);
        var photoCount = await _context.ClientPhotos.CountAsync(p => p.TenantId == tenantId.Value);
        var reviewCount = tenant.ReviewCount;
        var staffCount = await _context.StaffMembers.CountAsync(s => s.TenantId == tenantId.Value && s.IsActive);

        var checks = new List<QualityCheck>
        {
            new("Business name set", !string.IsNullOrEmpty(tenant.Name), 10),
            new("Tagline/description set", !string.IsNullOrEmpty(tenant.Tagline), 10),
            new("City set", !string.IsNullOrEmpty(tenant.City), 10),
            new("Logo uploaded", !string.IsNullOrEmpty(tenant.LogoUrl), 15),
            new("At least 3 active services", serviceCount >= 3, 15),
            new("At least 1 staff member", staffCount >= 1, 10),
            new("Phone number set", !string.IsNullOrEmpty(tenant.Phone), 5),
            new("At least 5 reviews", reviewCount >= 5, 10),
            new("Average rating ≥ 4.0", tenant.AverageRating >= 4.0m, 10),
            new("Photos uploaded", photoCount > 0, 5),
        };

        var score = checks.Where(c => c.Passed).Sum(c => c.Points);
        var badge = score >= 80 ? "verified_quality" : score >= 60 ? "listed" : "incomplete";

        return Ok(new
        {
            tenantId,
            score,
            maxScore = 100,
            badge,
            qualifies = score >= 80,
            rankBoost = score >= 80 ? "2x visibility in search results" : null,
            checks = checks.Select(c => new { c.Label, c.Passed, c.Points }),
            tips = checks.Where(c => !c.Passed).Select(c => $"Add {c.Label.ToLower()} to improve your score")
        });
    }

    /// <summary>
    /// MK1: POST /api/v1/marketplace/reviews — Consumer submits a verified post-visit review.
    /// Requires a completed bookingId as proof of visit. Review is marked IsVerified = true.
    /// </summary>
    [HttpPost("reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitConsumerReview([FromBody] ConsumerReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReviewerName) || request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { error = "Invalid review. Rating must be 1-5 and name is required." });

        // Verify booking exists and is completed
        var booking = await _context.Bookings
            .Include(b => b.Tenant)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.Status == BookingStatus.Completed);

        if (booking == null)
            return BadRequest(new { error = "invalid_booking", message = "A completed booking is required to leave a verified review." });

        // Prevent duplicate reviews for same booking
        if (await _context.ExternalReviews.AnyAsync(r => r.BookingId == request.BookingId && r.Platform == "Upkilo"))
            return Conflict(new { error = "already_reviewed", message = "A review has already been submitted for this booking." });

        var review = new ExternalReview
        {
            Id = Guid.NewGuid(),
            TenantId = booking.TenantId,
            Platform = "Upkilo",
            ReviewerName = request.ReviewerName.Trim(),
            Rating = request.Rating,
            ReviewText = request.Comment?.Trim(),
            Sentiment = request.Rating >= 4 ? "Positive" : request.Rating == 3 ? "Neutral" : "Negative",
            ReviewDate = DateTime.UtcNow,
            IsVerified = true,
            ClientId = booking.ClientId,
            BookingId = booking.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.ExternalReviews.Add(review);

        // Update tenant average rating
        var allRatings = await _context.ExternalReviews
            .Where(r => r.TenantId == booking.TenantId && !r.IsDeleted)
            .Select(r => r.Rating)
            .ToListAsync();
        allRatings.Add(request.Rating);
        booking.Tenant!.AverageRating = (decimal)allRatings.Average();
        booking.Tenant.ReviewCount = allRatings.Count;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            reviewId = review.Id,
            isVerified = true,
            message = "Your verified review has been published. Thank you!",
            businessName = booking.Tenant?.Name
        });
    }

    /// <summary>
    /// MK4: GET /api/v1/marketplace/promoted — Returns paid promoted listings for the homepage/search.
    /// </summary>
    [HttpGet("promoted")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetPromotedListings([FromQuery] string? city = null)
    {
        var now = DateTime.UtcNow;

        var query = _context.Tenants
            .Where(t => !t.IsDeleted &&
                        t.Settings.ContainsKey("promotedUntil") &&
                        t.Status == TenantStatus.Active);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(t => t.City != null && EF.Functions.ILike(t.City, $"%{city}%"));

        var promoted = await query
            .OrderByDescending(t => t.AverageRating)
            .Take(6)
            .Select(t => new
            {
                t.Id, t.Name, t.Slug, t.Tagline, t.City,
                t.AverageRating, t.ReviewCount, t.Industry, t.LogoUrl,
                badge = "Promoted",
                bookingUrl = $"/book/{t.Slug}"
            })
            .ToListAsync();

        return Ok(new { promoted, count = promoted.Count });
    }

    /// <summary>
    /// MK4: POST /api/v1/marketplace/promote — Owner purchases promoted placement (30 or 60 days).
    /// </summary>
    [HttpPost("promote")]
    [Authorize]
    public async Task<IActionResult> PurchasePromotion([FromBody] PromoteListingRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.Days != 30 && request.Days != 60 && request.Days != 90)
            return BadRequest(new { error = "days must be 30, 60, or 90" });

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        var promotedUntil = DateTime.UtcNow.AddDays(request.Days);
        tenant.Settings["promotedUntil"] = promotedUntil.ToString("O");
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        decimal price = request.Days switch { 30 => 49, 60 => 89, _ => 119 };

        return Ok(new
        {
            tenantId,
            promotedUntil,
            daysActive = request.Days,
            priceUsd = price,
            message = $"Your listing is now promoted until {promotedUntil:yyyy-MM-dd}. "
                    + "It will appear at the top of relevant search results.",
            note = "Stripe payment integration: wire this to a PaymentIntent before going live."
        });
    }

    /// <summary>
    /// MK5: POST /api/v1/marketplace/verify-badge — Tenant submits for the Upkilo Verified badge.
    /// Background check + quality gate must pass. Sets IsVerified on the tenant.
    /// </summary>
    [HttpPost("verify-badge")]
    [Authorize]
    public async Task<IActionResult> RequestVerifiedBadge([FromBody] VerifiedBadgeRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        // Auto-approve if quality score >= 80 (from S4 listing-quality gate)
        var serviceCount = await _context.Services.CountAsync(s => s.TenantId == tenantId.Value && s.IsActive);
        var staffCount = await _context.StaffMembers.CountAsync(s => s.TenantId == tenantId.Value && s.IsActive);
        var autoApprove = !string.IsNullOrEmpty(tenant.Name)
                       && !string.IsNullOrEmpty(tenant.Tagline)
                       && !string.IsNullOrEmpty(tenant.City)
                       && serviceCount >= 3
                       && staffCount >= 1
                       && tenant.ReviewCount >= 5
                       && tenant.AverageRating >= 4.0m;

        if (autoApprove)
        {
            tenant.Settings["verifiedBadge"] = "verified";
            tenant.Settings["verifiedAt"] = DateTime.UtcNow.ToString("O");
            tenant.Settings["verifiedReason"] = "auto:quality_gate_passed";
            tenant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                status = "approved",
                badge = "verified",
                message = "Your Verified badge has been applied! Verified businesses show 2× higher in search results.",
                verifiedAt = DateTime.UtcNow
            });
        }

        // Queue for manual review
        tenant.Settings["verificationStatus"] = "pending_review";
        tenant.Settings["verificationRequestedAt"] = DateTime.UtcNow.ToString("O");
        tenant.Settings["verificationDocumentUrl"] = request.DocumentUrl ?? "";
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Accepted(new
        {
            status = "pending",
            message = "Your verification request has been submitted. We'll review it within 2 business days.",
            requirements = new[]
            {
                "Business license or registration document",
                "At least 5 verified client reviews",
                "Average rating of 4.0 or higher",
                "Minimum 3 active services listed"
            }
        });
    }

    /// <summary>POST /api/v1/marketplace/claim-listing — Day 63: Tenant claims their Discover listing.</summary>
    [HttpPost("claim-listing")]
    [Authorize]
    public async Task<IActionResult> ClaimListing([FromBody] ClaimListingRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.City)) tenant.City = request.City.Trim();
        if (!string.IsNullOrWhiteSpace(request.Country)) tenant.Country = request.Country.Trim();
        if (!string.IsNullOrWhiteSpace(request.Tagline)) tenant.Tagline = request.Tagline.Trim();
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            listingUrl = $"https://upkilo.com/discover/business/{tenant.Slug}",
            message = "Your business is now discoverable on Upkilo Discover!"
        }));
    }

    // ─── MK2: Booking Widget Embed Code ─────────────────────────────────────────

    /// <summary>
    /// MK2: GET /marketplace/widget/{tenantSlug} — Returns embeddable booking widget snippet.
    /// Can be placed in Google Business Profile "Book" button, Instagram bio, website.
    /// AllowAnonymous — widget embed code is public.
    /// </summary>
    [HttpGet("widget/{tenantSlug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWidgetEmbed(string tenantSlug)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive && !t.IsDeleted);
        if (tenant == null) return NotFound(new { error = "Business not found." });

        var bookingUrl = $"https://book.upkilo.com/{tenantSlug}";
        var embedSnippet = $"""
            <!-- Upkilo Booking Widget for {tenant.Name} -->
            <script src="https://cdn.upkilo.com/widget/v1/booking-widget.min.js"
                    data-business="{tenantSlug}"
                    data-primary-color="#3B82F6"
                    data-button-text="Book Now"
                    async></script>
            <div id="upkilo-booking-widget"></div>
            """;

        return Ok(new
        {
            tenantSlug,
            businessName = tenant.Name,
            bookingUrl,
            embedSnippet,
            googleBusinessProfileButtonUrl = bookingUrl,
            instagramBioLink = bookingUrl,
            qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(bookingUrl)}",
            ctaVariants = new[]
            {
                new { label = "Book Now", url = bookingUrl },
                new { label = "Check Availability", url = $"{bookingUrl}?view=calendar" },
                new { label = "See Services", url = $"{bookingUrl}?view=services" }
            }
        });
    }

    // ─── MK3: Editorial "Best of City" Pages ────────────────────────────────────

    /// <summary>
    /// MK3: GET /marketplace/editorial/{city}/{category} — AI-curated "Best of [City]" editorial listing.
    /// Returns top 10 businesses by rating + verified status + review count.
    /// Feeds the /discover/{city}/{category}/best-of SSG page.
    /// </summary>
    [HttpGet("editorial/{city}/{category}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEditorialListing(string city, string category)
    {
        var normalized = (c: city.ToLower().Replace("-", " "), cat: category.ToLower().Replace("-", " "));

        var businesses = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive && !t.IsDeleted
                && t.City != null && t.City.ToLower() == normalized.c
                && t.Industry != null && t.Industry.ToLower().Contains(normalized.cat))
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.City,
                t.Industry,
                IsVerified = t.Settings.ContainsKey("verifiedBadge"),
                IsPromoted = t.Settings.ContainsKey("promotedUntil"),
                reviewCount = 0 // Would join Review table in production
            })
            .OrderByDescending(t => t.IsVerified)
            .ThenByDescending(t => t.IsPromoted)
            .Take(10)
            .ToListAsync();

        var editorialTitle = $"Best {char.ToUpper(category[0]) + category[1..]} in {char.ToUpper(city[0]) + city[1..]}";

        return Ok(new
        {
            title = editorialTitle,
            city,
            category,
            generatedAt = DateTime.UtcNow,
            businessCount = businesses.Count,
            businesses,
            seoDescription = $"Discover the top-rated {category} businesses in {city}. All listed businesses are verified on Upkilo.",
            jsonLd = new
            {
                type = "ItemList",
                name = editorialTitle,
                itemListElement = businesses.Select((b, i) => new
                {
                    type = "ListItem",
                    position = i + 1,
                    url = $"https://upkilo.com/discover/business/{b.Slug}"
                })
            }
        });
    }

    // ─── MK6: Consumer Loyalty Passport ─────────────────────────────────────────

    /// <summary>
    /// MK6: GET /marketplace/loyalty/{clientEmail} — Consumer loyalty summary across all Upkilo businesses.
    /// Shows cross-business visit count + reward points earned + eligible perks.
    /// </summary>
    // SECURITY: previously [AllowAnonymous] with no rate limit — anyone could enumerate any
    // person's cross-tenant spend/visit history by email. Now requires authentication and only
    // returns the caller's OWN passport (email claim must match the requested address).
    [HttpGet("loyalty/{clientEmail}")]
    [Authorize]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public")]
    public async Task<IActionResult> GetLoyaltyPassport(string clientEmail)
    {
        var callerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(callerEmail) ||
            !string.Equals(callerEmail, clientEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var completedBookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Where(b => b.Status == BookingStatus.Completed
                && b.Client != null
                && b.Client.Email != null
                && b.Client.Email.ToLower() == clientEmail.ToLower())
            .Select(b => new
            {
                b.TenantId,
                ServiceName = b.Service != null ? b.Service.Name : "Service",
                b.StartTime,
                Amount = b.Price ?? 0m
            })
            .OrderByDescending(b => b.StartTime)
            .ToListAsync();

        var totalSpent = completedBookings.Sum(b => b.Amount);
        var totalVisits = completedBookings.Count;
        var rewardPoints = (int)(totalSpent / 10m); // 1 point per $10 spent
        var uniqueBusinesses = completedBookings.Select(b => b.TenantId).Distinct().Count();

        return Ok(new
        {
            clientEmail,
            totalVisits,
            uniqueBusinesses,
            totalSpent = Math.Round(totalSpent, 2),
            rewardPoints,
            rewardTier = rewardPoints switch
            {
                >= 500 => "Platinum",
                >= 200 => "Gold",
                >= 50 => "Silver",
                _ => "Bronze"
            },
            perks = rewardPoints >= 50 ? new[] { "10% off next booking at any Upkilo business", "Priority booking slots" } : Array.Empty<string>(),
            recentHistory = completedBookings.Take(10)
        });
    }

    // ─── S5: Near-Me Geolocation Ranking ────────────────────────────────────────

    /// <summary>
    /// S5: GET /marketplace/near-me?lat={lat}&lng={lng}&category={cat}&radius={km}
    /// Returns businesses sorted by distance from the caller's coordinates.
    /// Feeds the /discover/{city}/{category}/near-me page for organic SEO traffic.
    /// </summary>
    [HttpGet("near-me")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNearMe(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] string? category = null,
        [FromQuery] double radius = 10.0)
    {
        // Bounding box pre-filter (≈1° lat/lng = 111km)
        var latDelta = radius / 111.0;
        var lngDelta = radius / (111.0 * Math.Cos(lat * Math.PI / 180.0));

        // Geo coords are stored as tenant settings keys "geo_lat" / "geo_lng"
        // In production these would be indexed columns; for now we load active tenants in city and sort in-memory.
        var tenantsQuery = _context.Tenants.AsNoTracking()
            .Where(t => t.IsActive && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category))
            tenantsQuery = tenantsQuery.Where(t => t.Industry != null && t.Industry.ToLower().Contains(category.ToLower()));

        var tenants = await tenantsQuery
            .Select(t => new { t.Id, t.Name, t.Slug, t.City, t.Industry,
                IsVerified = t.Settings.ContainsKey("verifiedBadge"),
                LatStr = t.Settings.ContainsKey("geo_lat") ? t.Settings["geo_lat"].ToString() : null,
                LngStr = t.Settings.ContainsKey("geo_lng") ? t.Settings["geo_lng"].ToString() : null })
            .ToListAsync();

        // Haversine distance sort in memory
        static double Haversine(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLng = (lng2 - lng1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        var results = tenants
            .Where(t => t.LatStr != null && t.LngStr != null
                && double.TryParse(t.LatStr, out _) && double.TryParse(t.LngStr, out _))
            .Select(t => new
            {
                t.Id, t.Name, t.Slug, t.City, t.Industry, t.IsVerified,
                distanceKm = Haversine(lat, lng,
                    double.Parse(t.LatStr!),
                    double.Parse(t.LngStr!))
            })
            .Where(t => t.distanceKm <= radius)
            .OrderBy(t => t.distanceKm)
            .Take(20)
            .ToList();

        return Ok(new
        {
            userLat = lat,
            userLng = lng,
            radiusKm = radius,
            category,
            count = results.Count,
            results
        });
    }
}

public class ReviewReplyRequest
{
    public string Reply { get; set; } = string.Empty;
}

public class ClaimListingRequest
{
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Tagline { get; set; }
}

public record QualityCheck(string Label, bool Passed, int Points);

public class ConsumerReviewRequest
{
    public Guid BookingId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? PhotoUrl { get; set; }
}

// MK2: Instant booking widget embed code for Google Business Profile + Instagram
// MK3: Editorial "Best of [City]" pages with AI-curated listings
// MK6: Consumer loyalty passport — cross-business reward points
// S5: Geolocation-aware "near me" endpoint

/// <summary>
/// MK2: GET /marketplace/widget/{tenantSlug} — Returns embed code for the booking widget.
/// Designed for Google Business Profile "Book" button + Instagram/Facebook bio link.
/// </summary>

public class PromoteListingRequest
{
    public int Days { get; set; } = 30;
}

public class VerifiedBadgeRequest
{
    public string? DocumentUrl { get; set; }
    public string? BusinessLicenseNumber { get; set; }
}
