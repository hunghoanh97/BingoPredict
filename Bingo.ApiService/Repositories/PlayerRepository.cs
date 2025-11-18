using Bingo.ApiService.Data;
using Bingo.ApiService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bingo.ApiService.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly BingoDbContext _context;

    public PlayerRepository(BingoDbContext context)
    {
        _context = context;
    }

    public async Task<Player?> GetByIdAsync(Guid id)
    {
        return await _context.Players
            .Include(p => p.Bets)
            .Include(p => p.Transactions)
            .Include(p => p.Statistics)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Player?> GetByUsernameAsync(string username)
    {
        return await _context.Players
            .Include(p => p.Bets)
            .Include(p => p.Transactions)
            .Include(p => p.Statistics)
            .FirstOrDefaultAsync(p => p.Username == username);
    }

    public async Task<Player?> GetByEmailAsync(string email)
    {
        return await _context.Players
            .Include(p => p.Bets)
            .Include(p => p.Transactions)
            .Include(p => p.Statistics)
            .FirstOrDefaultAsync(p => p.Email == email);
    }

    public async Task<Player> CreateAsync(CreatePlayerRequest request)
    {
        var player = new Player
        {
            Username = request.Username,
            Email = request.Email,
            Phone = request.Phone,
            Balance = request.InitialBalance,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Players.Add(player);
        await _context.SaveChangesAsync();
        return player;
    }

    public async Task<Player> UpdateAsync(Player player)
    {
        player.UpdatedAt = DateTime.UtcNow;
        _context.Players.Update(player);
        await _context.SaveChangesAsync();
        return player;
    }

    public async Task Update(Player player)
    {
        player.UpdatedAt = DateTime.UtcNow;
        _context.Players.Update(player);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateBalanceAsync(Guid playerId, decimal amount, bool isAddition = true)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        if (isAddition)
        {
            player.Balance += amount;
        }
        else
        {
            if (player.Balance < amount) return false;
            player.Balance -= amount;
        }

        player.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetBalanceAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        return player?.Balance ?? 0;
    }

    public async Task<IEnumerable<Player>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        return await _context.Players
            .OrderBy(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Players.CountAsync();
    }
}