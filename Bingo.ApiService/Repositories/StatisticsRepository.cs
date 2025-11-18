using Bingo.ApiService.Data;
using Bingo.ApiService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bingo.ApiService.Repositories;

public class StatisticsRepository : IStatisticsRepository
{
    private readonly BingoDbContext _context;

    public StatisticsRepository(BingoDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerStatistics?> GetByPlayerAndTypeAsync(Guid playerId, StatisticType type)
    {
        return await _context.PlayerStatistics
            .Include(s => s.Player)
            .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.StatisticType == type);
    }

    public async Task<PlayerStatistics> CreateAsync(PlayerStatistics statistics)
    {
        _context.PlayerStatistics.Add(statistics);
        await _context.SaveChangesAsync();
        return statistics;
    }

    public async Task<PlayerStatistics> UpdateAsync(PlayerStatistics statistics)
    {
        _context.PlayerStatistics.Update(statistics);
        await _context.SaveChangesAsync();
        return statistics;
    }

    public async Task Update(PlayerStatistics statistics)
    {
        _context.PlayerStatistics.Update(statistics);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<PlayerStatistics>> GetByPlayerAsync(Guid playerId)
    {
        return await _context.PlayerStatistics
            .Include(s => s.Player)
            .Where(s => s.PlayerId == playerId)
            .ToListAsync();
    }

    public async Task<IEnumerable<PlayerStatistics>> GetTopPerformersAsync(StatisticType type, int limit = 10)
    {
        return await _context.PlayerStatistics
            .Include(s => s.Player)
            .Where(s => s.StatisticType == type)
            .OrderByDescending(s => s.WinRate ?? 0)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var statistics = await _context.PlayerStatistics.FindAsync(id);
        if (statistics == null) return false;

        _context.PlayerStatistics.Remove(statistics);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PlayerStatistics?> GetPlayerStatisticsAsync(Guid playerId)
    {
        return await _context.PlayerStatistics
            .Include(s => s.Player)
            .FirstOrDefaultAsync(s => s.PlayerId == playerId);
    }

    public async Task AddAsync(PlayerStatistics statistics)
    {
        _context.PlayerStatistics.Add(statistics);
        await _context.SaveChangesAsync();
    }
}