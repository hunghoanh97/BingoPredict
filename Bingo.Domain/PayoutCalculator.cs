using Bingo.Domain.Entities;
using Bingo.Domain.Enums;

namespace Bingo.Domain;

/// <summary>Kết quả settle một vé.</summary>
public readonly record struct SettlementResult(bool IsWin, decimal Multiplier, decimal Payout);

/// <summary>
/// Tính thắng/thua và tiền thưởng của một vé dựa trên kết quả quay và bảng hệ số.
/// Pure logic — dùng được cả khi settle live lẫn khi backtest/tuner.
/// </summary>
public static class PayoutCalculator
{
    /// <summary>
    /// Settle một vé. <paramref name="multipliers"/> map (BetKind, BetValue chuẩn hóa) -> hệ số.
    /// </summary>
    public static SettlementResult Settle(
        BetKind kind,
        string betValue,
        decimal stake,
        Draw draw,
        IReadOnlyDictionary<(BetKind, string), decimal> multipliers)
    {
        bool win;
        string ruleKey;

        switch (kind)
        {
            case BetKind.Sum:
                var target = int.Parse(betValue);
                win = draw.Sum == target;
                ruleKey = betValue;
                break;

            case BetKind.Size:
                var size = Enum.Parse<SizeResult>(betValue, ignoreCase: true);
                win = draw.Size == size;
                ruleKey = size.ToString();
                break;

            case BetKind.NumberCount:
                var digit = int.Parse(betValue);
                var count = BingoRules.CountDigit(digit, draw.D1, draw.D2, draw.D3);
                win = count > 0;
                ruleKey = count.ToString(); // "1" | "2" | "3"
                break;

            case BetKind.Triple:
                if (betValue.StartsWith("specific:", StringComparison.OrdinalIgnoreCase))
                {
                    var td = int.Parse(betValue.Split(':')[1]);
                    win = draw.IsTriple && draw.TripleDigit == td;
                    ruleKey = "specific";
                }
                else
                {
                    win = draw.IsTriple;
                    ruleKey = "any";
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        if (!win)
            return new SettlementResult(false, 0m, 0m);

        var mult = multipliers.TryGetValue((kind, ruleKey), out var m) ? m : 0m;
        return new SettlementResult(true, mult, stake * mult);
    }
}
