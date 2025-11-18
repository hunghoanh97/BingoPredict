using Bingo.ApiService.Models.Entities;

namespace Bingo.ApiService.Repositories;

public interface IGameResultRepository
{
    Task<GameResult?> GetByIdAsync(Guid id);
    Task<GameResult> CreateAsync(GameResult gameResult);
    Task<GameResult> UpdateAsync(GameResult gameResult);
    Task AddAsync(GameResult gameResult);
    Task<IEnumerable<GameResult>> GetByGameAsync(Guid gameId);
    Task<IEnumerable<GameResult>> GetRecentResultsAsync(int limit = 50);
    Task<bool> DeleteAsync(Guid id);
}