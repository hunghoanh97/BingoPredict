using Bingo.ApiService.Data;
using Bingo.ApiService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bingo.ApiService.Repositories;

public class BetRepository : IBetRepository
{
    private readonly BingoDbContext _context;

    public BetRepository(BingoDbContext context)
    {
        _context = context;
    }

    public async Task<Bet?> GetByIdAsync(Guid id)
    {
        return await _context.Bets
            .Include(b => b.Player)
            .Include(b => b.Game)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Bet> CreateAsync(Bet bet)
    {
        _context.Bets.Add(bet);
        await _context.SaveChangesAsync();
        return bet;
    }

    public async Task AddAsync(Bet bet)
    {
        _context.Bets.Add(bet);
        await _context.SaveChangesAsync();
    }

    public async Task<Bet> UpdateAsync(Bet bet)
    {
        _context.Bets.Update(bet);
        await _context.SaveChangesAsync();
        return bet;
    }

    public async Task Update(Bet bet)
    {
        _context.Bets.Update(bet);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Bet>> GetByPlayerAsync(Guid playerId, int limit = 50)
    {
        return await _context.Bets
            .Include(b => b.Game)
            .Where(b => b.PlayerId == playerId)
            .OrderByDescending(b => b.PlacedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bet>> GetByGameAsync(Guid gameId)
    {
        return await _context.Bets
            .Include(b => b.Player)
            .Where(b => b.GameId == gameId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bet>> GetPendingBetsAsync(Guid gameId)
    {
        return await _context.Bets
            .Include(b => b.Player)
            .Where(b => b.GameId == gameId && b.Status == BetStatus.Pending)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalBetsForGameAsync(Guid gameId)
    {
        return await _context.Bets
            .Where(b => b.GameId == gameId)
            .SumAsync(b => b.BetAmount);
    }

    public async Task<int> GetBetCountForGameAsync(Guid gameId)
    {
        return await _context.Bets
            .CountAsync(b => b.GameId == gameId);
    }

    public async Task<IEnumerable<Bet>> GetPlayerBetsAsync(Guid playerId, int limit = 50)
    {
        return await _context.Bets
            .Include(b => b.Game)
            .Where(b => b.PlayerId == playerId)
            .OrderByDescending(b => b.PlacedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bet>> GetPendingBetsByGameAsync(Guid gameId)
    {
        return await _context.Bets
            .Include(b => b.Player)
            .Where(b => b.GameId == gameId && b.Status == BetStatus.Pending)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalBetsAsync()
    {
        return await _context.Bets
            .SumAsync(b => b.BetAmount);
    }

    public async Task<decimal> GetTotalPayoutsAsync()
    {
        return await _context.Bets
            .Where(b => b.Status == BetStatus.Won)
            .SumAsync(b => b.ActualWin ?? 0);
    }
}