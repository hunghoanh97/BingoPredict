using Bingo.ApiService.Models.Entities;

namespace Bingo.ApiService.Services;

public interface IGameService
{
    Task<Game> CreateGameAsync(CreateGameRequest request);
    Task<Game?> GetGameAsync(Guid gameId);
    Task<Game?> GetCurrentGameAsync();
    Task<GameResult> DrawNumbersAsync(Guid gameId);
    Task<IEnumerable<Game>> GetRecentGamesAsync(int limit = 10);
    Task<bool> CompleteGameAsync(Guid gameId);
}

public class CreateGameRequest
{
    public string GameNumber { get; set; } = string.Empty;
    public DateTime DrawTime { get; set; }
}

public class GameResultDto
{
    public Guid GameId { get; set; }
    public string GameNumber { get; set; } = string.Empty;
    public List<int> DrawnNumbers { get; set; } = new();
    public int TotalSum { get; set; }
    public SizeResult SizeResult { get; set; }
    public DateTime DrawTime { get; set; }
}