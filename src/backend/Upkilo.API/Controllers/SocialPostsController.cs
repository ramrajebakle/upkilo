using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin,Owner,Marketing")]
public class SocialPostsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public SocialPostsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] string? platform, [FromQuery] string? status)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.SocialPosts.Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrEmpty(platform)) query = query.Where(p => p.Platform == platform);
        if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.Status == status);

        var posts = await query.OrderByDescending(p => p.ScheduledFor).ToListAsync();
        return Ok(posts);
    }

    [HttpPost]
    public async Task<IActionResult> SchedulePost([FromBody] SchedulePostRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var post = new SocialPost
        {
            TenantId = tenantId.Value,
            Platform = request.Platform, // Facebook, Instagram, Twitter/X
            ContentText = request.ContentText,
            MediaUrlsJson = string.Join(",", request.MediaUrls),
            ScheduledFor = request.ScheduledFor ?? DateTime.UtcNow,
            Status = request.ScheduledFor.HasValue && request.ScheduledFor > DateTime.UtcNow ? "Scheduled" : "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.SocialPosts.Add(post);
        await _context.SaveChangesAsync();
        
        // Example: if Status is Pending (publish now), you might directly call a service
        // Otherwise a background job like SocialPostDeliveryJob picks it up

        return CreatedAtAction(nameof(GetPosts), new { id = post.Id }, post);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelPost(Guid id)
    {
        var post = await _context.SocialPosts.FindAsync(id);
        if (post == null) return NotFound();

        if (post.Status == "Published")
            return BadRequest("Cannot cancel an already published post.");

        post.Status = "Cancelled";
        await _context.SaveChangesAsync();

        return Ok(post);
    }
}

public class SchedulePostRequest
{
    public string Platform { get; set; } = string.Empty;
    public string ContentText { get; set; } = string.Empty;
    public List<string> MediaUrls { get; set; } = new();
    public DateTime? ScheduledFor { get; set; }
}
