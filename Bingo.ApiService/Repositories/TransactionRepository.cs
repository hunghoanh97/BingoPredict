using Bingo.ApiService.Data;
using Bingo.ApiService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bingo.ApiService.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly BingoDbContext _context;

    public TransactionRepository(BingoDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task AddAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByPlayerAsync(Guid playerId, int limit = 50)
    {
        return await _context.Transactions
            .Include(t => t.Player)
            .Where(t => t.PlayerId == playerId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByTypeAsync(Guid playerId, TransactionType type, int limit = 50)
    {
        return await _context.Transactions
            .Include(t => t.Player)
            .Where(t => t.PlayerId == playerId && t.Type == type)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<decimal> GetPlayerBalanceAsync(Guid playerId)
    {
        var latestTransaction = await _context.Transactions
            .Where(t => t.PlayerId == playerId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        return latestTransaction?.BalanceAfter ?? 0;
    }

    public async Task<IEnumerable<Transaction>> GetRecentTransactionsAsync(int limit = 100)
    {
        return await _context.Transactions
            .Include(t => t.Player)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}