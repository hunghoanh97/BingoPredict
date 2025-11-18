using Bingo.ApiService.Models.Entities;

namespace Bingo.ApiService.Repositories;

public interface IBetRepository
{
    Task<Bet?> GetByIdAsync(Guid id);
    Task<Bet> CreateAsync(Bet bet);
    Task<Bet> UpdateAsync(Bet bet);
    Task AddAsync(Bet bet);
    Task Update(Bet bet);
    Task<IEnumerable<Bet>> GetByPlayerAsync(Guid playerId, int limit = 50);
    Task<IEnumerable<Bet>> GetByGameAsync(Guid gameId);
    Task<IEnumerable<Bet>> GetPendingBetsAsync(Guid gameId);
    Task<decimal> GetTotalBetsForGameAsync(Guid gameId);
    Task<int> GetBetCountForGameAsync(Guid gameId);
    Task<IEnumerable<Bet>> GetPlayerBetsAsync(Guid playerId, int limit = 50);
    Task<IEnumerable<Bet>> GetPendingBetsByGameAsync(Guid gameId);
    Task<decimal> GetTotalBetsAsync();
    Task<decimal> GetTotalPayoutsAsync();
}