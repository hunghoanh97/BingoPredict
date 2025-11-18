using Bingo.ApiService.Models.Entities;

namespace Bingo.ApiService.Services;

public interface IBettingService
{
    Task<Bet> PlaceBetAsync(PlaceBetRequest request);
    Task<BetResult> ProcessBetAsync(Bet bet, Game game);
    Task<IEnumerable<Bet>> GetPlayerBetsAsync(Guid playerId, int limit = 50);
    Task<decimal> CalculatePotentialWinAsync(BetType betType, decimal betAmount, string? betNumbers);
    Task<bool> ValidateBetAsync(PlaceBetRequest request);
}

public class PlaceBetRequest
{
    public Guid PlayerId { get; set; }
    public Guid GameId { get; set; }
    public BetType BetType { get; set; }
    public string? BetNumbers { get; set; } // JSON array based on bet type
    public decimal BetAmount { get; set; }
}

public class BetResult
{
    public bool IsWin { get; set; }
    public decimal WinAmount { get; set; }
    public decimal Multiplier { get; set; }
    public string ResultDetails { get; set; } = string.Empty;
}