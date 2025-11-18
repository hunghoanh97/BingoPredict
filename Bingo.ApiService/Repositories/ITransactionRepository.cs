using Bingo.ApiService.Models.Entities;

namespace Bingo.ApiService.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> CreateAsync(Transaction transaction);
    Task AddAsync(Transaction transaction);
    Task<IEnumerable<Transaction>> GetByPlayerAsync(Guid playerId, int limit = 50);
    Task<IEnumerable<Transaction>> GetByTypeAsync(Guid playerId, TransactionType type, int limit = 50);
    Task<decimal> GetPlayerBalanceAsync(Guid playerId);
    Task<IEnumerable<Transaction>> GetRecentTransactionsAsync(int limit = 100);
}