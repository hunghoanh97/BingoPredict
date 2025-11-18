using Bingo.ApiService.Models.Entities;
using Bingo.ApiService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bingo.ApiService.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBingoService _bingoService;

    public StatisticsService(IUnitOfWork unitOfWork, IBingoService bingoService)
    {
        _unitOfWork = unitOfWork;
        _bingoService = bingoService;
    }

    public async Task<Dictionary<int, int>> GetNumberFrequencyAsync(int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);
        var games = await _unitOfWork.Games.GetCompletedGamesSinceAsync(fromDate);
        
        var frequency = new Dictionary<int, int>();
        
        for (int i = 1; i <= 6; i++)
        {
            frequency[i] = 0;
        }

        foreach (var game in games)
        {
            if (!string.IsNullOrEmpty(game.DrawnNumbers))
            {
                var numbers = System.Text.Json.JsonSerializer.Deserialize<List<int>>(game.DrawnNumbers);
                if (numbers != null)
                {
                    foreach (var number in numbers)
                    {
                        if (number >= 1 && number <= 6)
                        {
                            frequency[number]++;
                        }
                    }
                }
            }
        }

        return frequency;
    }

    public async Task<PlayerStatsDto> GetPlayerStatisticsAsync(Guid playerId)
    {
        var player = await _unitOfWork.Players.GetByIdAsync(playerId);
        if (player == null)
            throw new ArgumentException("Player not found");

        var bets = await _unitOfWork.Bets.GetPlayerBetsAsync(playerId, int.MaxValue);
        var completedBets = bets.Where(b => b.Status != BetStatus.Pending).ToList();

        var totalBets = completedBets.Count;
        var totalWins = completedBets.Count(b => b.Status == BetStatus.Won);
        var totalBetAmount = completedBets.Sum(b => b.BetAmount);
        var totalWinAmount = completedBets.Where(b => b.Status == BetStatus.Won).Sum(b => b.ActualWin ?? 0);
        var winRate = totalBets > 0 ? (decimal)totalWins / totalBets * 100 : 0;
        var profitLoss = totalWinAmount - totalBetAmount;

        var betTypeStats = new Dictionary<BetType, BetStatsDto>();
        foreach (Models.Entities.BetType entityBetType in Enum.GetValues<Models.Entities.BetType>())
        {
            var betType = (Services.BetType)entityBetType;
            var typeBets = completedBets.Where(b => b.BetType == entityBetType).ToList();
            var typeTotalBets = typeBets.Count;
            var typeWins = typeBets.Count(b => b.Status == BetStatus.Won);
            var typeTotalAmount = typeBets.Sum(b => b.BetAmount);
            var typeTotalWins = typeBets.Where(b => b.Status == BetStatus.Won).Sum(b => b.ActualWin ?? 0);
            var typeWinRate = typeTotalBets > 0 ? (decimal)typeWins / typeTotalBets * 100 : 0;

            betTypeStats[betType] = new BetStatsDto
            {
                Count = typeTotalBets,
                Wins = typeWins,
                TotalAmount = typeTotalAmount,
                TotalWins = typeTotalWins,
                WinRate = typeWinRate
            };
        }

        return new PlayerStatsDto
        {
            PlayerId = playerId,
            TotalBets = totalBets,
            TotalWins = totalWins,
            TotalBetAmount = totalBetAmount,
            TotalWinAmount = totalWinAmount,
            WinRate = winRate,
            ProfitLoss = profitLoss,
            BetTypeStats = betTypeStats
        };
    }

    public async Task<GameStatsDto> GetGameStatisticsAsync()
    {
        var totalGames = await _unitOfWork.Games.GetTotalGamesAsync();
        var completedGames = await _unitOfWork.Games.GetCompletedGamesAsync();
        var totalBets = await _unitOfWork.Bets.GetTotalBetsAsync();
        var totalPayouts = await _unitOfWork.Bets.GetTotalPayoutsAsync();

        var numberFrequency = await GetNumberFrequencyAsync(365); // Get frequency for last year

        var sizeResultFrequency = new Dictionary<SizeResult, int>
        {
            { SizeResult.Small, 0 },
            { SizeResult.Tie, 0 },
            { SizeResult.Large, 0 }
        };

        var games = await _unitOfWork.Games.GetCompletedGamesSinceAsync(DateTime.UtcNow.AddYears(-1));
        foreach (var game in games)
        {
            if (game.SizeResult.HasValue)
            {
                sizeResultFrequency[(Services.SizeResult)game.SizeResult.Value]++;
            }
        }

        return new GameStatsDto
        {
            TotalGames = totalGames,
            CompletedGames = completedGames,
            TotalBets = totalBets,
            TotalPayouts = totalPayouts,
            NumberFrequency = numberFrequency,
            SizeResultFrequency = sizeResultFrequency
        };
    }

    public async Task UpdatePlayerStatisticsAsync(Guid playerId)
    {
        var player = await _unitOfWork.Players.GetByIdAsync(playerId);
        if (player == null)
            throw new ArgumentException("Player not found");

        var stats = await GetPlayerStatisticsAsync(playerId);
        
        // Update player statistics entity if it exists, or create new one
        var playerStats = await _unitOfWork.Statistics.GetPlayerStatisticsAsync(playerId);
        if (playerStats == null)
        {
            playerStats = new PlayerStatistics
            {
                PlayerId = playerId,
                TotalBets = stats.TotalBets,
                TotalWins = stats.TotalWins,
                TotalBet = stats.TotalBetAmount,
                TotalWin = stats.TotalWinAmount,
                WinRate = stats.WinRate,
                ProfitLoss = stats.ProfitLoss,
                LastUpdated = DateTime.UtcNow
            };
            await _unitOfWork.Statistics.AddAsync(playerStats);
        }
        else
        {
            playerStats.TotalBets = stats.TotalBets;
            playerStats.TotalWins = stats.TotalWins;
            playerStats.TotalBet = stats.TotalBetAmount;
            playerStats.TotalWin = stats.TotalWinAmount;
            playerStats.WinRate = stats.WinRate;
            playerStats.ProfitLoss = stats.ProfitLoss;
            playerStats.LastUpdated = DateTime.UtcNow;
            _unitOfWork.Statistics.Update(playerStats);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<PredictionStatsDto> GetPredictionStatisticsAsync()
    {
        try
        {
            // Get prediction accuracy from ML model
            var accuracyResult = await _bingoService.CheckPredictionAccuracyAsync();
            
            return new PredictionStatsDto
            {
                TotalPredictions = accuracyResult.Predictions?.Count ?? 0,
                CorrectPredictions = accuracyResult.IsAccurate ? 1 : 0,
                AccuracyRate = accuracyResult.IsAccurate ? 100 : 0,
                ModelAccuracy = new Dictionary<string, decimal>
                {
                    { "SumPrediction", accuracyResult.IsAccurate ? 100 : 0 },
                    { "NumberFrequency", await CalculateNumberFrequencyAccuracy() }
                }
            };
        }
        catch
        {
            // Return default stats if ML service is not available
            return new PredictionStatsDto
            {
                TotalPredictions = 0,
                CorrectPredictions = 0,
                AccuracyRate = 0,
                ModelAccuracy = new Dictionary<string, decimal>()
            };
        }
    }

    private async Task<decimal> CalculateNumberFrequencyAccuracy()
    {
        // Calculate accuracy based on number frequency predictions
        var frequency = await GetNumberFrequencyAsync(30);
        var total = frequency.Values.Sum();
        
        if (total == 0) return 0;

        // Calculate how close the frequency is to uniform distribution
        var expected = total / 6.0; // Each number should appear equally
        var variance = frequency.Values.Sum(count => Math.Pow(count - expected, 2)) / 6.0;
        var stdDev = Math.Sqrt(variance);
        
        // Convert to accuracy percentage (lower std deviation = higher accuracy)
        var accuracy = Math.Max(0, 100 - (stdDev / expected * 100));
        return (decimal)accuracy;
    }
}