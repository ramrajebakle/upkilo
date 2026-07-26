using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Referrals controller for referral program management.
/// Uses real database queries against ReferralRecords.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ReferralsController : ControllerBase
{
    private readonly ILogger<ReferralsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ReferralsController(
        ILogger<ReferralsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _emailService = emailService;
        _configuration = configuration;
    }

    /// <summary>
    /// Get all referrals (admin view)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllReferrals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.ReferralRecords
            .Where(r => r.TenantId == tenantId.Value && !r.IsDeleted);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync();

        var referrals = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.ReferrerId,
                r.ReferredEmail,
                r.ReferralCode,
                r.Status,
                r.ReferrerCredit,
                r.ReferredCredit,
                r.QualifiedAt,
                r.RewardedAt,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = referrals, total, page, pageSize });
    }

    /// <summary>
    /// Validate a referral code (public)
    /// </summary>
    [HttpGet("validate/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCode(string code)
    {
        var referral = await _context.ReferralRecords
            .FirstOrDefaultAsync(r => r.ReferralCode == code && r.Status == "Pending" && !r.IsDeleted);

        if (referral == null)
            return NotFound(new { error = "Invalid or expired referral code." });

        return Ok(new
        {
            valid = true,
            referralId = referral.Id,
            referredCredit = referral.ReferredCredit,
            code = referral.ReferralCode
        });
    }

    /// <summary>
    /// Generate a new referral code
    /// </summary>
    [HttpPost("generate-code")]
    public async Task<IActionResult> GenerateCode()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var code = $"REF{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var record = new ReferralRecord
        {
            TenantId = tenantId.Value,
            ReferrerId = tenantId.Value,
            ReferralCode = code,
            ReferredEmail = string.Empty,
            Status = "Pending",
            ReferrerCredit = 20m,  // $20 credit issued to referrer on conversion
            ReferredCredit = 20m   // $20 credit issued to referee on signup
        };

        _context.ReferralRecords.Add(record);
        await _context.SaveChangesAsync();

        var appUrl = (_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
        return Ok(new
        {
            id = record.Id,
            code,
            link = $"{appUrl}/ref/{code}",
            record.CreatedAt
        });
    }

    /// <summary>
    /// Apply a referral code
    /// </summary>
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyReferral([FromBody] ApplyReferralRequest request)
    {
        var referral = await _context.ReferralRecords
            .FirstOrDefaultAsync(r => r.ReferralCode == request.Code && r.Status == "Pending" && !r.IsDeleted);

        if (referral == null)
            return BadRequest(new { error = "Invalid or already-used referral code." });

        referral.ReferredEmail = request.RefereeEmail;
        referral.Status = "SignedUp";
        referral.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Referral code applied: {Code} by {Email}", request.Code, request.RefereeEmail);

        return Ok(new
        {
            success = true,
            referralId = referral.Id,
            referredCredit = referral.ReferredCredit,
            message = $"Referral applied! You'll receive ${referral.ReferredCredit:F2} credit."
        });
    }

    /// <summary>
    /// Complete a referral (after qualifying action)
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteReferral(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var referral = await _context.ReferralRecords
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value && !r.IsDeleted);

        if (referral == null) return NotFound();

        if (referral.Status == "Rewarded")
            return BadRequest(new { error = "Referral already completed." });

        referral.Status = "Rewarded";
        referral.QualifiedAt ??= DateTime.UtcNow;
        referral.RewardedAt = DateTime.UtcNow;
        referral.UpdatedAt = DateTime.UtcNow;

        // Deposit $20 credit into the referrer's CreditAccount
        if (referral.ReferrerCredit > 0)
        {
            var creditAccount = await _context.CreditAccounts
                .FirstOrDefaultAsync(a => a.TenantId == referral.ReferrerId);

            if (creditAccount == null)
            {
                creditAccount = new CreditAccount
                {
                    Id = Guid.NewGuid(),
                    TenantId = referral.ReferrerId,
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CreditAccounts.Add(creditAccount);
            }

            creditAccount.Balance += referral.ReferrerCredit;
            creditAccount.UpdatedAt = DateTime.UtcNow;

            _context.CreditAccountTransactions.Add(new CreditAccountTransaction
            {
                Id = Guid.NewGuid(),
                CreditAccountId = creditAccount.Id,
                Amount = referral.ReferrerCredit,
                Type = "ReferralReward",
                Description = $"Referral reward for converting {referral.ReferredEmail}",
                ReferenceId = referral.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Referral completed: {ReferralId}, referrer credited ${Credit}", id, referral.ReferrerCredit);

        return Ok(new
        {
            success = true,
            referrerCredited = referral.ReferrerCredit,
            message = "Referral completed! Credit added to referrer's account."
        });
    }

    /// <summary>
    /// Get referral analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var records = await _context.ReferralRecords
            .Where(r => r.TenantId == tenantId.Value && !r.IsDeleted)
            .ToListAsync();

        var total = records.Count;
        var rewarded = records.Count(r => r.Status == "Rewarded");
        var pending = records.Count(r => r.Status == "Pending");
        var signedUp = records.Count(r => r.Status == "SignedUp");

        return Ok(new
        {
            totalReferrals = total,
            rewarded,
            pending,
            signedUp,
            conversionRate = total > 0 ? Math.Round((double)rewarded / total * 100, 1) : 0,
            totalCreditsIssued = records.Where(r => r.Status == "Rewarded").Sum(r => r.ReferrerCredit),
            totalDiscountsGiven = records.Where(r => r.Status == "Rewarded").Sum(r => r.ReferredCredit)
        });
    }

    /// <summary>
    /// Send referral invite via email
    /// </summary>
    [HttpPost("send-invite")]
    public async Task<IActionResult> SendInvite([FromBody] SendReferralInviteRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var code = $"REF{Guid.NewGuid().ToString()[..6].ToUpper()}";
        
        var record = new ReferralRecord
        {
            TenantId = tenantId.Value,
            ReferrerId = tenantId.Value,
            ReferralCode = code,
            ReferredEmail = request.Email,
            Status = "Pending",
            ReferrerCredit = 20m,
            ReferredCredit = 20m,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferralRecords.Add(record);
        await _context.SaveChangesAsync();

        var appUrl = (_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
        var link = $"{appUrl}/ref/{code}";
        
        await _emailService.SendSystemEmailAsync(
            request.Email,
            "You've been invited to join Upkilo!",
            $@"<h2>You're invited!</h2>
               <p>{request.PersonalMessage ?? "Here is an exclusive invite to try our platform."}</p>
               <p>Click <a href='{link}'>here</a> to join using this referral code: <b>{code}</b>.</p>");

        _logger.LogInformation("Referral invite sent to {Email} with code {Code}", request.Email, code);

        return Ok(new
        {
            success = true,
            message = $"Referral invite sent to {request.Email}"
        });
    }
}

// Request DTOs
public class UpdateReferralSettingsRequest
{
    public bool? Enabled { get; set; }
    public decimal? ReferrerRewardAmount { get; set; }
    public string? ReferrerRewardType { get; set; }
    public int? RefereeDiscountPercent { get; set; }
    public int? MaxReferralsPerMonth { get; set; }
    public int? ExpiryDays { get; set; }
    public string? CustomMessage { get; set; }
}

public class ApplyReferralRequest
{
    public string Code { get; set; } = string.Empty;
    public string RefereeEmail { get; set; } = string.Empty;
    public string? RefereeName { get; set; }
}

public class SendReferralInviteRequest
{
    public string Email { get; set; } = string.Empty;
    public string? PersonalMessage { get; set; }
}

