using Bingo.ApiService.Models.Entities;

namespace Bingo.ApiService.Services;

public interface IPrizeCalculationService
{
    decimal CalculatePrize(BetType betType, string? betNumbers, List<int> drawnNumbers, decimal betAmount);
    decimal GetMultiplier(BetType betType, string? betNumbers, List<int> drawnNumbers);
    bool IsWinningBet(BetType betType, string? betNumbers, List<int> drawnNumbers);
    Dictionary<BetType, decimal> GetPayoutMultipliers();
}

public class PrizeCalculationResult
{
    public bool IsWin { get; set; }
    public decimal WinAmount { get; set; }
    public decimal Multiplier { get; set; }
    public string ResultDescription { get; set; } = string.Empty;
}