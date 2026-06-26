using Bingo.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bingo.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BingoDbContext _ctx;

    public UnitOfWork(BingoDbContext ctx)
    {
        _ctx = ctx;
        Draws = new DrawRepository(ctx);
        Strategies = new StrategyRepository(ctx);
        SimUsers = new SimUserRepository(ctx);
        DailyAccounts = new DailyAccountRepository(ctx);
        Tickets = new TicketRepository(ctx);
        PrizeRules = new PrizeRuleRepository(ctx);
        UserStats = new UserStatRepository(ctx);
        StrategyStates = new UserStrategyStateRepository(ctx);
    }

    public IDrawRepository Draws { get; }
    public IStrategyRepository Strategies { get; }
    public ISimUserRepository SimUsers { get; }
    public IDailyAccountRepository DailyAccounts { get; }
    public ITicketRepository Tickets { get; }
    public IPrizeRuleRepository PrizeRules { get; }
    public IUserStatRepository UserStats { get; }
    public IUserStrategyStateRepository StrategyStates { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _ctx.SaveChangesAsync(ct);

    public async Task ResetBettingDataAsync(CancellationToken ct = default)
    {
        // Xóa theo thứ tự phụ thuộc FK: tickets -> daily accounts -> stats/states. Giữ draws, users, strategies, prize rules.
        await _ctx.Tickets.ExecuteDeleteAsync(ct);
        await _ctx.DailyAccounts.ExecuteDeleteAsync(ct);
        await _ctx.UserStats.ExecuteDeleteAsync(ct);
        await _ctx.StrategyStates.ExecuteDeleteAsync(ct);
        _ctx.ChangeTracker.Clear();
    }
}
