using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Landing pages controller for page builder functionality.
/// Uses real database queries against LandingPages.
/// </summary>
[ApiController]
[Route("api/landing-pages")]
[Authorize]
public class LandingPagesController : ControllerBase
{
    private readonly ILogger<LandingPagesController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public LandingPagesController(
        ILogger<LandingPagesController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all landing pages
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLandingPages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? published = null,
        [FromQuery] string? search = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.LandingPages
            .Where(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        if (published.HasValue)
            query = query.Where(p => p.IsPublished == published.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Title.Contains(search) || p.Slug.Contains(search));

        var total = await query.CountAsync();

        var pages = await query
            .OrderByDescending(p => p.UpdatedAt != default ? p.UpdatedAt : p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Description,
                p.IsPublished,
                p.Views,
                p.Conversions,
                conversionRate = p.Views > 0 ? Math.Round((double)p.Conversions / p.Views * 100, 1) : 0,
                p.VariantGroup,
                p.VariantLabel,
                p.PublishedAt,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { data = pages, total, page, pageSize });
    }

    /// <summary>
    /// Get landing page by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLandingPage(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var page = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (page == null) return NotFound();

        return Ok(new
        {
            page.Id,
            page.Title,
            page.Slug,
            page.Description,
            page.HtmlContent,
            page.CssOverrides,
            page.IsPublished,
            page.Views,
            page.Conversions,
            conversionRate = page.Views > 0 ? Math.Round((double)page.Conversions / page.Views * 100, 1) : 0,
            page.CampaignId,
            page.VariantGroup,
            page.VariantLabel,
            page.PublishedAt,
            page.CreatedAt,
            page.UpdatedAt
        });
    }

    /// <summary>
    /// Create landing page
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateLandingPage([FromBody] CreateLandingPageRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Title is required." });

        var slug = GenerateSlug(request.Title);

        // Ensure slug is unique
        var existingSlug = await _context.LandingPages
            .AnyAsync(p => p.TenantId == tenantId.Value && p.Slug == slug && !p.IsDeleted);
        if (existingSlug)
            slug = $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var page = new LandingPage
        {
            TenantId = tenantId.Value,
            Title = request.Title,
            Slug = slug,
            Description = request.Description,
            HtmlContent = request.HtmlContent ?? "<div class='hero'><h1>Welcome</h1></div>",
            CssOverrides = request.CssOverrides,
            CampaignId = request.CampaignId,
            IsPublished = false
        };

        _context.LandingPages.Add(page);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Landing page created: {Id} - {Title}", page.Id, page.Title);

        return CreatedAtAction(nameof(GetLandingPage), new { id = page.Id }, new { page.Id, page.Title, page.Slug, page.CreatedAt });
    }

    /// <summary>
    /// Update landing page
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLandingPage(Guid id, [FromBody] UpdateLandingPageRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var page = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (page == null) return NotFound();

        if (request.Title != null) page.Title = request.Title;
        if (request.Description != null) page.Description = request.Description;
        if (request.HtmlContent != null) page.HtmlContent = request.HtmlContent;
        if (request.CssOverrides != null) page.CssOverrides = request.CssOverrides;
        page.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, page.UpdatedAt });
    }

    /// <summary>
    /// Delete landing page (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLandingPage(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var page = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (page == null) return NotFound();

        page.IsDeleted = true;
        page.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Publish landing page
    /// </summary>
    [HttpPost("{id}/publish")]
    public async Task<IActionResult> PublishLandingPage(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var page = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (page == null) return NotFound();

        page.IsPublished = true;
        page.PublishedAt = DateTime.UtcNow;
        page.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, page.PublishedAt, url = $"/p/{page.Slug}" });
    }

    /// <summary>
    /// Unpublish landing page
    /// </summary>
    [HttpPost("{id}/unpublish")]
    public async Task<IActionResult> UnpublishLandingPage(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var page = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (page == null) return NotFound();

        page.IsPublished = false;
        page.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    /// <summary>
    /// Duplicate landing page
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> DuplicateLandingPage(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var original = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (original == null) return NotFound();

        var copy = new LandingPage
        {
            TenantId = tenantId.Value,
            Title = $"{original.Title} (Copy)",
            Slug = $"{original.Slug}-copy-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Description = original.Description,
            HtmlContent = original.HtmlContent,
            CssOverrides = original.CssOverrides,
            CampaignId = original.CampaignId,
            IsPublished = false
        };

        _context.LandingPages.Add(copy);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLandingPage), new { id = copy.Id }, new { copy.Id, copy.Title, copy.Slug });
    }

    /// <summary>
    /// Get landing page analytics
    /// </summary>
    [HttpGet("{id}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var page = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (page == null) return NotFound();

        // Check for A/B test variants
        var variants = page.VariantGroup != null
            ? await _context.LandingPages
                .Where(p => p.VariantGroup == page.VariantGroup && p.TenantId == tenantId.Value && !p.IsDeleted)
                .Select(p => new { p.VariantLabel, p.Views, p.Conversions, rate = p.Views > 0 ? Math.Round((double)p.Conversions / p.Views * 100, 1) : 0 })
                .ToListAsync()
            : null;

        return Ok(new
        {
            page.Views,
            page.Conversions,
            conversionRate = page.Views > 0 ? Math.Round((double)page.Conversions / page.Views * 100, 1) : 0,
            abTest = variants
        });
    }

    /// <summary>
    /// Preview landing page (public)
    /// </summary>
    [HttpGet("preview/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> PreviewPage(string slug)
    {
        var page = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished && !p.IsDeleted);

        if (page == null) return NotFound();

        // Increment views
        page.Views++;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            page.Title,
            page.Description,
            page.HtmlContent,
            page.CssOverrides
        });
    }

    private static string GenerateSlug(string title)
    {
        return title.ToLower()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("'", "")
            .Replace("\"", "");
    }

    /// <summary>
    /// Get available section types for the page builder
    /// </summary>
    [HttpGet("section-types")]
    public IActionResult GetSectionTypes()
    {
        var sectionTypes = new List<object>
        {
            new { id = "hero", name = "Hero Section", description = "Full-width hero banner with headline, subtitle, and CTA button.", icon = "star" },
            new { id = "features", name = "Features Grid", description = "2-4 column grid showcasing key features with icons.", icon = "grid" },
            new { id = "cta", name = "Call to Action", description = "Prominent CTA block with headline and button.", icon = "cursor-click" },
            new { id = "testimonials", name = "Testimonials", description = "Client testimonial carousel or grid.", icon = "chat-bubble" },
            new { id = "pricing", name = "Pricing Table", description = "Pricing cards with plan comparison.", icon = "credit-card" },
            new { id = "faq", name = "FAQ Accordion", description = "Frequently asked questions with expandable answers.", icon = "question-mark" },
            new { id = "gallery", name = "Image Gallery", description = "Photo gallery with lightbox support.", icon = "photo" },
            new { id = "contact", name = "Contact Form", description = "Contact form with name, email, message fields.", icon = "envelope" },
            new { id = "team", name = "Team Members", description = "Staff/team showcase with photos and bios.", icon = "user-group" },
            new { id = "video", name = "Video Embed", description = "Embedded video section with overlay text.", icon = "play" },
            new { id = "stats", name = "Statistics Counter", description = "Animated number counters for key stats.", icon = "chart-bar" },
            new { id = "booking", name = "Booking Widget", description = "Embeddable booking form for direct scheduling.", icon = "calendar" }
        };

        return Ok(new { data = sectionTypes });
    }

    /// <summary>
    /// Get SEO settings for a page
    /// </summary>
    [HttpGet("{id}/seo")]
    public async Task<IActionResult> GetSeoSettings(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var page = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);
        if (page == null) return NotFound();

        return Ok(new
        {
            title = page.Title,
            description = page.Description,
            slug = page.Slug,
            canonicalUrl = $"/p/{page.Slug}",
            ogTitle = page.Title,
            ogDescription = page.Description
        });
    }

    /// <summary>
    /// Update SEO settings for a page
    /// </summary>
    [HttpPut("{id}/seo")]
    public async Task<IActionResult> UpdateSeoSettings(Guid id, [FromBody] UpdateSeoRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var page = await _context.LandingPages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);
        if (page == null) return NotFound();

        if (request.Title != null) page.Title = request.Title;
        if (request.Description != null) page.Description = request.Description;
        if (request.Slug != null) page.Slug = request.Slug;
        page.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

// Request DTOs
public class CreateLandingPageRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HtmlContent { get; set; }
    public string? CssOverrides { get; set; }
    public Guid? CampaignId { get; set; }
}

public class UpdateLandingPageRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? HtmlContent { get; set; }
    public string? CssOverrides { get; set; }
    public string? Slug { get; set; }
}

public record UpdateSeoRequest(
    string? Title,
    string? Description,
    string? Slug
);
