namespace Bingo.Application.Persistence;

/// <summary>
/// Unit of Work: gom các repository và quản lý lưu thay đổi/giao dịch.
/// </summary>
public interface IUnitOfWork
{
    IDrawRepository Draws { get; }
    IStrategyRepository Strategies { get; }
    ISimUserRepository SimUsers { get; }
    IDailyAccountRepository DailyAccounts { get; }
    ITicketRepository Tickets { get; }
    IPrizeRuleRepository PrizeRules { get; }
    IUserStatRepository UserStats { get; }
    IUserStrategyStateRepository StrategyStates { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Xóa toàn bộ dữ liệu cá cược (tickets, daily accounts, user stats, strategy states).
    /// Giữ lại draws, users, strategies, prize rules.</summary>
    Task ResetBettingDataAsync(CancellationToken ct = default);
}
