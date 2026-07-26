using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Waivers controller for managing liability forms and signatures
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class WaiversController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<WaiversController> _logger;

    public WaiversController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<WaiversController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all waiver templates
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWaivers()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var waivers = await _context.DigitalWaivers
            .Where(w => w.TenantId == tenantId && w.IsActive)
            .OrderBy(w => w.Title)
            .Select(w => new
            {
                w.Id,
                Name = w.Title,
                w.IsRequired,
                w.IsActive,
                w.Version,
                w.CreatedAt,
                SignedCount = w.Signatures.Count
            })
            .ToListAsync();

        return Ok(new { data = waivers });
    }

    /// <summary>
    /// Get waiver by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWaiver(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var waiver = await _context.DigitalWaivers
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);

        if (waiver == null) return NotFound();

        return Ok(waiver);
    }

    /// <summary>
    /// Create a new waiver template
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateWaiver([FromBody] CreateWaiverRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var waiver = new DigitalWaiver
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Title = request.Name,
            Content = request.Content,
            IsRequired = request.IsRequired,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DigitalWaivers.Add(waiver);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Waiver created: {WaiverId} - {Name}", waiver.Id, waiver.Title);

        return CreatedAtAction(nameof(GetWaiver), new { id = waiver.Id }, waiver);
    }

    /// <summary>
    /// Update a waiver template (increments version)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWaiver(Guid id, [FromBody] UpdateWaiverRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var waiver = await _context.DigitalWaivers
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);

        if (waiver == null) return NotFound();

        if (request.Name != null) waiver.Title = request.Name;
        if (request.Content != null)
        {
            waiver.Content = request.Content;
            waiver.Version++; // Increment version when content changes
        }
        if (request.IsRequired.HasValue) waiver.IsRequired = request.IsRequired.Value;
        if (request.IsActive.HasValue) waiver.IsActive = request.IsActive.Value;

        waiver.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Waiver updated: {WaiverId} (version {Version})", id, waiver.Version);

        return Ok(waiver);
    }

    /// <summary>
    /// Delete a waiver template
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWaiver(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var waiver = await _context.DigitalWaivers
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);

        if (waiver == null) return NotFound();

        // Soft delete by marking inactive
        waiver.IsActive = false;
        waiver.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get required waivers for a client (that they haven't signed or need to re-sign)
    /// </summary>
    [HttpGet("pending/{clientId}")]
    public async Task<IActionResult> GetPendingWaivers(Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Get all required waivers
        var requiredWaivers = await _context.DigitalWaivers
            .Where(w => w.TenantId == tenantId && w.IsActive && w.IsRequired)
            .ToListAsync();

        // Get signed waivers for this client
        var signedWaivers = await _context.WaiverSignatures
            .Where(cw => cw.ClientId == clientId && cw.Client!.TenantId == tenantId)
            .ToListAsync();

        // Find pending (not signed OR signed an older version)
        var pending = requiredWaivers.Where(w =>
        {
            var signed = signedWaivers.FirstOrDefault(s => s.WaiverId == w.Id);
            return signed == null || signed.WaiverVersion < w.Version;
        }).Select(w => new { w.Id, Name = w.Title, w.Content, w.Version }).ToList();

        return Ok(new { data = pending, hasPending = pending.Any() });
    }

    /// <summary>
    /// Sign a waiver
    /// </summary>
    [HttpPost("{waiverId}/sign")]
    public async Task<IActionResult> SignWaiver(Guid waiverId, [FromBody] SignWaiverRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var waiver = await _context.DigitalWaivers
            .FirstOrDefaultAsync(w => w.Id == waiverId && w.TenantId == tenantId && w.IsActive);

        if (waiver == null) return NotFound("Waiver not found");

        if (string.IsNullOrWhiteSpace(request.Signature))
            return BadRequest(new { error = "Signature is required." });

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == request.ClientId && c.TenantId == tenantId);

        if (client == null) return NotFound("Client not found");

        // Check if already signed this version
        var existingSig = await _context.WaiverSignatures
            .FirstOrDefaultAsync(cw => cw.WaiverId == waiverId && 
                                       cw.ClientId == request.ClientId &&
                                       cw.WaiverVersion == waiver.Version);

        if (existingSig != null)
            return BadRequest("This waiver version has already been signed");

        var clientWaiver = new WaiverSignature
        {
            Id = Guid.NewGuid(),
            WaiverId = waiverId,
            ClientId = request.ClientId,
            WaiverVersion = waiver.Version,
            SignatureData = request.Signature,
            SignedAt = DateTime.UtcNow,
            SignedFromIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WaiverSignatures.Add(clientWaiver);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Waiver {WaiverId} signed by client {ClientId}", waiverId, request.ClientId);

        return Ok(new
        {
            success = true,
            signedAt = clientWaiver.SignedAt,
            version = waiver.Version
        });
    }

    /// <summary>
    /// Get client's signed waivers
    /// </summary>
    [HttpGet("client/{clientId}")]
    public async Task<IActionResult> GetClientWaivers(Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var signed = await _context.WaiverSignatures
            .Include(cw => cw.Waiver)
            .Where(cw => cw.ClientId == clientId && cw.Client!.TenantId == tenantId)
            .OrderByDescending(cw => cw.SignedAt)
            .Select(cw => new
            {
                cw.Id,
                WaiverName = cw.Waiver != null ? cw.Waiver.Title : string.Empty,
                cw.WaiverVersion,
                cw.SignedAt,
                IpAddress = cw.SignedFromIP,
                IsCurrentVersion = cw.Waiver != null && cw.WaiverVersion == cw.Waiver.Version
            })
            .ToListAsync();

        return Ok(new { data = signed });
    }

    /// <summary>
    /// Generate a PDF version of the signed waiver
    /// </summary>
    [HttpGet("{waiverId}/client/{clientId}/pdf")]
    public async Task<IActionResult> GenerateWaiverPdf(Guid waiverId, Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var signedResponse = await _context.WaiverSignatures
            .Include(cw => cw.Waiver)
            .Include(cw => cw.Client)
            .FirstOrDefaultAsync(cw => cw.WaiverId == waiverId && cw.ClientId == clientId && cw.Client!.TenantId == tenantId);

        if (signedResponse == null) return NotFound(new { error = "Signed waiver not found." });

        var htmlContent = $@"
            <html>
                <head>
                    <title>Signed Waiver - {signedResponse.Waiver!.Title}</title>
                    <style>
                        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; padding: 50px; border: 1px solid #eee; }}
                        .header {{ text-align: center; margin-bottom: 40px; border-bottom: 2px solid #333; padding-bottom: 20px; }}
                        .content {{ margin-bottom: 40px; white-space: pre-wrap; font-size: 14px; color: #555; }}
                        .signature-box {{ border: 1px solid #ccc; padding: 20px; background: #f9f9f9; }}
                        .footer {{ margin-top: 50px; font-size: 12px; color: #888; text-align: center; border-top: 1px solid #eee; padding-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class=""header"">
                        <h1>{signedResponse.Waiver.Title}</h1>
                        <p>Document ID: {signedResponse.Id}</p>
                    </div>
                    <div class=""content"">
                        {signedResponse.Waiver.Content}
                    </div>
                    <div class=""signature-box"">
                        <p><strong>Signed By:</strong> {signedResponse.Client!.FirstName} {signedResponse.Client.LastName}</p>
                        <p><strong>Date Signed:</strong> {signedResponse.SignedAt:MMMM dd, yyyy HH:mm:ss UTC}</p>
                        <p><strong>IP Address:</strong> {signedResponse.SignedFromIP}</p>
                        <p><strong>Digital Signature:</strong></p>
                        <div style=""font-family: 'Brush Script MT', cursive; font-size: 24px; margin-top: 10px;"">
                            {signedResponse.SignatureData}
                        </div>
                    </div>
                    <div class=""footer"">
                        <p>This is a legally binding electronic document generated by Upkilo SaaS.</p>
                        <p>&copy; {DateTime.UtcNow.Year} Upkilo</p>
                    </div>
                </body>
            </html>";

        // Returning as HTML for now. A real PDF library like DinkToPdf would export bytes.
        var bytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);
        return File(bytes, "text/html", $"waiver_{waiverId}.html");
    }

    /// <summary>
    /// Revoke a previously signed waiver (forces re-sign)
    /// </summary>
    [HttpPost("{waiverId}/client/{clientId}/revoke")]
    public async Task<IActionResult> RevokeWaiver(Guid waiverId, Guid clientId, [FromBody] RevokeWaiverRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var signedResponse = await _context.WaiverSignatures
            .FirstOrDefaultAsync(cw => cw.WaiverId == waiverId && cw.ClientId == clientId && cw.Client!.TenantId == tenantId);

        if (signedResponse == null) return NotFound(new { error = "Signed waiver not found." });

        _context.WaiverSignatures.Remove(signedResponse);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Waiver {WaiverId} revoked for client {ClientId}. Reason: {Reason}", waiverId, clientId, request.Reason);

        return Ok(new { success = true, message = "Waiver signature revoked successfully." });
    }
}

// DTOs
public record CreateWaiverRequest(string Name, string Content, bool IsRequired = true);
public record UpdateWaiverRequest(string? Name = null, string? Content = null, bool? IsRequired = null, bool? IsActive = null);
public record SignWaiverRequest(Guid ClientId, string Signature);
public record RevokeWaiverRequest(string? Reason = null);
