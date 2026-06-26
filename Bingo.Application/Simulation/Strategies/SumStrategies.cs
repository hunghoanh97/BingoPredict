using Bingo.Domain;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation.Strategies;

/// <summary>Cược tổng "nóng" — tổng xuất hiện nhiều nhất trong cửa sổ gần đây.</summary>
public sealed class HotSumStrategy : StrategyBase
{
    public override string Key => "hot_sum";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var window = ctx.Config.GetInt("window", 200);
        var sum = HottestSum(ctx.RecentDraws, window);
        return new[] { new BetDecision(BetKind.Sum, sum.ToString()) };
    }
}

/// <summary>Cược tổng "nguội/đến hẹn" — tổng lâu chưa xuất hiện nhất (gambler's fallacy).</summary>
public sealed class ColdSumStrategy : StrategyBase
{
    public override string Key => "cold_sum";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var window = ctx.Config.GetInt("window", 500);
        var lastSeen = new int[19];
        Array.Fill(lastSeen, int.MaxValue);
        var recent = ctx.RecentDraws;
        for (int i = 0; i < recent.Count && i < window; i++)
        {
            var s = recent[i].Sum;
            if (lastSeen[s] == int.MaxValue) lastSeen[s] = i; // index nhỏ = gần đây
        }
        int coldest = 10, maxGap = -1;
        for (int s = 3; s <= 18; s++)
            if (lastSeen[s] > maxGap) { maxGap = lastSeen[s]; coldest = s; }
        return new[] { new BetDecision(BetKind.Sum, coldest.ToString()) };
    }
}

/// <summary>Cược 2 tổng có xác suất cao nhất: 10 và 11 (mỗi tổng 12.5%).</summary>
public sealed class MostLikelySumStrategy : StrategyBase
{
    public override string Key => "most_likely_sum";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx) => new[]
    {
        new BetDecision(BetKind.Sum, "10"),
        new BetDecision(BetKind.Sum, "11")
    };
}

/// <summary>Phân bổ nhiều vé vào các tổng có tần suất cao nhất trong cửa sổ.</summary>
public sealed class FrequencyWeightedStrategy : StrategyBase
{
    public override string Key => "frequency_weighted";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var window = ctx.Config.GetInt("window", 300);
        var counts = new int[19];
        foreach (var d in ctx.RecentDraws.Take(window)) counts[d.Sum]++;
        var k = Math.Clamp(ctx.Config.GetInt("topK", 5), 1, 5);
        var top = Enumerable.Range(3, 16)
            .OrderByDescending(s => counts[s])
            .Take(k)
            .Select(s => new BetDecision(BetKind.Sum, s.ToString()))
            .ToArray();
        return top;
    }
}

/// <summary>Săn EV cao nhất trong nhóm Cộng tổng: cược 2 tổng có kỳ vọng (xác suất × hệ số) lớn nhất
/// (thường là 3 và 18 — jackpot). Chỉ xét Cộng tổng để khác biệt với nhóm chiến lược Tài/Xỉu.</summary>
public sealed class EvMaxStrategy : StrategyBase
{
    public override string Key => "ev_max";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var scored = new List<(int sum, double ev)>();
        for (int s = 3; s <= 18; s++)
            if (ctx.Multipliers.TryGetValue((BetKind.Sum, s.ToString()), out var m))
                scored.Add((s, BingoRules.SumProbability(s) * (double)m));

        return scored
            .OrderByDescending(x => x.ev)
            .Take(2)
            .Select(x => new BetDecision(BetKind.Sum, x.sum.ToString()))
            .ToArray();
    }
}
