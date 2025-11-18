using Bingo.ApiService.Models.Entities;

namespace Bingo.ApiService.Repositories;

public interface IStatisticsRepository
{
    Task<PlayerStatistics?> GetByPlayerAndTypeAsync(Guid playerId, StatisticType type);
    Task<PlayerStatistics> CreateAsync(PlayerStatistics statistics);
    Task<PlayerStatistics> UpdateAsync(PlayerStatistics statistics);
    Task Update(PlayerStatistics statistics);
    Task<IEnumerable<PlayerStatistics>> GetByPlayerAsync(Guid playerId);
    Task<IEnumerable<PlayerStatistics>> GetTopPerformersAsync(StatisticType type, int limit = 10);
    Task<bool> DeleteAsync(Guid id);
    Task<PlayerStatistics?> GetPlayerStatisticsAsync(Guid playerId);
    Task AddAsync(PlayerStatistics statistics);
}