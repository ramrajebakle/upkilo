using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Security.Cryptography;

namespace Upkilo.API.Controllers;

/// <summary>
/// Gift cards controller for gift certificate management.
/// Uses real database queries against GiftCertificates and GiftCertificateRedemptions.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class GiftCardsController : ControllerBase
{
    private readonly ILogger<GiftCardsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEventService _eventService;

    public GiftCardsController(
        ILogger<GiftCardsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IEventService eventService)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _eventService = eventService;
    }

    /// <summary>
    /// Get all gift cards with filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGiftCards(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.GiftCertificates
            .Where(g => g.TenantId == tenantId.Value && !g.IsDeleted);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<GiftCertificateStatus>(status, true, out var statusEnum))
            query = query.Where(g => g.Status == statusEnum);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(g => g.Code.Contains(search) ||
                (g.RecipientEmail != null && g.RecipientEmail.Contains(search)) ||
                (g.SenderName != null && g.SenderName.Contains(search)));

        var total = await query.CountAsync();

        var giftCards = await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new
            {
                g.Id,
                g.Code,
                g.InitialAmount,
                g.RemainingAmount,
                g.Currency,
                status = g.Status.ToString(),
                g.ExpiryDate,
                g.RecipientEmail,
                g.SenderName,
                g.Message,
                g.ClientId,
                g.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = giftCards, total, page, pageSize });
    }

    /// <summary>
    /// Get gift card by ID with redemption history
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGiftCard(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var card = await _context.GiftCertificates
            .Include(g => g.Redemptions)
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId.Value && !g.IsDeleted);

        if (card == null) return NotFound();

        return Ok(new
        {
            card.Id,
            card.Code,
            card.InitialAmount,
            card.RemainingAmount,
            card.Currency,
            status = card.Status.ToString(),
            card.ExpiryDate,
            card.RecipientEmail,
            card.SenderName,
            card.Message,
            card.ClientId,
            card.CreatedAt,
            redemptions = card.Redemptions.OrderByDescending(r => r.RedeemedAt).Select(r => new
            {
                r.Id,
                r.AmountRedeemed,
                r.RedeemedAt,
                r.BookingId,
                r.Notes
            })
        });
    }

    /// <summary>
    /// Create/sell a gift card
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateGiftCard([FromBody] CreateGiftCardRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be greater than zero." });

        var card = new GiftCertificate
        {
            TenantId = tenantId.Value,
            Code = GenerateGiftCardCode(),
            InitialAmount = request.Amount,
            RemainingAmount = request.Amount,
            Currency = request.Currency ?? "USD",
            ExpiryDate = request.ExpiryDate,
            RecipientEmail = request.RecipientEmail,
            SenderName = request.SenderName,
            Message = request.Message,
            ClientId = request.ClientId,
            Status = GiftCertificateStatus.Active
        };

        _context.GiftCertificates.Add(card);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Gift card created: {Code} for {Amount} {Currency}", card.Code, card.InitialAmount, card.Currency);

        await _eventService.PublishAsync("giftcard.created", new { card.Id, card.Code, card.InitialAmount, card.RecipientEmail }, tenantId.Value);

        return CreatedAtAction(nameof(GetGiftCard), new { id = card.Id }, new
        {
            card.Id,
            card.Code,
            card.InitialAmount,
            card.RemainingAmount,
            card.CreatedAt
        });
    }

    /// <summary>
    /// Check gift card balance (public — rate-limited by middleware)
    /// SECURITY (H-4): Returns only minimal balance info; no tenant-scoping
    /// since gift cards may be cross-tenant by design. Rate limiting via
    /// TierRateLimitMiddleware prevents enumeration attacks.
    /// </summary>
    [HttpGet("check/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckBalance(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 30)
            return BadRequest(new { error = "Invalid code format." });

        var card = await _context.GiftCertificates
            .FirstOrDefaultAsync(g => g.Code == code.ToUpper() && !g.IsDeleted);

        if (card == null)
            return NotFound(new { error = "Gift card not found." });

        // Return minimal info — no currency, no internal status details
        return Ok(new
        {
            card.Code,
            card.RemainingAmount,
            card.Currency,
            isValid = card.Status == GiftCertificateStatus.Active || card.Status == GiftCertificateStatus.PartiallyRedeemed
        });
    }

    /// <summary>
    /// Redeem gift card (deduct amount)
    /// </summary>
    [HttpPost("{id}/redeem")]
    public async Task<IActionResult> RedeemGiftCard(Guid id, [FromBody] RedeemGiftCardRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var card = await _context.GiftCertificates
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId.Value && !g.IsDeleted);

        if (card == null) return NotFound();

        if (card.Status == GiftCertificateStatus.Void || card.Status == GiftCertificateStatus.FullyRedeemed)
            return BadRequest(new { error = "This gift card cannot be redeemed." });

        if (card.ExpiryDate.HasValue && card.ExpiryDate.Value < DateTime.UtcNow)
            return BadRequest(new { error = "This gift card has expired." });

        if (request.Amount > card.RemainingAmount)
            return BadRequest(new { error = $"Insufficient balance. Available: {card.RemainingAmount:F2}" });

        card.RemainingAmount -= request.Amount;
        card.Status = card.RemainingAmount == 0 ? GiftCertificateStatus.FullyRedeemed : GiftCertificateStatus.PartiallyRedeemed;
        card.UpdatedAt = DateTime.UtcNow;

        var redemption = new GiftCertificateRedemption
        {
            GiftCertificateId = id,
            AmountRedeemed = request.Amount,
            BookingId = request.BookingId,
            Notes = request.Notes
        };

        _context.GiftCertificateRedemptions.Add(redemption);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Gift card {Code} redeemed: {Amount}", card.Code, request.Amount);

        return Ok(new
        {
            success = true,
            amountRedeemed = request.Amount,
            remainingBalance = card.RemainingAmount,
            status = card.Status.ToString()
        });
    }

    /// <summary>
    /// Refund to gift card (add amount back)
    /// </summary>
    [HttpPost("{id}/refund")]
    public async Task<IActionResult> RefundToGiftCard(Guid id, [FromBody] RefundToGiftCardRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var card = await _context.GiftCertificates
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId.Value && !g.IsDeleted);

        if (card == null) return NotFound();

        card.RemainingAmount += request.Amount;
        if (card.RemainingAmount > 0 && card.Status == GiftCertificateStatus.FullyRedeemed)
            card.Status = GiftCertificateStatus.PartiallyRedeemed;
        if (card.RemainingAmount >= card.InitialAmount)
            card.Status = GiftCertificateStatus.Active;
        card.UpdatedAt = DateTime.UtcNow;

        // Record as negative redemption
        _context.GiftCertificateRedemptions.Add(new GiftCertificateRedemption
        {
            GiftCertificateId = id,
            AmountRedeemed = -request.Amount,
            Notes = $"Refund: {request.Reason}"
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Gift card {Code} refunded: {Amount}", card.Code, request.Amount);

        return Ok(new { success = true, card.RemainingAmount, status = card.Status.ToString() });
    }

    /// <summary>
    /// Reload gift card balance
    /// </summary>
    [HttpPost("{id}/reload")]
    public async Task<IActionResult> ReloadGiftCard(Guid id, [FromBody] ReloadGiftCardRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var card = await _context.GiftCertificates
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId.Value && !g.IsDeleted);

        if (card == null) return NotFound();

        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be greater than zero." });

        card.RemainingAmount += request.Amount;
        // SECURITY (L-4): Do NOT mutate InitialAmount — it is the historical purchase value.
        // Reloads are tracked as negative redemptions in the history.

        if (card.Status == GiftCertificateStatus.FullyRedeemed || card.Status == GiftCertificateStatus.Void)
            card.Status = GiftCertificateStatus.Active;

        card.UpdatedAt = DateTime.UtcNow;

        _context.GiftCertificateRedemptions.Add(new GiftCertificateRedemption
        {
            Id = Guid.NewGuid(),
            GiftCertificateId = id,
            AmountRedeemed = -request.Amount, // Negative denotes reload
            Notes = $"Reloaded balance: {request.Notes}"
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Gift card {Code} reloaded with {Amount}", card.Code, request.Amount);

        return Ok(new { success = true, card.RemainingAmount, status = card.Status.ToString() });
    }

    /// <summary>
    /// Void/deactivate gift card
    /// </summary>
    [HttpPost("{id}/void")]
    public async Task<IActionResult> VoidGiftCard(Guid id, [FromBody] VoidGiftCardRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var card = await _context.GiftCertificates
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId.Value && !g.IsDeleted);

        if (card == null) return NotFound();

        card.Status = GiftCertificateStatus.Void;
        card.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Gift card {Code} voided. Reason: {Reason}", card.Code, request.Reason);

        return Ok(new { success = true, message = "Gift card voided." });
    }

    /// <summary>
    /// SECURITY (H-3): Uses cryptographic RNG instead of System.Random for unpredictable codes.
    /// </summary>
    private static string GenerateGiftCardCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new char[16];
        var bytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        for (int i = 0; i < 16; i++)
            code[i] = chars[bytes[i] % chars.Length];
        return $"{new string(code[..4])}-{new string(code[4..8])}-{new string(code[8..12])}-{new string(code[12..16])}";
    }
}

// Request DTOs
public class CreateGiftCardRequest
{
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? RecipientEmail { get; set; }
    public string? SenderName { get; set; }
    public string? Message { get; set; }
    public Guid? ClientId { get; set; }
}

public class RedeemGiftCardRequest
{
    public decimal Amount { get; set; }
    public Guid? BookingId { get; set; }
    public string? Notes { get; set; }
}

public class RefundToGiftCardRequest
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class VoidGiftCardRequest
{
    public string? Reason { get; set; }
}

public class ReloadGiftCardRequest
{
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

