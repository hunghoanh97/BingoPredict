namespace Bingo.ApiService.Services;

public interface IStatisticsService
{
    Task<Dictionary<int, int>> GetNumberFrequencyAsync(int days = 30);
    Task<PlayerStatsDto> GetPlayerStatisticsAsync(Guid playerId);
    Task<GameStatsDto> GetGameStatisticsAsync();
    Task UpdatePlayerStatisticsAsync(Guid playerId);
    Task<PredictionStatsDto> GetPredictionStatisticsAsync();
}

public class PlayerStatsDto
{
    public Guid PlayerId { get; set; }
    public int TotalBets { get; set; }
    public int TotalWins { get; set; }
    public decimal TotalBetAmount { get; set; }
    public decimal TotalWinAmount { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitLoss { get; set; }
    public Dictionary<BetType, BetStatsDto> BetTypeStats { get; set; } = new();
}

public class BetStatsDto
{
    public int Count { get; set; }
    public int Wins { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalWins { get; set; }
    public decimal WinRate { get; set; }
}

public class GameStatsDto
{
    public int TotalGames { get; set; }
    public int CompletedGames { get; set; }
    public decimal TotalBets { get; set; }
    public decimal TotalPayouts { get; set; }
    public Dictionary<int, int> NumberFrequency { get; set; } = new();
    public Dictionary<SizeResult, int> SizeResultFrequency { get; set; } = new();
}

public class PredictionStatsDto
{
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public decimal AccuracyRate { get; set; }
    public Dictionary<string, decimal> ModelAccuracy { get; set; } = new();
}

public enum SizeResult
{
    Small,
    Tie,
    Large
}

public enum BetType
{
    SingleNumber,
    MatchingNumbers,
    TotalSum,
    Size
}