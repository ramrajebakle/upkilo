using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class GiftCertificatesController : ControllerBase
{
    private readonly IGiftCertificateService _giftCertificateService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GiftCertificatesController> _logger;

    public GiftCertificatesController(
        IGiftCertificateService giftCertificateService,
        ITenantProvider tenantProvider,
        ILogger<GiftCertificatesController> logger)
    {
        _giftCertificateService = giftCertificateService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() ?? Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> GetGiftCertificates()
    {
        var tenantId = GetTenantId();
        var certificates = await _giftCertificateService.GetTenantGiftCertificatesAsync(tenantId);
        
        var result = certificates.Select(c => new
        {
            c.Id,
            c.Code,
            c.InitialAmount,
            c.RemainingAmount,
            c.Currency,
            c.ExpiryDate,
            status = c.Status.ToString(),
            c.RecipientEmail,
            c.SenderName,
            c.CreatedAt
        });

        return Ok(new { data = result });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGiftCertificate(Guid id)
    {
        var tenantId = GetTenantId();
        var cert = await _giftCertificateService.GetByIdAsync(id, tenantId);

        if (cert == null) return NotFound();

        return Ok(new
        {
            cert.Id,
            cert.Code,
            cert.InitialAmount,
            cert.RemainingAmount,
            cert.Currency,
            cert.ExpiryDate,
            status = cert.Status.ToString(),
            cert.RecipientEmail,
            cert.SenderName,
            cert.Message,
            cert.CreatedAt,
            redemptions = cert.Redemptions.Select(r => new
            {
                r.Id,
                r.AmountRedeemed,
                r.RedeemedAt,
                r.BookingId,
                r.Notes
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> IssueGiftCertificate([FromBody] IssueGiftCertificateRequest request)
    {
        var tenantId = GetTenantId();
        
        var cert = await _giftCertificateService.IssueGiftCertificateAsync(
            tenantId,
            request.Amount,
            request.RecipientEmail,
            request.SenderName,
            request.Message,
            request.ExpiryDate,
            request.ClientId);

        return CreatedAtAction(nameof(GetGiftCertificate), new { id = cert.Id }, cert);
    }

    [HttpGet("validate/{code}")]
    [AllowAnonymous] // Allow public booking page to validate
    public async Task<IActionResult> ValidateCode(string code, [FromQuery] Guid tenantId)
    {
        // If the user is authorized, we can get tenantId from provider, 
        // but for public booking widget, we need it as a query param or header.
        var tid = tenantId == Guid.Empty ? GetTenantId() : tenantId;
        
        if (tid == Guid.Empty) return BadRequest("TenantId is required.");

        var cert = await _giftCertificateService.ValidateCodeAsync(tid, code);

        if (cert == null) return NotFound(new { message = "Gift certificate not found." });

        if (cert.Status == GiftCertificateStatus.Expired)
            return BadRequest(new { message = "Gift certificate has expired." });
            
        if (cert.Status == GiftCertificateStatus.FullyRedeemed)
            return BadRequest(new { message = "Gift certificate has no remaining balance." });
            
        if (cert.Status == GiftCertificateStatus.Void)
            return BadRequest(new { message = "Gift certificate is void." });

        return Ok(new
        {
            cert.Code,
            cert.RemainingAmount,
            cert.Currency,
            Status = cert.Status.ToString()
        });
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> RedeemCode([FromBody] RedeemGiftCertificateRequest request)
    {
        var tenantId = GetTenantId();
        
        var success = await _giftCertificateService.RedeemAmountAsync(
            tenantId,
            request.Code,
            request.Amount,
            request.BookingId,
            request.Notes);

        if (!success)
            return BadRequest(new { message = "Failed to redeem gift certificate. Check balance and status." });

        return Ok(new { success = true });
    }
}

public class IssueGiftCertificateRequest
{
    public decimal Amount { get; set; }
    public string? RecipientEmail { get; set; }
    public string? SenderName { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? ClientId { get; set; }
}

public class RedeemGiftCertificateRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? BookingId { get; set; }
    public string? Notes { get; set; }
}

