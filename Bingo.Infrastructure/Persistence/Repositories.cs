using Bingo.Application.Persistence;
using Bingo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bingo.Infrastructure.Persistence;

public sealed class DrawRepository : IDrawRepository
{
    private readonly BingoDbContext _ctx;
    public DrawRepository(BingoDbContext ctx) => _ctx = ctx;

    public Task AddRangeAsync(IEnumerable<Draw> draws) => _ctx.Draws.AddRangeAsync(draws);
    public async Task<DateTime?> GetMaxDrawAtAsync() =>
        await _ctx.Draws.MaxAsync(x => (DateTime?)x.DrawAt);
    public async Task<IReadOnlyList<Draw>> GetRecentAsync(int count) =>
        await _ctx.Draws.AsNoTracking().OrderByDescending(x => x.DrawAt).Take(count).ToListAsync();
    public async Task<IReadOnlyList<Draw>> GetAllAscAsync() =>
        await _ctx.Draws.AsNoTracking().OrderBy(x => x.DrawAt).ToListAsync();
    public async Task<Draw?> GetByDrawAtAsync(DateTime drawAtUtc) =>
        await _ctx.Draws.FirstOrDefaultAsync(x => x.DrawAt == drawAtUtc);
    public async Task<int> CountAsync() => await _ctx.Draws.CountAsync();
}

public sealed class StrategyRepository : IStrategyRepository
{
    private readonly BingoDbContext _ctx;
    public StrategyRepository(BingoDbContext ctx) => _ctx = ctx;
    public async Task<IReadOnlyList<Strategy>> GetAllAsync() => await _ctx.Strategies.AsNoTracking().ToListAsync();
    public Task<bool> AnyAsync() => _ctx.Strategies.AnyAsync();
    public Task AddRangeAsync(IEnumerable<Strategy> strategies) => _ctx.Strategies.AddRangeAsync(strategies);
}

public sealed class SimUserRepository : ISimUserRepository
{
    private readonly BingoDbContext _ctx;
    public SimUserRepository(BingoDbContext ctx) => _ctx = ctx;
    public async Task<IReadOnlyList<SimUser>> GetEnabledAsync() =>
        await _ctx.SimUsers.Where(u => u.Enabled).OrderBy(u => u.Id).ToListAsync();
    public async Task<IReadOnlyList<SimUser>> GetAllAsync() =>
        await _ctx.SimUsers.OrderBy(u => u.Id).ToListAsync();
    public Task<SimUser?> GetAsync(int id) => _ctx.SimUsers.FirstOrDefaultAsync(u => u.Id == id);
    public Task<bool> AnyAsync() => _ctx.SimUsers.AnyAsync();
    public Task AddRangeAsync(IEnumerable<SimUser> users) => _ctx.SimUsers.AddRangeAsync(users);
}

public sealed class DailyAccountRepository : IDailyAccountRepository
{
    private readonly BingoDbContext _ctx;
    public DailyAccountRepository(BingoDbContext ctx) => _ctx = ctx;

    public async Task<DailyAccount?> GetAsync(int simUserId, DateOnly gameDate)
    {
        // Ưu tiên entity đang theo dõi trong context (chưa lưu) để tránh tạo trùng.
        var local = _ctx.DailyAccounts.Local
            .FirstOrDefault(a => a.SimUserId == simUserId && a.GameDate == gameDate);
        if (local is not null) return local;
        return await _ctx.DailyAccounts.FirstOrDefaultAsync(a => a.SimUserId == simUserId && a.GameDate == gameDate);
    }

    public Task AddAsync(DailyAccount account) => _ctx.DailyAccounts.AddAsync(account).AsTask();
    public async Task<IReadOnlyList<DailyAccount>> GetByDateAsync(DateOnly gameDate) =>
        await _ctx.DailyAccounts.AsNoTracking().Where(a => a.GameDate == gameDate).ToListAsync();
    public async Task<IReadOnlyList<DailyAccount>> GetByUserAsync(int simUserId) =>
        await _ctx.DailyAccounts.Where(a => a.SimUserId == simUserId).OrderBy(a => a.GameDate).ToListAsync();
}

public sealed class TicketRepository : ITicketRepository
{
    private readonly BingoDbContext _ctx;
    public TicketRepository(BingoDbContext ctx) => _ctx = ctx;

    public Task AddRangeAsync(IEnumerable<Ticket> tickets) => _ctx.Tickets.AddRangeAsync(tickets);
    public async Task<IReadOnlyList<Ticket>> GetPendingByTargetAsync(DateTime targetDrawAt) =>
        await _ctx.Tickets.Where(t => !t.IsSettled && t.TargetDrawAt == targetDrawAt).ToListAsync();
    public async Task<IReadOnlyList<Ticket>> GetByUserAndDateAsync(int simUserId, DateOnly gameDate) =>
        await _ctx.Tickets.AsNoTracking()
            .Where(t => t.SimUserId == simUserId && t.DailyAccount!.GameDate == gameDate)
            .OrderByDescending(t => t.TargetDrawAt).ToListAsync();
    public async Task<IReadOnlyList<Ticket>> GetRecentByUserAsync(int simUserId, int count) =>
        await _ctx.Tickets.AsNoTracking()
            .Where(t => t.SimUserId == simUserId)
            .OrderByDescending(t => t.TargetDrawAt).Take(count).ToListAsync();
}

public sealed class PrizeRuleRepository : IPrizeRuleRepository
{
    private readonly BingoDbContext _ctx;
    public PrizeRuleRepository(BingoDbContext ctx) => _ctx = ctx;
    public async Task<IReadOnlyList<PrizeRule>> GetAllAsync() => await _ctx.PrizeRules.AsNoTracking().ToListAsync();
    public Task<bool> AnyAsync() => _ctx.PrizeRules.AnyAsync();
    public Task AddRangeAsync(IEnumerable<PrizeRule> rules) => _ctx.PrizeRules.AddRangeAsync(rules);
}

public sealed class UserStatRepository : IUserStatRepository
{
    private readonly BingoDbContext _ctx;
    public UserStatRepository(BingoDbContext ctx) => _ctx = ctx;
    public async Task<UserStat?> GetAsync(int simUserId) =>
        await _ctx.UserStats.FindAsync(simUserId);
    public async Task<IReadOnlyList<UserStat>> GetAllAsync() => await _ctx.UserStats.AsNoTracking().ToListAsync();
    public Task AddAsync(UserStat stat) => _ctx.UserStats.AddAsync(stat).AsTask();
}

public sealed class UserStrategyStateRepository : IUserStrategyStateRepository
{
    private readonly BingoDbContext _ctx;
    public UserStrategyStateRepository(BingoDbContext ctx) => _ctx = ctx;
    public async Task<UserStrategyState?> GetAsync(int simUserId) =>
        await _ctx.StrategyStates.FindAsync(simUserId);
    public Task AddAsync(UserStrategyState state) => _ctx.StrategyStates.AddAsync(state).AsTask();
}
