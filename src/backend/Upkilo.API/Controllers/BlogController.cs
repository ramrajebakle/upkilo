using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class BlogController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAIService _aiService;
    private readonly ILogger<BlogController> _logger;

    public BlogController(AppDbContext context, ITenantProvider tenantProvider, IAIService aiService, ILogger<BlogController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _aiService = aiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] string? status)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.Set<BlogPost>().Where(p => p.TenantId == tenantId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.Status == status);
        var posts = await query.OrderByDescending(p => p.PublishedAt)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Status,
                p.Excerpt,
                p.PublishedAt,
                p.ViewCount,
                p.Tags,
                p.FeaturedImageUrl,
                p.MetaTitle,
                p.MetaDescription,
                p.Author,
            }).ToListAsync();
        return Ok(posts);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPost(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var post = await _context.Set<BlogPost>()
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (post == null) return NotFound();
        return Ok(post);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] BlogPostRequest req)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var slugTaken = await _context.Set<BlogPost>()
            .AnyAsync(p => p.Slug == req.Slug && p.TenantId == tenantId);
        if (slugTaken) return Conflict(new { error = "Slug already in use" });

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.Empty,
            Title = req.Title,
            Slug = req.Slug.ToLower().Trim(),
            MetaTitle = req.MetaTitle,
            MetaDescription = req.MetaDescription,
            Content = req.Content ?? string.Empty,
            Excerpt = req.Excerpt,
            FeaturedImageUrl = req.FeaturedImageUrl,
            Tags = req.Tags,
            Status = req.Status ?? "Draft",
            Author = req.Author,
            PublishedAt = req.Status == "Published" ? DateTime.UtcNow : null,
        };
        _context.Set<BlogPost>().Add(post);
        await _context.SaveChangesAsync();
        return Ok(post);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] BlogPostRequest req)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var post = await _context.Set<BlogPost>()
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (post == null) return NotFound();

        post.Title = req.Title;
        post.Slug = req.Slug.ToLower().Trim();
        post.MetaTitle = req.MetaTitle;
        post.MetaDescription = req.MetaDescription;
        post.Content = req.Content ?? post.Content;
        post.Excerpt = req.Excerpt;
        post.FeaturedImageUrl = req.FeaturedImageUrl;
        post.Tags = req.Tags;
        post.Author = req.Author;

        if (req.Status != null && post.Status != "Published" && req.Status == "Published")
            post.PublishedAt = DateTime.UtcNow;
        if (req.Status != null) post.Status = req.Status;

        await _context.SaveChangesAsync();
        return Ok(post);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var post = await _context.Set<BlogPost>()
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (post == null) return NotFound();
        post.Status = "Archived";
        await _context.SaveChangesAsync();
        return Ok(new { message = "Post archived" });
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishPost(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var post = await _context.Set<BlogPost>()
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (post == null) return NotFound();
        post.Status = "Published";
        post.PublishedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(post);
    }

    /// <summary>
    /// Day 77: POST /api/v1/blog/ai-generate — generate a full blog post draft from topic + keywords.
    /// </summary>
    [HttpPost("ai-generate")]
    public async Task<IActionResult> AiGenerate([FromBody] AiBlogRequest req)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        var prompt = $"""
            Write a professional blog post for a {req.BusinessType ?? "service"} business.
            Topic: {req.Topic}
            Target keywords: {req.Keywords}
            Tone: {req.Tone ?? "professional, friendly"}
            Word count: approximately {req.WordCount ?? 600} words.

            Return a JSON object with these exact fields:
            - title: string (SEO-optimized headline)
            - metaDescription: string (155 chars max, includes primary keyword)
            - excerpt: string (2 sentences)
            - content: string (full blog post in Markdown)
            - tags: string (comma-separated, 3-5 relevant tags)

            Return ONLY valid JSON, no additional text.
            """;

        try
        {
            var result = await _aiService.GenerateTextAsync(tenantId, Guid.Empty, prompt);
            var raw = result.Content?.Trim() ?? "{}";
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start >= 0 && end > start) raw = raw[start..(end + 1)];
            var generated = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(raw);
            return Ok(ApiResponse<object>.Ok(generated));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Blog] AI generation failed for tenant {TenantId}", tenantId);
            return StatusCode(500, ApiResponse.Fail("AI generation failed. Please try again."));
        }
    }
}

public record BlogPostRequest(
    string Title, string Slug, string? MetaTitle, string? MetaDescription,
    string? Content, string? Excerpt, string? FeaturedImageUrl,
    string? Tags, string? Status, string? Author);

public record AiBlogRequest(string Topic, string Keywords, string? BusinessType, string? Tone, int? WordCount);
