using Bingo.Domain;
using Bingo.Domain.Entities;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation.Strategies;

/// <summary>Tiến trình Fibonacci trên Tài/Xỉu: thua tiến 1 bước Fibonacci, thắng lùi 2 bước. Reset mỗi ngày.</summary>
public sealed class FibonacciStrategy : StrategyBase
{
    public override string Key => "fibonacci";
    public override bool IsAdaptive => true;

    private static long Fib(int i)
    {
        long a = 1, b = 1;
        for (int k = 0; k < i; k++) { (a, b) = (b, a + b); }
        return a;
    }

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        if (ctx.Balance >= GameConstants.DailyBudget) ctx.State.SetScalar("idx", 0);
        var idx = (int)ctx.State.GetScalar("idx", 0);
        var stake = GameConstants.TicketPrice * Fib(idx);
        var side = ctx.Config.GetString("side", nameof(SizeResult.Lon));
        return new[] { new BetDecision(BetKind.Size, side, stake) };
    }

    public override void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng)
    {
        if (settledTickets.Count == 0) return;
        var idx = (int)state.GetScalar("idx", 0);
        idx = settledTickets.Any(t => t.IsWin) ? Math.Max(0, idx - 2) : Math.Min(idx + 1, 18);
        state.SetScalar("idx", idx);
    }
}

/// <summary>D'Alembert trên Tài/Xỉu: thua +1 đơn vị, thắng −1 đơn vị (biến động thấp). Reset mỗi ngày.</summary>
public sealed class DAlembertStrategy : StrategyBase
{
    public override string Key => "dalembert";
    public override bool IsAdaptive => true;

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        if (ctx.Balance >= GameConstants.DailyBudget) ctx.State.SetScalar("units", 1);
        var units = Math.Max(1, (int)ctx.State.GetScalar("units", 1));
        var side = ctx.Config.GetString("side", nameof(SizeResult.Lon));
        return new[] { new BetDecision(BetKind.Size, side, GameConstants.TicketPrice * units) };
    }

    public override void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng)
    {
        if (settledTickets.Count == 0) return;
        var units = Math.Max(1, (int)state.GetScalar("units", 1));
        units = settledTickets.Any(t => t.IsWin) ? Math.Max(1, units - 1) : Math.Min(units + 1, 100);
        state.SetScalar("units", units);
    }
}

/// <summary>Hệ 1-3-2-6 (tiến trình thuận khi thắng) trên Tài/Xỉu; thua hoặc xong chu kỳ thì reset.</summary>
public sealed class System1326Strategy : StrategyBase
{
    public override string Key => "system_1326";
    public override bool IsAdaptive => true;
    private static readonly int[] Seq = { 1, 3, 2, 6 };

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        if (ctx.Balance >= GameConstants.DailyBudget) ctx.State.SetScalar("step", 0);
        var step = (int)ctx.State.GetScalar("step", 0) % 4;
        var side = ctx.Config.GetString("side", nameof(SizeResult.Lon));
        return new[] { new BetDecision(BetKind.Size, side, GameConstants.TicketPrice * Seq[step]) };
    }

    public override void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng)
    {
        if (settledTickets.Count == 0) return;
        var step = (int)state.GetScalar("step", 0) % 4;
        step = settledTickets.Any(t => t.IsWin) ? (step + 1) % 4 : 0;
        state.SetScalar("step", step);
    }
}

/// <summary>Labouchère (cancellation) trên Tài/Xỉu: cược = đầu+cuối chuỗi; thắng xóa 2 đầu, thua nối thêm. Reset mỗi ngày/chu kỳ.</summary>
public sealed class LabouchereStrategy : StrategyBase
{
    public override string Key => "labouchere";
    public override bool IsAdaptive => true;
    private static double[] Initial => new double[] { 1, 1, 1, 1 };

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        if (ctx.Balance >= GameConstants.DailyBudget) ctx.State.SetArray("seq", Initial);
        var seq = ctx.State.GetArrayOrNull("seq");
        if (seq is null || seq.Length == 0) { seq = Initial; ctx.State.SetArray("seq", seq); }

        var units = seq.Length >= 2 ? seq[0] + seq[^1] : seq[0];
        var side = ctx.Config.GetString("side", nameof(SizeResult.Lon));
        return new[] { new BetDecision(BetKind.Size, side, GameConstants.TicketPrice * (decimal)Math.Max(1, units)) };
    }

    public override void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng)
    {
        if (settledTickets.Count == 0) return;
        var seq = (state.GetArrayOrNull("seq") ?? Initial).ToList();
        if (seq.Count == 0) seq = Initial.ToList();

        var units = seq.Count >= 2 ? seq[0] + seq[^1] : seq[0];
        if (settledTickets.Any(t => t.IsWin))
        {
            if (seq.Count >= 2) { seq.RemoveAt(seq.Count - 1); seq.RemoveAt(0); }
            else seq.RemoveAt(0);
        }
        else
        {
            seq.Add(units);
        }

        if (seq.Count == 0 || seq.Count > 12) seq = Initial.ToList(); // xong chu kỳ hoặc quá dài → reset
        state.SetArray("seq", seq.ToArray());
    }
}
