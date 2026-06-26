using Bingo.Domain;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation.Strategies;

/// <summary>
/// MIX nhiều cách chơi: tổng hợp phiếu của 4 tín hiệu (theo cầu / bẻ cầu / size nóng / mean-reversion tổng)
/// cho hướng Lớn vs Nhỏ. CHỈ cược khi đồng thuận mạnh (≥ threshold), còn lại BỎ QUA → giảm số vé.
/// </summary>
public sealed class EnsembleVoteStrategy : StrategyBase
{
    public override string Key => "ensemble_vote";

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var r = ctx.RecentDraws;
        if (r.Count < 5) return Array.Empty<BetDecision>();
        var window = ctx.Config.GetInt("window", 100);
        var threshold = ctx.Config.GetInt("threshold", 3); // cần ít nhất 3/4 phiếu cùng hướng

        int lon = 0, nho = 0;
        void Vote(SizeResult s) { if (s == SizeResult.Lon) lon++; else if (s == SizeResult.Nho) nho++; }

        // 1) Theo cầu: lặp lại size kỳ trước.
        Vote(r[0].Size);
        // 2) Bẻ cầu: ngược size kỳ trước.
        Vote(r[0].Size == SizeResult.Lon ? SizeResult.Nho : SizeResult.Lon);
        // 3) Size nóng trong cửa sổ.
        int cl = 0, cn = 0;
        foreach (var d in r.Take(window)) { if (d.Size == SizeResult.Lon) cl++; else if (d.Size == SizeResult.Nho) cn++; }
        Vote(cl >= cn ? SizeResult.Lon : SizeResult.Nho);
        // 4) Mean-reversion tổng: trung bình gần đây > 10.5 → kỳ vọng về Nhỏ, ngược lại Lớn.
        double avg = r.Take(20).Average(d => d.Sum);
        Vote(avg > 10.5 ? SizeResult.Nho : SizeResult.Lon);

        if (Math.Max(lon, nho) < threshold) return Array.Empty<BetDecision>(); // không đồng thuận → bỏ qua
        var side = lon > nho ? SizeResult.Lon : SizeResult.Nho;
        return new[] { new BetDecision(BetKind.Size, side.ToString()) };
    }
}

/// <summary>
/// Kelly phân số: cược một TỈ LỆ nhỏ của số dư vào cửa EV tốt nhất (Hòa). Khi thua, mức cược tự co lại
/// (không bao giờ cháy nhanh); khi thắng, cược nhích lên. Biến động thấp, lỗ chậm.
/// </summary>
public sealed class KellyFractionStrategy : StrategyBase
{
    public override string Key => "kelly_frac";

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var f = Math.Clamp(ctx.Config.GetDouble("fraction", 0.02), 0.005, 0.2);
        var target = ctx.Config.GetString("target", nameof(SizeResult.Hoa));
        var raw = ctx.Balance * (decimal)f;
        var units = Math.Max(1, (int)(raw / GameConstants.TicketPrice));
        var stake = GameConstants.TicketPrice * units;
        return new[] { new BetDecision(BetKind.Size, target, stake) };
    }
}
