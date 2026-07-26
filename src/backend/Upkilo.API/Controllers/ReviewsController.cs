using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(AppDbContext context, ITenantProvider tenantProvider, ILogger<ReviewsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews([FromQuery] string? platform, [FromQuery] int? rating)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.ExternalReviews.Where(r => r.TenantId == tenantId);
        if (!string.IsNullOrEmpty(platform)) query = query.Where(r => r.Platform == platform);
        if (rating.HasValue) query = query.Where(r => r.Rating == rating.Value);
        var reviews = await query.OrderByDescending(r => r.ReviewDate)
            .Select(r => new {
                r.Id, r.Platform, r.ReviewerName, r.Rating,
                r.ReviewText, r.ResponseText, r.RespondedAt,
                r.Sentiment, r.ReviewDate, r.IsVerified, r.ExternalReviewId,
                hasResponse = r.ResponseText != null,
            }).ToListAsync();
        return Ok(reviews);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var reviews = await _context.ExternalReviews.Where(r => r.TenantId == tenantId).ToListAsync();
        if (!reviews.Any()) return Ok(new { averageRating = 0.0, totalCount = 0, responseRate = 0.0, recentCount = 0 });
        var total = reviews.Count;
        var responded = reviews.Count(r => r.ResponseText != null);
        return Ok(new {
            averageRating    = Math.Round(reviews.Average(r => r.Rating), 1),
            totalCount       = total,
            responseRate     = total > 0 ? Math.Round((double)responded / total * 100, 0) : 0.0,
            recentCount      = reviews.Count(r => r.ReviewDate >= DateTime.UtcNow.AddDays(-30)),
            countByPlatform  = reviews.GroupBy(r => r.Platform).ToDictionary(g => g.Key, g => g.Count()),
            countBySentiment = reviews.GroupBy(r => r.Sentiment).ToDictionary(g => g.Key, g => g.Count()),
            ratingBreakdown  = Enumerable.Range(1, 5).ToDictionary(i => i.ToString(), i => reviews.Count(r => r.Rating == i)),
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddReview([FromBody] AddReviewRequest req)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var sentiment = req.Rating >= 4 ? "Positive" : req.Rating <= 2 ? "Negative" : "Neutral";
        var review = new ExternalReview {
            Id = Guid.NewGuid(), TenantId = tenantId ?? Guid.Empty,
            Platform = req.Platform, ReviewerName = req.ReviewerName,
            Rating = req.Rating, ReviewText = req.ReviewText,
            ExternalReviewId = req.ExternalReviewId, Sentiment = sentiment,
            ReviewDate = req.ReviewDate ?? DateTime.UtcNow, IsVerified = false,
        };
        _context.ExternalReviews.Add(review);
        await _context.SaveChangesAsync();
        return Ok(review);
    }

    [HttpPatch("{id:guid}/respond")]
    public async Task<IActionResult> RespondToReview(Guid id, [FromBody] RespondRequest req)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var review = await _context.ExternalReviews.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);
        if (review == null) return NotFound();
        review.ResponseText = req.ResponseText;
        review.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(review);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] string? status)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.ReviewRequests.Include(r => r.Client).Where(r => r.TenantId == tenantId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
        var requests = await query.OrderByDescending(r => r.SentAt)
            .Select(r => new {
                r.Id, r.Status, r.Channel, r.ReviewUrl, r.SentAt, r.CompletedAt, r.BookingId,
                clientName  = r.Client != null ? r.Client.FirstName + " " + r.Client.LastName : "Unknown",
                clientEmail = r.Client != null ? r.Client.Email : null,
            }).ToListAsync();
        return Ok(requests);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateReviewRequestDto req)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == req.ClientId && c.TenantId == tenantId);
        if (!clientExists) return NotFound(new { error = "Client not found" });
        var request = new ReviewRequest {
            Id = Guid.NewGuid(), TenantId = tenantId ?? Guid.Empty,
            ClientId = req.ClientId, BookingId = req.BookingId,
            Channel = req.Channel ?? "Email", ReviewUrl = req.ReviewUrl,
            Status = "Sent", SentAt = DateTime.UtcNow,
        };
        _context.ReviewRequests.Add(request);
        await _context.SaveChangesAsync();
        return Ok(request);
    }

    [HttpPatch("requests/{id:guid}/complete")]
    public async Task<IActionResult> CompleteRequest(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var req = await _context.ReviewRequests.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);
        if (req == null) return NotFound();
        req.Status = "Completed";
        req.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(req);
    }
}

public record AddReviewRequest(string Platform, string ReviewerName, int Rating, string? ReviewText, string? ExternalReviewId, DateTime? ReviewDate);
public record RespondRequest(string ResponseText);
public record CreateReviewRequestDto(Guid ClientId, Guid? BookingId, string? Channel, string? ReviewUrl);
