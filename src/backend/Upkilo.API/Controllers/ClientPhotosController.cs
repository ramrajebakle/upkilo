using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Client photos controller for managing client images
/// </summary>
[ApiController]
[Route("api/clients/{clientId}/photos")]
[Authorize]
public class ClientPhotosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFileService _fileService;
    private readonly ILogger<ClientPhotosController> _logger;

    public ClientPhotosController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        IFileService fileService,
        ILogger<ClientPhotosController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// Get all photos for a client
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPhotos(Guid clientId, [FromQuery] PhotoType? type = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Set<ClientPhoto>()
            .Where(p => p.ClientId == clientId && p.TenantId == tenantId);

        if (type.HasValue)
            query = query.Where(p => p.Type == type.Value);

        var photos = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Type,
                p.FileUrl,
                p.Caption,
                p.FileName,
                p.FileSizeBytes,
                p.MimeType,
                p.TakenAt,
                p.IsPublic,
                p.Width,
                p.Height,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = photos });
    }

    /// <summary>
    /// Get photo details
    /// </summary>
    [HttpGet("{photoId}")]
    public async Task<IActionResult> GetPhoto(Guid clientId, Guid photoId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var photo = await _context.Set<ClientPhoto>()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.ClientId == clientId && p.TenantId == tenantId);

        if (photo == null) return NotFound();

        return Ok(photo);
    }

    /// <summary>
    /// Upload photo
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UploadPhoto(
        Guid clientId,
        [FromForm] IFormFile file,
        [FromForm] PhotoType type = PhotoType.Other,
        [FromForm] string? caption = null,
        [FromForm] bool isPublic = false,
        [FromForm] Guid? serviceId = null,
        [FromForm] Guid? bookingId = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify client exists
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId);

        if (client == null) return NotFound("Client not found");

        // Validate file
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        // Validate image type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest("Only image files are allowed (JPEG, PNG, GIF, WebP)");

        // Upload to blob storage
        var uploadResult = await _fileService.UploadAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            tenantId.Value,
            FileCategory.ProfileImages
        );

        // Create photo record
        var photo = new ClientPhoto
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ClientId = clientId,
            Type = type,
            FileUrl = uploadResult.Url,
            Caption = caption,
            FileName = file.FileName,
            FileSizeBytes = file.Length,
            MimeType = file.ContentType,
            TakenAt = DateTime.UtcNow,
            ServiceId = serviceId,
            BookingId = bookingId,
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<ClientPhoto>().Add(photo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Uploaded photo {PhotoId} for client {ClientId}", photo.Id, clientId);

        return CreatedAtAction(nameof(GetPhoto), new { clientId, photoId = photo.Id }, photo);
    }

    /// <summary>
    /// Update photo metadata
    /// </summary>
    [HttpPut("{photoId}")]
    public async Task<IActionResult> UpdatePhoto(
        Guid clientId,
        Guid photoId,
        [FromBody] UpdatePhotoRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var photo = await _context.Set<ClientPhoto>()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.ClientId == clientId && p.TenantId == tenantId);

        if (photo == null) return NotFound();

        if (request.Caption != null) photo.Caption = request.Caption;
        if (request.Type.HasValue) photo.Type = request.Type.Value;
        if (request.IsPublic.HasValue) photo.IsPublic = request.IsPublic.Value;
        if (request.TakenAt.HasValue) photo.TakenAt = request.TakenAt;

        photo.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(photo);
    }

    /// <summary>
    /// Delete photo
    /// </summary>
    [HttpDelete("{photoId}")]
    public async Task<IActionResult> DeletePhoto(Guid clientId, Guid photoId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var photo = await _context.Set<ClientPhoto>()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.ClientId == clientId && p.TenantId == tenantId);

        if (photo == null) return NotFound();

        // Delete from blob storage
        try
        {
            await _fileService.DeleteAsync(photo.FileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file from blob storage: {FileUrl}", photo.FileUrl);
        }

        _context.Set<ClientPhoto>().Remove(photo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted photo {PhotoId} for client {ClientId}", photoId, clientId);

        return NoContent();
    }

    /// <summary>
    /// Get before/after pairs for a client
    /// </summary>
    [HttpGet("before-after")]
    public async Task<IActionResult> GetBeforeAfterPairs(Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var photos = await _context.Set<ClientPhoto>()
            .Where(p => p.ClientId == clientId && 
                        p.TenantId == tenantId &&
                        (p.Type == PhotoType.Before || p.Type == PhotoType.After))
            .OrderBy(p => p.TakenAt ?? p.CreatedAt)
            .ToListAsync();

        // Group by service/booking
        var pairs = photos
            .GroupBy(p => new { p.ServiceId, p.BookingId })
            .Select(g => new
            {
                ServiceId = g.Key.ServiceId,
                BookingId = g.Key.BookingId,
                Before = g.FirstOrDefault(p => p.Type == PhotoType.Before),
                After = g.FirstOrDefault(p => p.Type == PhotoType.After)
            })
            .Where(pair => pair.Before != null || pair.After != null)
            .ToList();

        return Ok(new { data = pairs });
    }

    /// <summary>
    /// Set profile photo
    /// </summary>
    [HttpPut("{photoId}/set-profile")]
    public async Task<IActionResult> SetAsProfile(Guid clientId, Guid photoId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var photo = await _context.Set<ClientPhoto>()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.ClientId == clientId && p.TenantId == tenantId);

        if (photo == null) return NotFound();

        // Update client's avatar
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
        if (client != null)
        {
            client.AvatarUrl = photo.FileUrl;
            await _context.SaveChangesAsync();
        }

        // Set photo type to Profile
        photo.Type = PhotoType.Profile;
        photo.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Profile photo updated", photoUrl = photo.FileUrl });
    }
}

// DTOs
public record UpdatePhotoRequest(
    string? Caption = null,
    PhotoType? Type = null,
    bool? IsPublic = null,
    DateTime? TakenAt = null
);
