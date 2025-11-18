using Bingo.ApiService.Models.Entities;
using System.Text.Json;

namespace Bingo.ApiService.Services;

public class PrizeCalculationService : IPrizeCalculationService
{
    private readonly Dictionary<BetType, decimal> _payoutMultipliers;

    public PrizeCalculationService()
    {
        _payoutMultipliers = new Dictionary<BetType, decimal>
        {
            { BetType.SingleNumber, 1.95m },     // 1:1.95 payout
            { BetType.MatchingNumbers, 3.5m }, // 1:3.5 payout for 2 matching, 1:210 for 3 matching
            { BetType.TotalSum, 1.95m },       // Varies by sum, default 1:1.95
            { BetType.Size, 1.95m }             // 1:1.95 payout
        };
    }

    public decimal CalculatePrize(BetType betType, string? betNumbers, List<int> drawnNumbers, decimal betAmount)
    {
        if (!IsWinningBet(betType, betNumbers, drawnNumbers))
            return 0;

        var multiplier = GetMultiplier(betType, betNumbers, drawnNumbers);
        return betAmount * multiplier;
    }

    public decimal GetMultiplier(BetType betType, string? betNumbers, List<int> drawnNumbers)
    {
        if (!IsWinningBet(betType, betNumbers, drawnNumbers))
            return 0;

        return betType switch
        {
            BetType.SingleNumber => 1.95m,
            BetType.MatchingNumbers => GetMatchingNumbersMultiplier(betNumbers, drawnNumbers),
            BetType.TotalSum => GetTotalSumMultiplier(betNumbers, drawnNumbers),
            BetType.Size => 1.95m,
            _ => 0
        };
    }

    public bool IsWinningBet(BetType betType, string? betNumbers, List<int> drawnNumbers)
    {
        if (string.IsNullOrEmpty(betNumbers) || drawnNumbers == null || drawnNumbers.Count != 3)
            return false;

        return betType switch
        {
            BetType.SingleNumber => IsSingleNumberWin(betNumbers, drawnNumbers),
            BetType.MatchingNumbers => IsMatchingNumbersWin(betNumbers, drawnNumbers),
            BetType.TotalSum => IsTotalSumWin(betNumbers, drawnNumbers),
            BetType.Size => IsSizeWin(betNumbers, drawnNumbers),
            _ => false
        };
    }

    public Dictionary<BetType, decimal> GetPayoutMultipliers()
    {
        return new Dictionary<BetType, decimal>(_payoutMultipliers);
    }

    private bool IsSingleNumberWin(string betNumbers, List<int> drawnNumbers)
    {
        try
        {
            var betNumber = JsonSerializer.Deserialize<int>(betNumbers);
            return drawnNumbers.Contains(betNumber) && betNumber >= 1 && betNumber <= 6;
        }
        catch
        {
            return false;
        }
    }

    private bool IsMatchingNumbersWin(string betNumbers, List<int> drawnNumbers)
    {
        try
        {
            var betArray = JsonSerializer.Deserialize<int[]>(betNumbers);
            if (betArray == null || betArray.Length != 3)
                return false;

            var matches = 0;
            for (int i = 0; i < 3; i++)
            {
                if (drawnNumbers.Contains(betArray[i]))
                    matches++;
            }

            return matches >= 2; // Win if 2 or 3 numbers match
        }
        catch
        {
            return false;
        }
    }

    private bool IsTotalSumWin(string betNumbers, List<int> drawnNumbers)
    {
        try
        {
            var betSum = JsonSerializer.Deserialize<int>(betNumbers);
            var actualSum = drawnNumbers.Sum();
            return betSum == actualSum;
        }
        catch
        {
            return false;
        }
    }

    private bool IsSizeWin(string betNumbers, List<int> drawnNumbers)
    {
        try
        {
            var betSize = JsonSerializer.Deserialize<SizeResult>(betNumbers);
            var actualSum = drawnNumbers.Sum();
            var actualSize = GetSizeResult(actualSum);
            
            return betSize == actualSize;
        }
        catch
        {
            return false;
        }
    }

    private decimal GetMatchingNumbersMultiplier(string betNumbers, List<int> drawnNumbers)
    {
        try
        {
            var betArray = JsonSerializer.Deserialize<int[]>(betNumbers);
            if (betArray == null || betArray.Length != 3)
                return 0;

            var matches = 0;
            for (int i = 0; i < 3; i++)
            {
                if (drawnNumbers.Contains(betArray[i]))
                    matches++;
            }

            return matches switch
            {
                2 => 3.5m,   // 2 matching numbers: 1:3.5
                3 => 210m,   // 3 matching numbers: 1:210
                _ => 0
            };
        }
        catch
        {
            return 0;
        }
    }

    private decimal GetTotalSumMultiplier(string betNumbers, List<int> drawnNumbers)
    {
        try
        {
            var betSum = JsonSerializer.Deserialize<int>(betNumbers);
            var actualSum = drawnNumbers.Sum();
            
            if (betSum == actualSum)
            {
                // Special multipliers for specific sums
                return betSum switch
                {
                    3 or 18 => 210m,  // Triple 1 or Triple 6: 1:210
                    4 or 17 => 70m,   // Special combinations: 1:70
                    5 or 16 => 35m,   // Other rare combinations: 1:35
                    _ => 1.95m        // Default: 1:1.95
                };
            }
            
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private SizeResult GetSizeResult(int sum)
    {
        return sum switch
        {
            >= 3 and <= 9 => SizeResult.Small,
            >=10 and <=11 => SizeResult.Tie,
            >= 12 and <= 18 => SizeResult.Large,
            _ => throw new ArgumentOutOfRangeException(nameof(sum), "Sum must be between 3 and 18")
        };
    }
}