using Bingo.Application.Abstractions;
using Bingo.Application.Dtos;
using Bingo.Application.Persistence;
using Bingo.Domain;
using Bingo.Domain.Entities;
using System.Text.Json;

namespace Bingo.Application.Services;

public sealed class LeaderboardService : ILeaderboardService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public LeaderboardService(IUnitOfWork uow, IClock clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(string metric)
    {
        var users = await _uow.SimUsers.GetAllAsync();
        var stats = (await _uow.UserStats.GetAllAsync()).ToDictionary(s => s.SimUserId);
        var strategies = (await _uow.Strategies.GetAllAsync()).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        var today = BingoRules.GameDateOf(_clock.UtcNow);
        var todayAccounts = (await _uow.DailyAccounts.GetByDateAsync(today)).ToDictionary(a => a.SimUserId);

        var list = new List<LeaderboardEntryDto>();
        foreach (var u in users)
        {
            stats.TryGetValue(u.Id, out var st);
            strategies.TryGetValue(u.StrategyKey, out var strat);
            todayAccounts.TryGetValue(u.Id, out var acc);
            list.Add(new LeaderboardEntryDto(
                u.Id, u.Name, u.StrategyKey, strat?.Name ?? u.StrategyKey,
                st?.TotalTickets ?? 0, st?.TotalWins ?? 0, st?.WinRate ?? 0,
                st?.TotalStaked ?? 0, st?.TotalPayout ?? 0, st?.NetProfit ?? 0, st?.Roi ?? 0,
                st?.DaysPlayed ?? 0, st?.DaysBusted ?? 0,
                acc?.CurrentBalance, acc?.IsBusted ?? false));
        }

        list = metric?.ToLowerInvariant() switch
        {
            "roi" => list.OrderByDescending(e => e.Roi).ToList(),
            _ => list.OrderByDescending(e => e.WinRate).ToList()
        };
        return list;
    }

    public async Task<IReadOnlyList<SimUserDto>> GetUsersAsync()
    {
        var users = await _uow.SimUsers.GetAllAsync();
        var stats = (await _uow.UserStats.GetAllAsync()).ToDictionary(s => s.SimUserId);
        var strategies = (await _uow.Strategies.GetAllAsync()).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

        return users.Select(u =>
        {
            stats.TryGetValue(u.Id, out var st);
            strategies.TryGetValue(u.StrategyKey, out var strat);
            return new SimUserDto(u.Id, u.Name, u.StrategyKey, strat?.Name ?? u.StrategyKey, u.Enabled, ToStatDto(st));
        }).ToList();
    }

    public async Task<UserDetailDto?> GetUserDetailAsync(int id)
    {
        var user = await _uow.SimUsers.GetAsync(id);
        if (user is null) return null;
        var st = await _uow.UserStats.GetAsync(id);
        var strategies = (await _uow.Strategies.GetAllAsync()).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        strategies.TryGetValue(user.StrategyKey, out var strat);
        var accounts = (await _uow.DailyAccounts.GetByUserAsync(id))
            .OrderBy(a => a.GameDate)
            .Select(ToDailyDto)
            .ToList();

        object? config = null;
        if (!string.IsNullOrWhiteSpace(user.ConfigJson))
        {
            try { config = JsonSerializer.Deserialize<object>(user.ConfigJson); } catch (JsonException) { }
        }

        return new UserDetailDto(
            user.Id, user.Name, user.StrategyKey, strat?.Name ?? user.StrategyKey,
            strat?.Description ?? string.Empty, config, ToStatDto(st), accounts);
    }

    public async Task<DailySummaryDto> GetDailyAsync(DateOnly date)
    {
        var accounts = await _uow.DailyAccounts.GetByDateAsync(date);
        var users = (await _uow.SimUsers.GetAllAsync()).ToDictionary(u => u.Id);

        var perUser = accounts.Select(a =>
        {
            users.TryGetValue(a.SimUserId, out var u);
            return new DailyAccountWithUserDto(
                a.SimUserId, u?.Name ?? $"#{a.SimUserId}", u?.StrategyKey ?? string.Empty,
                a.GameDate, a.StartingBalance, a.CurrentBalance, a.TicketsBought,
                a.TotalStaked, a.TotalPayout, a.Wins, a.Losses,
                a.NetProfit, Roi(a), a.IsBusted);
        }).OrderByDescending(x => x.NetProfit).ToList();

        var totals = new DailyTotalsDto(
            accounts.Sum(a => a.TotalStaked),
            accounts.Sum(a => a.TotalPayout),
            accounts.Sum(a => a.NetProfit),
            accounts.Count(a => a.IsBusted));

        return new DailySummaryDto(date, perUser, totals);
    }

    public async Task<IReadOnlyList<DrawDto>> GetLatestDrawsAsync(int count)
    {
        var draws = await _uow.Draws.GetRecentAsync(count);
        return draws.Select(d => new DrawDto(
            d.Id, d.DrawAt, d.WinningResult, d.D1, d.D2, d.D3, d.Sum, d.Size.ToString(), d.IsTriple)).ToList();
    }

    public async Task<IReadOnlyList<TicketDto>> GetTicketsAsync(int? userId, DateOnly? date)
    {
        if (userId is null) return Array.Empty<TicketDto>();
        var user = await _uow.SimUsers.GetAsync(userId.Value);
        var name = user?.Name ?? $"#{userId}";

        var tickets = date is not null
            ? await _uow.Tickets.GetByUserAndDateAsync(userId.Value, date.Value)
            : await _uow.Tickets.GetRecentByUserAsync(userId.Value, 200);

        return tickets.Select(t => new TicketDto(
            t.Id, t.SimUserId, name, t.TargetDrawAt, t.DrawId, t.BetKind.ToString(), t.BetValue,
            t.Stake, t.Multiplier, t.IsSettled, t.IsWin, t.Payout, t.Profit)).ToList();
    }

    public async Task<IReadOnlyList<StrategyDto>> GetStrategiesAsync()
    {
        var strategies = await _uow.Strategies.GetAllAsync();
        return strategies.Select(s => new StrategyDto(s.Key, s.Name, s.Description, s.IsAdaptive, s.Enabled)).ToList();
    }

    // ---- mappers ----
    private static decimal Roi(DailyAccount a) =>
        a.TotalStaked > 0 ? Math.Round(a.NetProfit / a.TotalStaked * 100m, 2) : 0m;

    private static DailyAccountDto ToDailyDto(DailyAccount a) => new(
        a.GameDate, a.StartingBalance, a.CurrentBalance, a.TicketsBought,
        a.TotalStaked, a.TotalPayout, a.Wins, a.Losses, a.NetProfit, Roi(a), a.IsBusted);

    private static UserStatDto ToStatDto(UserStat? st) => new(
        st?.TotalTickets ?? 0, st?.TotalWins ?? 0, st?.WinRate ?? 0,
        st?.TotalStaked ?? 0, st?.TotalPayout ?? 0, st?.NetProfit ?? 0, st?.Roi ?? 0,
        st?.DaysPlayed ?? 0, st?.DaysBusted ?? 0);
}
