using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Production loyalty points system.
/// 
/// Earn rules: 1 point per $1 spent, 50pt first-booking bonus,
/// 25pt referral, 10pt review, 2x on birthdays.
/// 
/// Tiers: Bronze(0) → Silver(500) → Gold(2000) → Platinum(5000)
/// Redemption: 100 points = $1 discount
/// </summary>
public class LoyaltyService : ILoyaltyService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LoyaltyService> _logger;

    public LoyaltyService(AppDbContext context, ILogger<LoyaltyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Tier Configuration ──────────────────────────────────
    private static readonly (string Tier, int MinPoints, decimal DiscountPct)[] TierConfig =
    {
        ("Platinum", 5000, 0.15m),
        ("Gold", 2000, 0.10m),
        ("Silver", 500, 0.05m),
        ("Bronze", 0, 0m)
    };

    public Task<int> CalculatePointsAsync(decimal amountSpent)
    {
        // 1 point per $1 spent (rounded down)
        return Task.FromResult((int)Math.Floor(amountSpent));
    }

    public async Task AwardPointsAsync(Guid clientId, int points, string reason)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return;

        client.LoyaltyPoints += points;

        // Log to CreditTransaction ledger
        _context.Set<CreditTransaction>().Add(new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = client.TenantId,
            ClientId = clientId,
            Amount = points,
            Type = CreditTransactionType.LoyaltyEarn,
            Description = reason
        });

        await UpdateClientTierAsync(clientId);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Awarded {Points} pts to client {ClientId}: {Reason}", points, clientId, reason);
    }

    public async Task RedeemPointsAsync(Guid clientId, int points, string reason)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return;

        if (client.LoyaltyPoints < points)
            throw new InvalidOperationException($"Insufficient points. Available: {client.LoyaltyPoints}");

        if (points < 100)
            throw new InvalidOperationException("Minimum redemption is 100 points ($1.00)");

        client.LoyaltyPoints -= points;
        var dollarValue = points / 100m;

        _context.Set<CreditTransaction>().Add(new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = client.TenantId,
            ClientId = clientId,
            Amount = -points,
            Type = CreditTransactionType.LoyaltyRedeem,
            Description = $"{reason} — ${dollarValue:F2} discount"
        });

        await UpdateClientTierAsync(clientId);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Redeemed {Points} pts from client {ClientId}: {Reason}", points, clientId, reason);
    }

    /// <summary>
    /// Auto-award points after a completed booking.
    /// </summary>
    public async Task AwardBookingPointsAsync(Guid clientId, decimal amountPaid, Guid bookingId, bool isFirstBooking = false)
    {
        var basePoints = await CalculatePointsAsync(amountPaid);

        // Birthday 2x multiplier
        var client = await _context.Clients.FindAsync(clientId);
        if (client?.DateOfBirth != null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (client.DateOfBirth.Value.Month == today.Month && client.DateOfBirth.Value.Day == today.Day)
                basePoints *= 2;
        }

        if (isFirstBooking)
            basePoints += 50; // First booking bonus

        if (basePoints > 0)
            await AwardPointsAsync(clientId, basePoints,
                isFirstBooking ? "First booking bonus + service" : $"Booking #{bookingId.ToString()[..8]}");
    }

    public async Task UpdateClientTierAsync(Guid clientId)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return;

        var oldTier = client.LoyaltyTier;
        client.LoyaltyTier = client.LoyaltyPoints switch
        {
            >= 5000 => "Platinum",
            >= 2000 => "Gold",
            >= 500 => "Silver",
            _ => "Bronze"
        };

        if (oldTier != client.LoyaltyTier)
        {
            _logger.LogInformation("Client {ClientId} tier: {Old} → {New}", clientId, oldTier, client.LoyaltyTier);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get loyalty summary with perks and progress.
    /// </summary>
    public async Task<LoyaltySummary> GetSummaryAsync(Guid clientId)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return new LoyaltySummary("Bronze", 0, 0m, 500, Array.Empty<string>());

        var discount = TierConfig.First(t => t.Tier == client.LoyaltyTier).DiscountPct;
        var nextTier = TierConfig.Reverse().SkipWhile(t => t.MinPoints <= client.LoyaltyPoints).FirstOrDefault();
        var toNext = nextTier.MinPoints > 0 ? nextTier.MinPoints - client.LoyaltyPoints : 0;

        return new LoyaltySummary(client.LoyaltyTier, client.LoyaltyPoints, discount, Math.Max(0, toNext), GetPerks(client.LoyaltyTier));
    }

    public async Task<List<Upkilo.Core.Interfaces.LoyaltyTransaction>> GetHistoryAsync(Guid clientId)
    {
        var transactions = await _context.Set<CreditTransaction>()
            .Where(t => t.ClientId == clientId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync();

        return transactions.Select(t => new Upkilo.Core.Interfaces.LoyaltyTransaction
        {
            Points = (int)t.Amount,
            Description = t.Description ?? "",
            Date = t.CreatedAt,
            Type = t.Amount > 0 ? "Earned" : "Redeemed"
        }).ToList();
    }

    private static string[] GetPerks(string tier) => tier switch
    {
        "Platinum" => new[] { "15% off services", "Priority booking", "Free add-on", "Birthday 2x points", "Exclusive offers" },
        "Gold" => new[] { "10% off services", "Priority booking", "Birthday 2x points" },
        "Silver" => new[] { "5% off services", "Birthday 2x points" },
        _ => new[] { "Earn 1pt per $1 spent", "Birthday 2x points" }
    };
}

public record LoyaltySummary(string Tier, int Points, decimal DiscountPercent, int PointsToNextTier, string[] Perks);
