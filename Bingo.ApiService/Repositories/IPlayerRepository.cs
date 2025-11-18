using Bingo.ApiService.Models.Entities;

namespace Bingo.ApiService.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(Guid id);
    Task<Player?> GetByUsernameAsync(string username);
    Task<Player?> GetByEmailAsync(string email);
    Task<Player> CreateAsync(CreatePlayerRequest request);
    Task<Player> UpdateAsync(Player player);
    Task Update(Player player);
    Task<bool> UpdateBalanceAsync(Guid playerId, decimal amount, bool isAddition = true);
    Task<decimal> GetBalanceAsync(Guid playerId);
    Task<IEnumerable<Player>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<int> GetTotalCountAsync();
}

public class CreatePlayerRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal InitialBalance { get; set; } = 0;
}