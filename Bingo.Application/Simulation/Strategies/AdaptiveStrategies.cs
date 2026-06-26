using Bingo.Domain;
using Bingo.Domain.Entities;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation.Strategies;

/// <summary>Martingale trên Tài/Xỉu: THUA thì gấp đôi MỨC CƯỢC (10k→20k→40k...), THẮNG reset về mức gốc.
/// Mục tiêu phục hồi lỗ và chốt lãi nhỏ mỗi chuỗi thắng.</summary>
public sealed class MartingaleSizeStrategy : StrategyBase
{
    public override string Key => "martingale_size";
    public override bool IsAdaptive => true;

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        if (ctx.Balance >= GameConstants.DailyBudget)
        {
            ctx.State.SetScalar("stake", (double)GameConstants.TicketPrice);
            ctx.State.SetScalar("won", 0);
        }
        if (ctx.Config.GetBool("stopOnWin", false) && ctx.State.GetScalar("won", 0) >= 1)
            return Array.Empty<BetDecision>();

        var side = ctx.Config.GetString("side", nameof(SizeResult.Lon));
        var stake = (decimal)ctx.State.GetScalar("stake", (double)GameConstants.TicketPrice);
        if (stake < GameConstants.TicketPrice) stake = GameConstants.TicketPrice;
        return new[] { new BetDecision(BetKind.Size, side, stake) };
    }

    public override void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng)
    {
        if (settledTickets.Count == 0) return;
        var anyWin = settledTickets.Any(t => t.IsWin);
        var cur = (decimal)state.GetScalar("stake", (double)GameConstants.TicketPrice);
        var maxStake = (decimal)config.GetDouble("maxStake", (double)GameConstants.DailyBudget);
        state.SetScalar("stake", (double)(anyWin ? GameConstants.TicketPrice : Math.Min(cur * 2m, maxStake)));
        if (anyWin) state.SetScalar("won", 1);
    }
}

/// <summary>Paroli (anti-martingale): THẮNG thì gấp đôi mức cược (chốt lời theo chuỗi nóng), THUA reset.</summary>
public sealed class ParoliSizeStrategy : StrategyBase
{
    public override string Key => "paroli_size";
    public override bool IsAdaptive => true;

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var side = ctx.Config.GetString("side", nameof(SizeResult.Lon));
        var stake = (decimal)ctx.State.GetScalar("stake", (double)GameConstants.TicketPrice);
        if (ctx.Balance >= GameConstants.DailyBudget) { stake = GameConstants.TicketPrice; ctx.State.SetScalar("stake", (double)stake); }
        if (stake < GameConstants.TicketPrice) stake = GameConstants.TicketPrice;
        return new[] { new BetDecision(BetKind.Size, side, stake) };
    }

    public override void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng)
    {
        if (settledTickets.Count == 0) return;
        var anyWin = settledTickets.Any(t => t.IsWin);
        var cur = (decimal)state.GetScalar("stake", (double)GameConstants.TicketPrice);
        var cap = (decimal)config.GetDouble("maxStake", (double)(GameConstants.TicketPrice * 8));
        var next = anyWin ? Math.Min(cur * 2m, cap) : GameConstants.TicketPrice;
        state.SetScalar("stake", (double)next);
    }
}

/// <summary>Markov: học ma trận chuyển tiếp tổng→tổng, cược tổng kế tiếp khả dĩ nhất.</summary>
public sealed class MarkovSumStrategy : StrategyBase
{
    private const int N = 16; // tổng 3..18
    public override string Key => "markov_sum";
    public override bool IsAdaptive => true;

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var last = (int)ctx.State.GetScalar("last", 0);
        if (last is < 3 or > 18)
            return new[] { new BetDecision(BetKind.Sum, HottestSum(ctx.RecentDraws, 200).ToString()) };

        var m = ctx.State.GetArray("m", N * N, 0);
        int row = (last - 3) * N, bestJ = 7 /*=>10*/, bestC = -1;
        for (int j = 0; j < N; j++)
            if (m[row + j] > bestC) { bestC = (int)m[row + j]; bestJ = j; }
        return new[] { new BetDecision(BetKind.Sum, (bestJ + 3).ToString()) };
    }

    public override void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng)
    {
        var prev = (int)state.GetScalar("last", 0);
        var cur = resultDraw.Sum;
        if (prev is >= 3 and <= 18)
        {
            var m = state.GetArray("m", N * N, 0);
            m[(prev - 3) * N + (cur - 3)] += 1;
            state.SetArray("m", m);
        }
        state.SetScalar("last", cur);
    }
}

/// <summary>EWMA: giữ trọng số mũ cho từng tổng, cược tổng có trọng số cao nhất; tự cập nhật mỗi kỳ.</summary>
public sealed class EwmaAdaptiveStrategy : StrategyBase
{
    private const int N = 16;
    public override string Key => "ewma_adaptive";
    public override bool IsAdaptive => true;

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var w = ctx.State.GetArray("w", N, 1.0 / N);
        int best = 7; double bestW = double.NegativeInfinity;
        for (int i = 0; i < N; i++) if (w[i] > bestW) { bestW = w[i]; best = i; }
        return new[] { new BetDecision(BetKind.Sum, (best + 3).ToString()) };
    }

    public override void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng)
    {
        var alpha = Math.Clamp(config.GetDouble("alpha", 0.05), 0.001, 0.5);
        var w = state.GetArray("w", N, 1.0 / N);
        for (int i = 0; i < N; i++) w[i] *= (1 - alpha);
        w[resultDraw.Sum - 3] += alpha;
        state.SetArray("w", w);
    }
}
