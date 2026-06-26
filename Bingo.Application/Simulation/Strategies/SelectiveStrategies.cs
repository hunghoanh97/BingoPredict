using Bingo.Domain;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation.Strategies;

/// <summary>Chơi CÁCH KỲ: chỉ cược Lớn mỗi N kỳ, các kỳ còn lại bỏ qua (giảm số vé → giảm lỗ tuyệt đối).</summary>
public sealed class SparseTaiStrategy : StrategyBase
{
    public override string Key => "sparse_tai";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        if (ctx.RecentDraws.Count == 0) return Array.Empty<BetDecision>();
        var n = Math.Max(2, ctx.Config.GetInt("everyN", 3));
        // "Slot" theo thời gian quay (6 phút/kỳ) → cách kỳ ổn định cả khi live lẫn backtest.
        var slot = (int)(ctx.RecentDraws[0].DrawAt.TimeOfDay.TotalMinutes / GameConstants.DrawIntervalMinutes);
        if (slot % n != 0) return Array.Empty<BetDecision>();
        return new[] { new BetDecision(BetKind.Size, nameof(SizeResult.Lon)) };
    }
}

/// <summary>Chơi CHỌN LỌC: chỉ cược khi có chuỗi K kỳ cùng khoảng (cầu), rồi cược NGƯỢC (bẻ cầu); còn lại bỏ qua.</summary>
public sealed class StreakBreakStrategy : StrategyBase
{
    public override string Key => "streak_break";
    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var k = Math.Max(2, ctx.Config.GetInt("streak", 3));
        if (ctx.RecentDraws.Count < k) return Array.Empty<BetDecision>();

        var first = ctx.RecentDraws[0].Size;
        for (int i = 1; i < k; i++)
            if (ctx.RecentDraws[i].Size != first) return Array.Empty<BetDecision>(); // chưa đủ chuỗi → bỏ qua kỳ

        var opposite = first switch
        {
            SizeResult.Lon => SizeResult.Nho,
            SizeResult.Nho => SizeResult.Lon,
            _ => SizeResult.Lon
        };
        return new[] { new BetDecision(BetKind.Size, opposite.ToString()) };
    }
}
