using Bingo.Domain;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation.Strategies;

/// <summary>Dùng mô hình ML.NET dự đoán tổng kế tiếp.</summary>
public sealed class MlPredictStrategy : StrategyBase
{
    public override string Key => "ml_predict";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var sum = ctx.PredictionModel?.PredictSum(ctx.RecentDraws) ?? 10;
        sum = Math.Clamp(sum, 3, 18);
        return new[] { new BetDecision(BetKind.Sum, sum.ToString()) };
    }
}

/// <summary>Hedge như ví dụ thực tế: cược Lớn + 2 vé jackpot (tổng 3 và 18).</summary>
public sealed class HedgeJackpotStrategy : StrategyBase
{
    public override string Key => "hedge_jackpot";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx) => new[]
    {
        new BetDecision(BetKind.Size, nameof(SizeResult.Lon)),
        new BetDecision(BetKind.Sum, "3"),
        new BetDecision(BetKind.Sum, "18")
    };
}

/// <summary>Baseline ngẫu nhiên — mốc so sánh.</summary>
public sealed class RandomBaselineStrategy : StrategyBase
{
    public override string Key => "random";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        if (ctx.Rng.NextDouble() < 0.5)
        {
            var size = ctx.Rng.Next(2) == 0 ? SizeResult.Nho : SizeResult.Lon;
            return new[] { new BetDecision(BetKind.Size, size.ToString()) };
        }
        var sum = ctx.Rng.Next(3, 19);
        return new[] { new BetDecision(BetKind.Sum, sum.ToString()) };
    }
}

/// <summary>Cách chơi Cơ bản: cược digit "nóng" nhất theo số lần xuất hiện trong cửa sổ.</summary>
public sealed class NumberHunterStrategy : StrategyBase
{
    public override string Key => "number_hunter";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var window = ctx.Config.GetInt("window", 200);
        var counts = new int[7];
        foreach (var d in ctx.RecentDraws.Take(window))
        {
            counts[d.D1]++; counts[d.D2]++; counts[d.D3]++;
        }
        int best = 1, bestC = -1;
        for (int digit = 1; digit <= 6; digit++) if (counts[digit] > bestC) { bestC = counts[digit]; best = digit; }
        return new[] { new BetDecision(BetKind.NumberCount, best.ToString()) };
    }
}
