using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers;

/// <summary>
/// Loyalty controller for rewards and points program.
/// Uses LoyaltyService + direct DB queries for members, settings, analytics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class LoyaltyController : ControllerBase
{
    private readonly ILogger<LoyaltyController> _logger;
    private readonly LoyaltyService _loyaltyService;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public LoyaltyController(
        ILogger<LoyaltyController> logger,
        LoyaltyService loyaltyService,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _loyaltyService = loyaltyService;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get loyalty program settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var program = await _context.LoyaltyPrograms
            .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        if (program == null)
            return Ok(new { enabled = false, message = "No loyalty program configured." });

        return Ok(new
        {
            id = program.Id,
            enabled = program.IsActive,
            programName = program.Name,
            pointsPerDollar = program.PointsPerDollar,
            pointsRedemptionRate = program.PointsRedemptionRate,
            referralBonusPoints = program.ReferralBonusPoints,
            tiers = program.Tiers, // JSON string
            pointExpiryDays = program.PointExpiryDays,
            program.CreatedAt,
            program.UpdatedAt
        });
    }

    /// <summary>
    /// Update loyalty program settings
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateLoyaltySettingsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var program = await _context.LoyaltyPrograms
            .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        if (program == null)
        {
            program = new LoyaltyProgram { TenantId = tenantId.Value };
            _context.LoyaltyPrograms.Add(program);
        }

        if (request.Enabled.HasValue) program.IsActive = request.Enabled.Value;
        if (request.ProgramName != null) program.Name = request.ProgramName;
        if (request.PointsPerDollar.HasValue) program.PointsPerDollar = request.PointsPerDollar.Value;
        if (request.PointsRedemptionRate.HasValue) program.PointsRedemptionRate = request.PointsRedemptionRate.Value;
        if (request.ExpiryMonths.HasValue) program.PointExpiryDays = request.ExpiryMonths.Value * 30;
        program.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Loyalty settings updated for tenant {TenantId}", tenantId);
        return Ok(new { success = true, program.UpdatedAt });
    }

    /// <summary>
    /// Configure loyalty tiers (acts as rewards levels)
    /// </summary>
    [HttpPost("settings/tiers")]
    public async Task<IActionResult> ConfigureTiers([FromBody] List<LoyaltyTierDto> tiers)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var program = await _context.LoyaltyPrograms
            .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        if (program == null)
            return NotFound("Loyalty program not found. Please update settings first.");

        program.Tiers = System.Text.Json.JsonSerializer.Serialize(tiers);
        program.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Loyalty tiers configured for tenant {TenantId}", tenantId);
        return Ok(new { success = true, program.Tiers });
    }

    /// <summary>
    /// Get all loyalty members (clients with loyalty balances)
    /// </summary>
    [HttpGet("members")]
    public async Task<IActionResult> GetMembers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? tier = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.LoyaltyBalances
            .Include(lb => lb.Client)
            .Where(lb => lb.TenantId == tenantId.Value && !lb.IsDeleted);

        if (!string.IsNullOrEmpty(tier))
            query = query.Where(lb => lb.CurrentTier == tier);

        var total = await query.CountAsync();

        var members = await query
            .OrderByDescending(lb => lb.TotalPoints)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(lb => new
            {
                id = lb.Id,
                clientId = lb.ClientId,
                clientName = lb.Client != null ? $"{lb.Client.FirstName} {lb.Client.LastName}" : "Unknown",
                email = lb.Client != null ? lb.Client.Email : null,
                points = lb.TotalPoints,
                lifetimePoints = lb.LifetimePoints,
                tier = lb.CurrentTier,
                stampCount = lb.StampCount,
                lb.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = members, total, page, pageSize });
    }

    /// <summary>
    /// Get member details
    /// </summary>
    [HttpGet("members/{id}")]
    public async Task<IActionResult> GetMember(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var balance = await _context.LoyaltyBalances
            .Include(lb => lb.Client)
            .FirstOrDefaultAsync(lb => lb.Id == id && lb.TenantId == tenantId.Value && !lb.IsDeleted);

        if (balance == null) return NotFound();

        // Get recent activity from CreditTransactions
        var recentActivity = await _context.CreditTransactions
            .Where(ct => ct.ClientId == balance.ClientId && ct.TenantId == tenantId.Value &&
                (ct.Type == CreditTransactionType.LoyaltyEarn || ct.Type == CreditTransactionType.LoyaltyRedeem))
            .OrderByDescending(ct => ct.CreatedAt)
            .Take(10)
            .Select(ct => new
            {
                type = ct.Type == CreditTransactionType.LoyaltyEarn ? "earned" : "redeemed",
                points = (int)ct.Amount,
                ct.Description,
                date = ct.CreatedAt
            })
            .ToListAsync();

        // Calculate next tier
        var nextTier = balance.CurrentTier switch
        {
            "Bronze" => new { name = "Silver", pointsNeeded = Math.Max(0, 500 - balance.TotalPoints) },
            "Silver" => new { name = "Gold", pointsNeeded = Math.Max(0, 2000 - balance.TotalPoints) },
            "Gold" => new { name = "Platinum", pointsNeeded = Math.Max(0, 5000 - balance.TotalPoints) },
            _ => (dynamic?)null
        };

        return Ok(new
        {
            balance.Id,
            balance.ClientId,
            clientName = balance.Client != null ? $"{balance.Client.FirstName} {balance.Client.LastName}" : "Unknown",
            email = balance.Client?.Email,
            phone = balance.Client?.Phone,
            points = balance.TotalPoints,
            lifetimePoints = balance.LifetimePoints,
            tier = balance.CurrentTier,
            nextTier = nextTier?.name,
            pointsToNextTier = nextTier?.pointsNeeded,
            stampCount = balance.StampCount,
            recentActivity,
            balance.CreatedAt
        });
    }

    /// <summary>
    /// Get specific member's tier and upcoming rewards
    /// </summary>
    [HttpGet("members/{id}/tier")]
    public async Task<IActionResult> GetLoyaltyTier(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var balance = await _context.LoyaltyBalances
            .FirstOrDefaultAsync(lb => lb.Id == id && lb.TenantId == tenantId.Value && !lb.IsDeleted);

        if (balance == null) return NotFound();

        var program = await _context.LoyaltyPrograms
            .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        var tiersJson = program?.Tiers ?? "[]";
        var tiersList = System.Text.Json.JsonSerializer.Deserialize<List<LoyaltyTierDto>>(tiersJson) ?? new List<LoyaltyTierDto>();

        var currentTierObj = tiersList.FirstOrDefault(t => t.Name == balance.CurrentTier);
        var nextTierObj = tiersList.Where(t => t.MinPoints > balance.TotalPoints).OrderBy(t => t.MinPoints).FirstOrDefault();

        return Ok(new
        {
            balance.ClientId,
            currentTier = balance.CurrentTier,
            currentPoints = balance.TotalPoints,
            benefits = currentTierObj?.Benefits ?? new List<string>(),
            nextTier = nextTierObj?.Name,
            pointsNeededForNextTier = nextTierObj != null ? nextTierObj.MinPoints - balance.TotalPoints : 0
        });
    }

    /// <summary>
    /// Enroll client in loyalty program
    /// </summary>
    [HttpPost("members")]
    public async Task<IActionResult> EnrollMember([FromBody] EnrollLoyaltyMemberRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Check if already enrolled
        var existing = await _context.LoyaltyBalances
            .AnyAsync(lb => lb.ClientId == request.ClientId && lb.TenantId == tenantId.Value && !lb.IsDeleted);

        if (existing)
            return BadRequest(new { error = "Client is already enrolled in the loyalty program." });

        var balance = new LoyaltyBalance
        {
            TenantId = tenantId.Value,
            ClientId = request.ClientId,
            TotalPoints = request.InitialPoints ?? 0,
            LifetimePoints = request.InitialPoints ?? 0,
            CurrentTier = "Bronze"
        };

        _context.LoyaltyBalances.Add(balance);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client enrolled in loyalty: {ClientId}", request.ClientId);

        return CreatedAtAction(nameof(GetMember), new { id = balance.Id }, new
        {
            balance.Id,
            balance.ClientId,
            points = balance.TotalPoints,
            tier = balance.CurrentTier,
            enrolledAt = balance.CreatedAt
        });
    }

    /// <summary>
    /// Add points to member
    /// </summary>
    [HttpPost("members/{id}/add-points")]
    public async Task<IActionResult> AddPoints(Guid id, [FromBody] AddPointsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var balance = await _context.LoyaltyBalances.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (balance == null) return NotFound();

        await _loyaltyService.AwardPointsAsync(balance.ClientId, request.Points, request.Reason ?? "Manual award");
        var summary = await _loyaltyService.GetSummaryAsync(balance.ClientId);

        return Ok(new
        {
            success = true,
            pointsAdded = request.Points,
            newBalance = summary.Points,
            newTier = summary.Tier
        });
    }

    /// <summary>
    /// Redeem points
    /// </summary>
    [HttpPost("members/{id}/redeem")]
    public async Task<IActionResult> RedeemPoints(Guid id, [FromBody] RedeemPointsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.Points <= 0)
            return BadRequest(new { error = "Points to redeem must be greater than zero." });

        var balance = await _context.LoyaltyBalances.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (balance == null) return NotFound();

        if (balance.TotalPoints < request.Points)
            return BadRequest(new { error = "Insufficient points balance." });

        try
        {
            await _loyaltyService.RedeemPointsAsync(balance.ClientId, request.Points, request.Reason ?? "Points redemption");
            var summary = await _loyaltyService.GetSummaryAsync(balance.ClientId);
            var discount = request.Points / 100m;

            return Ok(new
            {
                success = true,
                pointsRedeemed = request.Points,
                discountAmount = discount,
                newBalance = summary.Points,
                redemptionCode = $"LYL-{balance.Id.ToString()[..8].ToUpper()}"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get points history for member
    /// </summary>
    [HttpGet("members/{id}/history")]
    public async Task<IActionResult> GetPointsHistory(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var balance = await _context.LoyaltyBalances.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (balance == null) return NotFound();

        var history = await _loyaltyService.GetHistoryAsync(balance.ClientId);
        var paged = history.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new { data = paged, total = history.Count, page, pageSize });
    }

    /// <summary>
    /// Get loyalty analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var balances = await _context.LoyaltyBalances
            .Where(lb => lb.TenantId == tenantId.Value && !lb.IsDeleted)
            .ToListAsync();

        var totalMembers = balances.Count;
        var totalPointsIssued = balances.Sum(b => b.LifetimePoints);
        var totalPointsBalance = balances.Sum(b => b.TotalPoints);

        var tierBreakdown = balances
            .GroupBy(b => b.CurrentTier)
            .ToDictionary(g => g.Key.ToLower(), g => g.Count());

        return Ok(new
        {
            totalMembers,
            totalPointsIssued,
            totalPointsBalance,
            totalPointsRedeemed = totalPointsIssued - totalPointsBalance,
            averagePointsPerMember = totalMembers > 0 ? totalPointsBalance / totalMembers : 0,
            membersByTier = tierBreakdown,
            redemptionRate = totalPointsIssued > 0
                ? Math.Round((double)(totalPointsIssued - totalPointsBalance) / totalPointsIssued * 100, 1) : 0
        });
    }

    /// <summary>
    /// Add a stamp to client's stamp card (buy 10 get 1 free style)
    /// </summary>
    [HttpPost("members/{id}/stamps")]
    public async Task<IActionResult> AddStamp(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var balance = await _context.LoyaltyBalances
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

        if (balance == null) return NotFound();

        balance.StampCount += 1;
        var rewardEarned = false;

        // E.g., every 10 stamps = 1 reward/free service
        if (balance.StampCount >= 10)
        {
            rewardEarned = true;
            balance.StampCount -= 10;
            // Additional logic to grant a free service pass could go here
            await _loyaltyService.AwardPointsAsync(balance.ClientId, 500, "Stamp Card Completed Reward");
        }

        balance.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, stampCount = balance.StampCount, rewardEarned });
    }

    /// <summary>
    /// Issue referral bonus points
    /// </summary>
    [HttpPost("members/{id}/referral")]
    public async Task<IActionResult> AddReferralBonus(Guid id, [FromBody] ReferralBonusRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var balance = await _context.LoyaltyBalances
            .Include(lb => lb.Client)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

        if (balance == null) return NotFound();

        var program = await _context.LoyaltyPrograms
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && !p.IsDeleted);

        int bonusPoints = program?.ReferralBonusPoints ?? 200; // Default fallback

        await _loyaltyService.AwardPointsAsync(balance.ClientId, bonusPoints, $"Referral Bonus: {request.ReferredClientName}");

        return Ok(new { success = true, bonusPointsIssued = bonusPoints, newBalance = balance.TotalPoints + bonusPoints });
    }

    /// <summary>
    /// Create a loyalty reward (redeemable item)
    /// </summary>
    [HttpPost("rewards")]
    public async Task<IActionResult> CreateLoyaltyReward([FromBody] CreateLoyaltyRewardRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Reward name is required." });

        if (request.PointsCost <= 0)
            return BadRequest(new { error = "Points cost must be greater than zero." });

        var program = await _context.LoyaltyPrograms
            .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        if (program == null)
            return BadRequest(new { error = "No loyalty program configured. Please update settings first." });

        var reward = new LoyaltyReward
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            LoyaltyProgramId = program.Id,
            Name = request.Name,
            Description = request.Description,
            PointsCost = request.PointsCost,
            RewardType = request.RewardType ?? "Discount",
            RewardValue = request.RewardValue,
            IsActive = true,
            MaxRedemptions = request.MaxRedemptions,
            TimesRedeemed = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<LoyaltyReward>().Add(reward);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Loyalty reward created: {RewardId} - {Name} ({PointsCost} pts)", reward.Id, reward.Name, reward.PointsCost);

        return CreatedAtAction(nameof(GetLoyaltyRewards), null, new
        {
            reward.Id,
            reward.Name,
            reward.Description,
            reward.PointsCost,
            reward.RewardType,
            reward.RewardValue,
            reward.IsActive,
            reward.CreatedAt
        });
    }

    /// <summary>
    /// Get all loyalty rewards
    /// </summary>
    [HttpGet("rewards")]
    public async Task<IActionResult> GetLoyaltyRewards()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var rewards = await _context.Set<LoyaltyReward>()
            .Where(r => r.TenantId == tenantId.Value && r.IsActive)
            .OrderBy(r => r.PointsCost)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.PointsCost,
                r.RewardType,
                r.RewardValue,
                r.IsActive,
                r.MaxRedemptions,
                r.TimesRedeemed,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = rewards });
    }
}

// Request DTOs
public class UpdateLoyaltySettingsRequest
{
    public bool? Enabled { get; set; }
    public string? ProgramName { get; set; }
    public int? PointsPerDollar { get; set; }
    public int? PointsRedemptionRate { get; set; }
    public int? ExpiryMonths { get; set; }
}

public class EnrollLoyaltyMemberRequest
{
    public Guid ClientId { get; set; }
    public int? InitialPoints { get; set; }
}

public class AddPointsRequest
{
    public int Points { get; set; }
    public string? Reason { get; set; }
    public Guid? BookingId { get; set; }
}

public class RedeemPointsRequest
{
    public int Points { get; set; }
    public string? Reason { get; set; }
    public Guid? BookingId { get; set; }
}

public class ReferralBonusRequest
{
    public string ReferredClientName { get; set; } = string.Empty;
}

public class LoyaltyTierDto
{
    public string Name { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public List<string> Benefits { get; set; } = new();
}

public class CreateLoyaltyRewardRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PointsCost { get; set; }
    public string? RewardType { get; set; } // "Discount", "FreeService", "Product", "GiftCard"
    public decimal? RewardValue { get; set; }
    public int? MaxRedemptions { get; set; }
}

