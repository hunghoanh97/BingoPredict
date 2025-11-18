using Bingo.ApiService.Models.Entities;
using Bingo.ApiService.Repositories;
using System.Text.Json;

namespace Bingo.ApiService.Services;

public class GameService : IGameService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBettingService _bettingService;
    private readonly Random _random;

    public GameService(IUnitOfWork unitOfWork, IBettingService bettingService)
    {
        _unitOfWork = unitOfWork;
        _bettingService = bettingService;
        _random = new Random();
    }

    public async Task<Game> CreateGameAsync(CreateGameRequest request)
    {
        var game = new Game
        {
            GameNumber = request.GameNumber,
            DrawTime = request.DrawTime,
            Status = GameStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Games.AddAsync(game);
        await _unitOfWork.SaveChangesAsync();

        return game;
    }

    public async Task<Game?> GetGameAsync(Guid gameId)
    {
        return await _unitOfWork.Games.GetByIdAsync(gameId);
    }

    public async Task<Game?> GetCurrentGameAsync()
    {
        var now = DateTime.UtcNow;
        return await _unitOfWork.Games.GetCurrentGameAsync(now);
    }

    public async Task<GameResult> DrawNumbersAsync(Guid gameId)
    {
        var game = await _unitOfWork.Games.GetByIdAsync(gameId);
        if (game == null)
            throw new ArgumentException("Game not found");

        if (game.Status != GameStatus.Scheduled)
            throw new ArgumentException("Game is not in scheduled status");

        // Update game status to drawing
        game.Status = Models.Entities.GameStatus.Drawing;
        _unitOfWork.Games.Update(game);
        await _unitOfWork.SaveChangesAsync();

        // Draw 3 random numbers (1-6)
        var drawnNumbers = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            drawnNumbers.Add(_random.Next(1, 7)); // 1-6 inclusive
        }

        var totalSum = drawnNumbers.Sum();
        var sizeResult = GetSizeResult(totalSum);
        var entitySizeResult = (Models.Entities.SizeResult)sizeResult;

        // Update game with results
        game.DrawnNumbers = JsonSerializer.Serialize(drawnNumbers);
        game.TotalSum = totalSum;
        game.SizeResult = (Models.Entities.SizeResult)sizeResult;
        game.Status = Models.Entities.GameStatus.Completed;
        game.CompletedAt = DateTime.UtcNow;

        _unitOfWork.Games.Update(game);

        // Create game result record
        var gameResult = new GameResult
        {
            GameId = gameId,
            DrawnNumbers = JsonSerializer.Serialize(drawnNumbers),
            TotalSum = totalSum,
            SizeResult = entitySizeResult,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GameResults.AddAsync(gameResult);

        // Process all pending bets for this game
        var pendingBets = await _unitOfWork.Bets.GetPendingBetsByGameAsync(gameId);
        foreach (var bet in pendingBets)
        {
            try
            {
                await _bettingService.ProcessBetAsync(bet, game);
            }
            catch (Exception ex)
            {
                // Log error but continue processing other bets
                Console.WriteLine($"Error processing bet {bet.Id}: {ex.Message}");
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return new GameResult
        {
            GameId = gameId,
            DrawnNumbers = JsonSerializer.Serialize(drawnNumbers),
            TotalSum = totalSum,
            SizeResult = entitySizeResult,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<Game>> GetRecentGamesAsync(int limit = 10)
    {
        return await _unitOfWork.Games.GetRecentGamesAsync(limit);
    }

    public async Task<bool> CompleteGameAsync(Guid gameId)
    {
        var game = await _unitOfWork.Games.GetByIdAsync(gameId);
        if (game == null)
            return false;

        if (game.Status != GameStatus.Drawing)
            return false;

        game.Status = Models.Entities.GameStatus.Completed;
        game.CompletedAt = DateTime.UtcNow;
        
        _unitOfWork.Games.Update(game);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private Services.SizeResult GetSizeResult(int sum)
    {
        return sum switch
        {
            >= 3 and <= 10 => Services.SizeResult.Small,
            11 => Services.SizeResult.Tie,
            >= 12 and <= 18 => Services.SizeResult.Large,
            _ => throw new ArgumentOutOfRangeException(nameof(sum), "Sum must be between 3 and 18")
        };
    }
}