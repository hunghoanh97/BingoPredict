using Bingo.Domain.Entities;

namespace Bingo.Application.Persistence;

public interface IDrawRepository
{
    Task AddRangeAsync(IEnumerable<Draw> draws);
    Task<DateTime?> GetMaxDrawAtAsync();
    Task<IReadOnlyList<Draw>> GetRecentAsync(int count);
    Task<IReadOnlyList<Draw>> GetAllAscAsync();
    Task<Draw?> GetByDrawAtAsync(DateTime drawAtUtc);
    Task<int> CountAsync();
}

public interface IStrategyRepository
{
    Task<IReadOnlyList<Strategy>> GetAllAsync();
    Task<bool> AnyAsync();
    Task AddRangeAsync(IEnumerable<Strategy> strategies);
}

public interface ISimUserRepository
{
    Task<IReadOnlyList<SimUser>> GetEnabledAsync();
    Task<IReadOnlyList<SimUser>> GetAllAsync();
    Task<SimUser?> GetAsync(int id);
    Task<bool> AnyAsync();
    Task AddRangeAsync(IEnumerable<SimUser> users);
}

public interface IDailyAccountRepository
{
    Task<DailyAccount?> GetAsync(int simUserId, DateOnly gameDate);
    Task AddAsync(DailyAccount account);
    Task<IReadOnlyList<DailyAccount>> GetByDateAsync(DateOnly gameDate);
    Task<IReadOnlyList<DailyAccount>> GetByUserAsync(int simUserId);
}

public interface ITicketRepository
{
    Task AddRangeAsync(IEnumerable<Ticket> tickets);
    Task<IReadOnlyList<Ticket>> GetPendingByTargetAsync(DateTime targetDrawAt);
    Task<IReadOnlyList<Ticket>> GetByUserAndDateAsync(int simUserId, DateOnly gameDate);
    Task<IReadOnlyList<Ticket>> GetRecentByUserAsync(int simUserId, int count);
}

public interface IPrizeRuleRepository
{
    Task<IReadOnlyList<PrizeRule>> GetAllAsync();
    Task<bool> AnyAsync();
    Task AddRangeAsync(IEnumerable<PrizeRule> rules);
}

public interface IUserStatRepository
{
    Task<UserStat?> GetAsync(int simUserId);
    Task<IReadOnlyList<UserStat>> GetAllAsync();
    Task AddAsync(UserStat stat);
}

public interface IUserStrategyStateRepository
{
    Task<UserStrategyState?> GetAsync(int simUserId);
    Task AddAsync(UserStrategyState state);
}
