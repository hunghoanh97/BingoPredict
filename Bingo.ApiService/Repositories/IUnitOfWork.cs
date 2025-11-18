namespace Bingo.ApiService.Repositories;

public interface IUnitOfWork : IDisposable
{
    IPlayerRepository Players { get; }
    IGameRepository Games { get; }
    IBetRepository Bets { get; }
    ITransactionRepository Transactions { get; }
    IStatisticsRepository Statistics { get; }
    IGameResultRepository GameResults { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}