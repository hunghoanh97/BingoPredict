using Bingo.ApiService.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bingo.ApiService.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly BingoDbContext _context;
    private IDbContextTransaction? _transaction;

    public IPlayerRepository Players { get; }
    public IGameRepository Games { get; }
    public IBetRepository Bets { get; }
    public ITransactionRepository Transactions { get; }
    public IStatisticsRepository Statistics { get; }
    public IGameResultRepository GameResults { get; }

    public UnitOfWork(BingoDbContext context)
    {
        _context = context;
        Players = new PlayerRepository(_context);
        Games = new GameRepository(_context);
        Bets = new BetRepository(_context);
        Transactions = new TransactionRepository(_context);
        Statistics = new StatisticsRepository(_context);
        GameResults = new GameResultRepository(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}