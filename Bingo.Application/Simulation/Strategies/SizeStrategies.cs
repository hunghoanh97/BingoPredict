using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation.Strategies;

/// <summary>Luôn cược Lớn (Tài).</summary>
public sealed class AlwaysTaiStrategy : StrategyBase
{
    public override string Key => "always_tai";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx) =>
        new[] { new BetDecision(BetKind.Size, nameof(SizeResult.Lon)) };
}

/// <summary>Luôn cược Nhỏ (Xỉu).</summary>
public sealed class AlwaysXiuStrategy : StrategyBase
{
    public override string Key => "always_xiu";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx) =>
        new[] { new BetDecision(BetKind.Size, nameof(SizeResult.Nho)) };
}

/// <summary>Luôn cược Hòa (tổng 10/11) — xác suất 25%, trả thưởng cao hơn Tài/Xỉu.</summary>
public sealed class AlwaysHoaStrategy : StrategyBase
{
    public override string Key => "always_hoa";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx) =>
        new[] { new BetDecision(BetKind.Size, nameof(SizeResult.Hoa)) };
}

/// <summary>Theo xu hướng: cược cùng khoảng với kỳ gần nhất.</summary>
public sealed class StreakFollowStrategy : StrategyBase
{
    public override string Key => "streak_follow";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var last = ctx.RecentDraws.Count > 0 ? ctx.RecentDraws[0].Size : SizeResult.Lon;
        return new[] { new BetDecision(BetKind.Size, last.ToString()) };
    }
}

/// <summary>Đảo xu hướng: cược ngược khoảng của kỳ gần nhất (Hòa -> Lớn).</summary>
public sealed class StreakReverseStrategy : StrategyBase
{
    public override string Key => "streak_reverse";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var last = ctx.RecentDraws.Count > 0 ? ctx.RecentDraws[0].Size : SizeResult.Hoa;
        var opposite = last switch
        {
            SizeResult.Lon => SizeResult.Nho,
            SizeResult.Nho => SizeResult.Lon,
            _ => SizeResult.Lon
        };
        return new[] { new BetDecision(BetKind.Size, opposite.ToString()) };
    }
}
