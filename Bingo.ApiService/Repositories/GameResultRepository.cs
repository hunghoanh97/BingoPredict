using Bingo.ApiService.Data;
using Bingo.ApiService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bingo.ApiService.Repositories;

public class GameResultRepository : IGameResultRepository
{
    private readonly BingoDbContext _context;

    public GameResultRepository(BingoDbContext context)
    {
        _context = context;
    }

    public async Task<GameResult?> GetByIdAsync(Guid id)
    {
        return await _context.GameResults
            .Include(gr => gr.Game)
            .FirstOrDefaultAsync(gr => gr.Id == id);
    }

    public async Task<GameResult> CreateAsync(GameResult gameResult)
    {
        _context.GameResults.Add(gameResult);
        await _context.SaveChangesAsync();
        return gameResult;
    }

    public async Task AddAsync(GameResult gameResult)
    {
        _context.GameResults.Add(gameResult);
        await _context.SaveChangesAsync();
    }

    public async Task<GameResult> UpdateAsync(GameResult gameResult)
    {
        _context.GameResults.Update(gameResult);
        await _context.SaveChangesAsync();
        return gameResult;
    }

    public async Task<IEnumerable<GameResult>> GetByGameAsync(Guid gameId)
    {
        return await _context.GameResults
            .Include(gr => gr.Game)
            .Where(gr => gr.GameId == gameId)
            .OrderByDescending(gr => gr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<GameResult>> GetRecentResultsAsync(int limit = 50)
    {
        return await _context.GameResults
            .Include(gr => gr.Game)
            .OrderByDescending(gr => gr.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var gameResult = await _context.GameResults.FindAsync(id);
        if (gameResult == null) return false;

        _context.GameResults.Remove(gameResult);
        await _context.SaveChangesAsync();
        return true;
    }
}