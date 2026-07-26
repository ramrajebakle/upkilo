using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ILoyaltyService
{
    Task<int> CalculatePointsAsync(decimal amountSpent);
    Task UpdateClientTierAsync(Guid clientId);
    Task AwardPointsAsync(Guid clientId, int points, string reason);
    Task RedeemPointsAsync(Guid clientId, int points, string reason);
    Task<List<LoyaltyTransaction>> GetHistoryAsync(Guid clientId);
}

public class LoyaltyTransaction
{
    public DateTime Date { get; set; }
    public int Points { get; set; } // + or -
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Earned"; // Earned, Redeemed, Adjustment
}
