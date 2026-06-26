using Bingo.Application.Abstractions;
using Bingo.Application.Persistence;
using Bingo.Application.Simulation;
using Bingo.Domain;
using Bingo.Domain.Entities;
using Bingo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Bingo.Application.Services;

public sealed class SimulationService : ISimulationService
{
    private const int History = 600;

    private readonly IUnitOfWork _uow;
    private readonly IStrategyRegistry _registry;
    private readonly IClock _clock;
    private readonly IPredictionModel _model;
    private readonly ILogger<SimulationService> _logger;

    public SimulationService(
        IUnitOfWork uow,
        IStrategyRegistry registry,
        IClock clock,
        IPredictionModel model,
        ILogger<SimulationService> logger)
    {
        _uow = uow;
        _registry = registry;
        _clock = clock;
        _model = model;
        _logger = logger;
    }

    // ---------- Runtime per-user (dùng cho replay in-memory) ----------
    private sealed class UserRuntime
    {
        public required SimUser User { get; init; }
        public required IBettingStrategy Strategy { get; init; }
        public required StrategyConfig Config { get; init; }
        public required StrategyState State { get; init; }
        public required Random Rng { get; init; }
        public Dictionary<DateOnly, DailyAccount> Accounts { get; } = new();
    }

    private async Task<IReadOnlyDictionary<(BetKind, string), decimal>> GetMultipliersAsync()
    {
        var rules = await _uow.PrizeRules.GetAllAsync();
        return rules.ToDictionary(r => (r.BetKind, r.BetValue), r => r.Multiplier);
    }

    // =====================================================================
    //  REPLAY (backtest điền dữ liệu)
    // =====================================================================
    public async Task<ReplayResult> ReplayAsync(int maxDraws = 2000, CancellationToken ct = default)
    {
        await _uow.ResetBettingDataAsync(ct);

        var allDraws = await _uow.Draws.GetAllAscAsync();
        if (allDraws.Count == 0) return new ReplayResult(0, 0);
        if (maxDraws > 0 && allDraws.Count > maxDraws)
            allDraws = allDraws.Skip(allDraws.Count - maxDraws).ToList();

        var users = await _uow.SimUsers.GetEnabledAsync();
        var strategies = (await _uow.Strategies.GetAllAsync()).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        var multipliers = await GetMultipliersAsync();
        var now = _clock.UtcNow;

        var runtimes = new List<UserRuntime>();
        foreach (var u in users)
        {
            var strat = _registry.Get(u.StrategyKey);
            if (strat is null) continue;
            strategies.TryGetValue(u.StrategyKey, out var stratEntity);
            runtimes.Add(new UserRuntime
            {
                User = u,
                Strategy = strat,
                Config = StrategyConfig.Parse(stratEntity?.DefaultParamsJson, u.ConfigJson),
                State = StrategyState.FromJson(null),
                Rng = new Random(u.Id == 0 ? 1 : u.Id)
            });
        }

        var recent = new List<Draw>(History);
        var allTickets = new List<Ticket>();
        var newAccounts = new List<DailyAccount>();

        foreach (var d in allDraws)
        {
            var gameDate = BingoRules.GameDateOf(d.DrawAt);
            foreach (var rt in runtimes)
            {
                if (!rt.Accounts.TryGetValue(gameDate, out var account))
                {
                    account = NewAccount(rt.User.Id, gameDate);
                    rt.Accounts[gameDate] = account;
                    newAccounts.Add(account);
                }

                if (account.IsBusted || account.CurrentBalance < GameConstants.TicketPrice)
                {
                    account.IsBusted = true;
                    continue;
                }

                var ctx = BuildContext(recent, account, rt.Config, rt.State, rt.Rng, multipliers);
                var decisions = rt.Strategy.DecideBets(ctx);

                var userTickets = new List<Ticket>();
                int betsThisDraw = 0;
                foreach (var dec in decisions)
                {
                    if (betsThisDraw >= GameConstants.MaxBetsPerDraw) break;
                    var stake = NormalizeStake(dec.Stake, account.CurrentBalance);
                    if (stake <= 0m) break;

                    var t = PlaceTicket(rt.User.Id, account, dec, stake, d.DrawAt, now);
                    betsThisDraw++;

                    SettleTicket(t, d, multipliers);
                    account.CurrentBalance += t.Payout;
                    account.TotalPayout += t.Payout;
                    if (t.IsWin) account.Wins++; else account.Losses++;

                    userTickets.Add(t);
                    allTickets.Add(t);
                }

                if (account.CurrentBalance < GameConstants.TicketPrice)
                    account.IsBusted = true;

                if (rt.Strategy.IsAdaptive)
                    rt.Strategy.OnSettled(rt.State, rt.Config, d, userTickets, rt.Rng);
            }

            recent.Insert(0, d);
            if (recent.Count > History) recent.RemoveAt(recent.Count - 1);
        }

        foreach (var a in newAccounts) await _uow.DailyAccounts.AddAsync(a);
        await _uow.Tickets.AddRangeAsync(allTickets);
        await _uow.SaveChangesAsync(ct);

        await PersistStatesAsync(runtimes, now, ct);
        await RecomputeStatsAsync(ct);

        _logger.LogInformation("Replay xong: {Draws} kỳ, {Tickets} vé", allDraws.Count, allTickets.Count);
        return new ReplayResult(allDraws.Count, allTickets.Count);
    }

    // =====================================================================
    //  LIVE TICK
    // =====================================================================
    public async Task<int> RunTickAsync(CancellationToken ct = default)
    {
        var recent = await _uow.Draws.GetRecentAsync(History);
        if (recent.Count == 0) return 0;

        var latest = recent[0];
        var multipliers = await GetMultipliersAsync();
        var users = await _uow.SimUsers.GetEnabledAsync();
        var strategies = (await _uow.Strategies.GetAllAsync()).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        var now = _clock.UtcNow;

        var pending = await _uow.Tickets.GetPendingByTargetAsync(latest.DrawAt);
        var pendingByUser = pending.GroupBy(t => t.SimUserId).ToDictionary(g => g.Key, g => g.ToList());

        var nextTarget = latest.DrawAt.AddMinutes(GameConstants.DrawIntervalMinutes);
        var nextGameDate = BingoRules.GameDateOf(nextTarget);
        var rng = RngFor(latest.Sum, recent.Count);
        int placed = 0;

        foreach (var user in users)
        {
            var strat = _registry.Get(user.StrategyKey);
            if (strat is null) continue;
            strategies.TryGetValue(user.StrategyKey, out var stratEntity);
            var config = StrategyConfig.Parse(stratEntity?.DefaultParamsJson, user.ConfigJson);
            var state = await GetOrCreateStateAsync(user.Id);

            // 1) Settle các vé đã đặt cho kỳ vừa quay (latest).
            if (pendingByUser.TryGetValue(user.Id, out var userPending) && userPending.Count > 0)
            {
                var settledAccount = await _uow.DailyAccounts.GetAsync(user.Id, BingoRules.GameDateOf(latest.DrawAt));
                foreach (var t in userPending)
                {
                    SettleTicket(t, latest, multipliers);
                    if (settledAccount is not null)
                    {
                        settledAccount.CurrentBalance += t.Payout;
                        settledAccount.TotalPayout += t.Payout;
                        if (t.IsWin) settledAccount.Wins++; else settledAccount.Losses++;
                    }
                }
                if (settledAccount is not null && settledAccount.CurrentBalance < GameConstants.TicketPrice)
                    settledAccount.IsBusted = true;

                if (strat.IsAdaptive)
                    strat.OnSettled(state.State, config, latest, userPending, rng);
            }

            // 2) Đặt cược cho kỳ kế tiếp.
            var account = await _uow.DailyAccounts.GetAsync(user.Id, nextGameDate);
            if (account is null)
            {
                account = NewAccount(user.Id, nextGameDate);
                await _uow.DailyAccounts.AddAsync(account);
            }
            if (!account.IsBusted && account.CurrentBalance >= GameConstants.TicketPrice)
            {
                var ctx = BuildContext(recent, account, config, state.State, rng, multipliers);
                var decisions = strat.DecideBets(ctx);

                int betsThisDraw = 0;
                var newTickets = new List<Ticket>();
                foreach (var dec in decisions)
                {
                    if (betsThisDraw >= GameConstants.MaxBetsPerDraw) break;
                    var stake = NormalizeStake(dec.Stake, account.CurrentBalance);
                    if (stake <= 0m) break;
                    newTickets.Add(PlaceTicket(user.Id, account, dec, stake, nextTarget, now));
                    betsThisDraw++;
                }
                if (newTickets.Count > 0)
                {
                    await _uow.Tickets.AddRangeAsync(newTickets);
                    placed += newTickets.Count;
                }
            }
            else if (account.CurrentBalance < GameConstants.TicketPrice)
            {
                account.IsBusted = true;
            }

            // Lưu lại state adaptive (đã có thể đổi qua OnSettled).
            if (strat.IsAdaptive)
            {
                state.Entity.StateJson = state.State.ToJson();
                state.Entity.UpdatedAt = now;
            }
        }

        await _uow.SaveChangesAsync(ct);
        await RecomputeStatsAsync(ct);
        return placed;
    }

    // =====================================================================
    //  STAT RECOMPUTE (dẫn xuất từ DailyAccounts)
    // =====================================================================
    public async Task RecomputeStatsAsync(CancellationToken ct = default)
    {
        var users = await _uow.SimUsers.GetAllAsync();
        var now = _clock.UtcNow;

        foreach (var u in users)
        {
            var accounts = await _uow.DailyAccounts.GetByUserAsync(u.Id);
            var totalStaked = accounts.Sum(a => a.TotalStaked);
            var totalPayout = accounts.Sum(a => a.TotalPayout);
            var totalTickets = accounts.Sum(a => a.TicketsBought);
            var totalWins = accounts.Sum(a => a.Wins);
            var net = totalPayout - totalStaked;

            var stat = await _uow.UserStats.GetAsync(u.Id);
            if (stat is null)
            {
                stat = new UserStat { SimUserId = u.Id };
                await _uow.UserStats.AddAsync(stat);
            }
            stat.TotalTickets = totalTickets;
            stat.TotalWins = totalWins;
            stat.WinRate = totalTickets > 0 ? Math.Round((decimal)totalWins / totalTickets * 100m, 2) : 0m;
            stat.TotalStaked = totalStaked;
            stat.TotalPayout = totalPayout;
            stat.NetProfit = net;
            stat.Roi = totalStaked > 0 ? Math.Round(net / totalStaked * 100m, 2) : 0m;
            stat.DaysPlayed = accounts.Count;
            stat.DaysBusted = accounts.Count(a => a.IsBusted);
            stat.UpdatedAt = now;
        }

        await _uow.SaveChangesAsync(ct);
    }

    // =====================================================================
    //  Helpers
    // =====================================================================
    private static DailyAccount NewAccount(int userId, DateOnly date) => new()
    {
        SimUserId = userId,
        GameDate = date,
        StartingBalance = GameConstants.DailyBudget,
        CurrentBalance = GameConstants.DailyBudget
    };

    private StrategyContext BuildContext(
        IReadOnlyList<Draw> recent, DailyAccount account, StrategyConfig config,
        StrategyState state, Random rng, IReadOnlyDictionary<(BetKind, string), decimal> multipliers)
    {
        return new StrategyContext
        {
            RecentDraws = recent,
            MaxBets = GameConstants.MaxBetsPerDraw,
            Balance = account.CurrentBalance,
            TicketPrice = GameConstants.TicketPrice,
            Config = config,
            State = state,
            Rng = rng,
            PredictionModel = _model,
            Multipliers = multipliers
        };
    }

    /// <summary>Quy mức cược về bội số của đơn vị 10.000, không vượt quá số dư. Trả 0 nếu không đủ tiền.</summary>
    private static decimal NormalizeStake(decimal requested, decimal balance)
    {
        var unit = GameConstants.TicketPrice;
        if (balance < unit) return 0m;
        var s = Math.Floor(requested / unit) * unit;
        if (s < unit) s = unit;
        if (s > balance) s = Math.Floor(balance / unit) * unit;
        return s;
    }

    private static Ticket PlaceTicket(int userId, DailyAccount account, BetDecision dec, decimal stake, DateTime targetDrawAt, DateTime now)
    {
        account.CurrentBalance -= stake;
        account.TotalStaked += stake;
        account.TicketsBought++;
        return new Ticket
        {
            SimUserId = userId,
            DailyAccount = account,
            TargetDrawAt = targetDrawAt,
            BetKind = dec.BetKind,
            BetValue = dec.BetValue,
            Stake = stake,
            PlacedAt = now
        };
    }

    private static void SettleTicket(Ticket t, Draw draw, IReadOnlyDictionary<(BetKind, string), decimal> multipliers)
    {
        var sr = PayoutCalculator.Settle(t.BetKind, t.BetValue, t.Stake, draw, multipliers);
        t.IsSettled = true;
        t.DrawId = draw.Id;
        t.IsWin = sr.IsWin;
        t.Multiplier = sr.Multiplier;
        t.Payout = sr.Payout;
        t.Profit = sr.Payout - t.Stake;
    }

    private static Random RngFor(int userId, int salt) => new(HashCode.Combine(userId, salt));

    private readonly record struct StateHandle(UserStrategyState Entity, StrategyState State);

    private async Task<StateHandle> GetOrCreateStateAsync(int userId)
    {
        var entity = await _uow.StrategyStates.GetAsync(userId);
        if (entity is null)
        {
            entity = new UserStrategyState { SimUserId = userId, StateJson = "{}", UpdatedAt = _clock.UtcNow };
            await _uow.StrategyStates.AddAsync(entity);
        }
        return new StateHandle(entity, StrategyState.FromJson(entity.StateJson));
    }

    private async Task PersistStatesAsync(IEnumerable<UserRuntime> runtimes, DateTime now, CancellationToken ct)
    {
        foreach (var rt in runtimes)
        {
            if (!rt.Strategy.IsAdaptive) continue;
            var entity = await _uow.StrategyStates.GetAsync(rt.User.Id);
            if (entity is null)
            {
                entity = new UserStrategyState { SimUserId = rt.User.Id };
                await _uow.StrategyStates.AddAsync(entity);
            }
            entity.StateJson = rt.State.ToJson();
            entity.UpdatedAt = now;
        }
        await _uow.SaveChangesAsync(ct);
    }
}
