using Bingo.Application.Abstractions;
using Bingo.Domain;
using Bingo.Domain.Entities;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation;

public readonly record struct BacktestMetrics(int Tickets, int Wins, decimal Staked, decimal Payout)
{
    public decimal WinRate => Tickets > 0 ? Math.Round((decimal)Wins / Tickets * 100m, 2) : 0m;
    public decimal NetProfit => Payout - Staked;
    public decimal Roi => Staked > 0 ? Math.Round(NetProfit / Staked * 100m, 2) : 0m;
}

/// <summary>
/// Chạy mô phỏng một chiến lược trên dữ liệu draws (chỉ tính metric, KHÔNG ghi DB).
/// Dùng cho tuner để so sánh nhiều bộ tham số.
/// </summary>
public static class BacktestRunner
{
    private const int History = 600;

    public static BacktestMetrics Run(
        IBettingStrategy strategy,
        StrategyConfig config,
        IReadOnlyList<Draw> drawsAsc,
        IReadOnlyDictionary<(BetKind, string), decimal> multipliers,
        IPredictionModel? model,
        int seed)
    {
        var state = StrategyState.FromJson(null);
        var rng = new Random(seed);
        var recent = new List<Draw>(History);
        var balByDate = new Dictionary<DateOnly, decimal>();
        var bustedByDate = new HashSet<DateOnly>();

        int tickets = 0, wins = 0;
        decimal staked = 0m, payout = 0m;

        foreach (var d in drawsAsc)
        {
            var gd = BingoRules.GameDateOf(d.DrawAt);
            if (!balByDate.TryGetValue(gd, out var bal)) bal = GameConstants.DailyBudget;

            if (!bustedByDate.Contains(gd) && bal >= GameConstants.TicketPrice)
            {
                var ctx = new StrategyContext
                {
                    RecentDraws = recent,
                    MaxBets = GameConstants.MaxBetsPerDraw,
                    Balance = bal,
                    TicketPrice = GameConstants.TicketPrice,
                    Config = config,
                    State = state,
                    Rng = rng,
                    PredictionModel = model,
                    Multipliers = multipliers
                };

                var decisions = strategy.DecideBets(ctx);
                var userTickets = new List<Ticket>();
                int betsThisDraw = 0;
                foreach (var dec in decisions)
                {
                    if (betsThisDraw >= GameConstants.MaxBetsPerDraw) break;
                    var stake = NormalizeStake(dec.Stake, bal);
                    if (stake <= 0m) break;

                    bal -= stake;
                    staked += stake;
                    betsThisDraw++;
                    tickets++;

                    var sr = PayoutCalculator.Settle(dec.BetKind, dec.BetValue, stake, d, multipliers);
                    bal += sr.Payout;
                    payout += sr.Payout;
                    if (sr.IsWin) wins++;
                    userTickets.Add(new Ticket { BetKind = dec.BetKind, BetValue = dec.BetValue, Stake = stake, IsWin = sr.IsWin });
                }

                if (bal < GameConstants.TicketPrice) bustedByDate.Add(gd);
                if (strategy.IsAdaptive) strategy.OnSettled(state, config, d, userTickets, rng);
            }

            balByDate[gd] = bal;

            recent.Insert(0, d);
            if (recent.Count > History) recent.RemoveAt(recent.Count - 1);
        }

        return new BacktestMetrics(tickets, wins, staked, payout);
    }

    private static decimal NormalizeStake(decimal requested, decimal balance)
    {
        var unit = GameConstants.TicketPrice;
        if (balance < unit) return 0m;
        var s = Math.Floor(requested / unit) * unit;
        if (s < unit) s = unit;
        if (s > balance) s = Math.Floor(balance / unit) * unit;
        return s;
    }
}
