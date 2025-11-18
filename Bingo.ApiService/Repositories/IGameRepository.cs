using Bingo.ApiService.Models.Entities;

namespace Bingo.ApiService.Repositories;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id);
    Task<Game?> GetByGameNumberAsync(string gameNumber);
    Task<Game> CreateAsync(Game game);
    Task<Game> UpdateAsync(Game game);
    Task AddAsync(Game game);
    Task Update(Game game);
    Task<IEnumerable<Game>> GetScheduledGamesAsync(DateTime? beforeDate = null);
    Task<IEnumerable<Game>> GetCompletedGamesAsync(int limit = 50);
    Task<Game?> GetLatestCompletedGameAsync();
    Task<Game?> GetCurrentGameAsync(DateTime? currentTime = null);
    Task<bool> GameExistsAsync(string gameNumber);
    Task<int> GetTotalGamesAsync();
    Task<int> GetCompletedGamesAsync();
    Task<IEnumerable<Game>> GetCompletedGamesSinceAsync(DateTime sinceDate);
    Task<IEnumerable<Game>> GetRecentGamesAsync(int limit = 10);
}