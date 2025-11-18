using Bingo.ApiService.Data;
using Bingo.ApiService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bingo.ApiService.Repositories;

public class GameRepository : IGameRepository
{
    private readonly BingoDbContext _context;

    public GameRepository(BingoDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(Guid id)
    {
        return await _context.Games
            .Include(g => g.Bets)
            .Include(g => g.Results)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Game?> GetByGameNumberAsync(string gameNumber)
    {
        return await _context.Games
            .Include(g => g.Bets)
            .Include(g => g.Results)
            .FirstOrDefaultAsync(g => g.GameNumber == gameNumber);
    }

    public async Task<Game> CreateAsync(Game game)
    {
        _context.Games.Add(game);
        await _context.SaveChangesAsync();
        return game;
    }

    public async Task<Game> UpdateAsync(Game game)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync();
        return game;
    }

    public async Task AddAsync(Game game)
    {
        _context.Games.Add(game);
        await _context.SaveChangesAsync();
    }

    public async Task Update(Game game)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Game>> GetScheduledGamesAsync(DateTime? beforeDate = null)
    {
        var query = _context.Games
            .Where(g => g.Status == GameStatus.Scheduled);

        if (beforeDate.HasValue)
        {
            query = query.Where(g => g.DrawTime <= beforeDate.Value);
        }

        return await query
            .OrderBy(g => g.DrawTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetCompletedGamesAsync(int limit = 50)
    {
        return await _context.Games
            .Where(g => g.Status == GameStatus.Completed)
            .OrderByDescending(g => g.DrawTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Game?> GetLatestCompletedGameAsync()
    {
        return await _context.Games
            .Where(g => g.Status == GameStatus.Completed)
            .OrderByDescending(g => g.DrawTime)
            .FirstOrDefaultAsync();
    }

    public async Task<Game?> GetCurrentGameAsync(DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.UtcNow;
        return await _context.Games
            .Where(g => g.Status == GameStatus.Scheduled && g.DrawTime <= now)
            .OrderBy(g => g.DrawTime)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> GameExistsAsync(string gameNumber)
    {
        return await _context.Games.AnyAsync(g => g.GameNumber == gameNumber);
    }

    public async Task<int> GetTotalGamesAsync()
    {
        return await _context.Games.CountAsync();
    }

    public async Task<int> GetCompletedGamesAsync()
    {
        return await _context.Games.CountAsync(g => g.Status == GameStatus.Completed);
    }

    public async Task<IEnumerable<Game>> GetCompletedGamesSinceAsync(DateTime sinceDate)
    {
        return await _context.Games
            .Where(g => g.Status == GameStatus.Completed && g.CompletedAt >= sinceDate)
            .OrderByDescending(g => g.CompletedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetRecentGamesAsync(int limit = 10)
    {
        return await _context.Games
            .Where(g => g.Status == GameStatus.Completed)
            .OrderByDescending(g => g.CompletedAt)
            .Take(limit)
            .ToListAsync();
    }
}