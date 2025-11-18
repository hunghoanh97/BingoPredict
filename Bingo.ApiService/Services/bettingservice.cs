using Bingo.ApiService.Models.Entities;
using Bingo.ApiService.Repositories;
using System.Text.Json;

namespace Bingo.ApiService.Services;

public class BettingService : IBettingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPrizeCalculationService _prizeCalculationService;

    public BettingService(IUnitOfWork unitOfWork, IPrizeCalculationService prizeCalculationService)
    {
        _unitOfWork = unitOfWork;
        _prizeCalculationService = prizeCalculationService;
    }

    public async Task<Bet> PlaceBetAsync(PlaceBetRequest request)
    {
        // Validate the bet
        var isValid = await ValidateBetAsync(request);
        if (!isValid)
            throw new ArgumentException("Invalid bet request");

        // Check if game exists and is open for betting
        var game = await _unitOfWork.Games.GetByIdAsync(request.GameId);
        if (game == null)
            throw new ArgumentException("Game not found");

        if (game.Status != Models.Entities.GameStatus.Scheduled)
            throw new ArgumentException("Game is not open for betting");

        // Check if player exists
        var player = await _unitOfWork.Players.GetByIdAsync(request.PlayerId);
        if (player == null)
            throw new ArgumentException("Player not found");

        // Check player balance
        if (player.Balance < request.BetAmount)
            throw new ArgumentException("Insufficient balance");

        // Calculate potential win
        var potentialWin = await CalculatePotentialWinAsync(request.BetType, request.BetAmount, request.BetNumbers);

        // Create bet
        var bet = new Bet
        {
            PlayerId = request.PlayerId,
            GameId = request.GameId,
            BetType = (Models.Entities.BetType)request.BetType,
            BetNumbers = request.BetNumbers,
            BetAmount = request.BetAmount,
            PotentialWin = potentialWin,
            Status = Models.Entities.BetStatus.Pending,
            PlacedAt = DateTime.UtcNow
        };

        // Deduct bet amount from player balance
        player.Balance -= request.BetAmount;

        // Add bet and update player
        await _unitOfWork.Bets.AddAsync(bet);
        _unitOfWork.Players.Update(player);

        // Create transaction record
        var transaction = new Transaction
        {
            PlayerId = request.PlayerId,
            Type = Models.Entities.TransactionType.BetPlaced,
            Amount = -request.BetAmount,
            BalanceAfter = player.Balance,
            Description = $"Bet placed on game {game.GameNumber}",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Transactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return bet;
    }

    public async Task<BetResult> ProcessBetAsync(Bet bet, Game game)
    {
        if (bet.Status != BetStatus.Pending)
            throw new ArgumentException("Bet is not pending");

        if (game.Status != GameStatus.Completed || string.IsNullOrEmpty(game.DrawnNumbers))
            throw new ArgumentException("Game is not completed or has no drawn numbers");

        var drawnNumbers = JsonSerializer.Deserialize<List<int>>(game.DrawnNumbers!);
        if (drawnNumbers == null || drawnNumbers.Count != 3)
            throw new ArgumentException("Invalid drawn numbers");

        // Calculate win
        var winAmount = _prizeCalculationService.CalculatePrize((Services.BetType)bet.BetType, bet.BetNumbers, drawnNumbers, bet.BetAmount);
        var isWin = winAmount > 0;
        var multiplier = _prizeCalculationService.GetMultiplier((Services.BetType)bet.BetType, bet.BetNumbers, drawnNumbers);

        // Update bet
        bet.Status = isWin ? Models.Entities.BetStatus.Won : Models.Entities.BetStatus.Lost;
        bet.ActualWin = isWin ? winAmount : 0;
        bet.ResolvedAt = DateTime.UtcNow;

        // Update player balance if won
        if (isWin)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(bet.PlayerId);
            if (player != null)
            {
                player.Balance += winAmount;
                _unitOfWork.Players.Update(player);

                // Create win transaction
                var winTransaction = new Transaction
                {
                    PlayerId = bet.PlayerId,
                    Type = Models.Entities.TransactionType.BetWon,
                    Amount = winAmount,
                    BalanceAfter = player.Balance,
                    Description = $"Bet won on game {game.GameNumber}",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Transactions.AddAsync(winTransaction);
            }
        }

        _unitOfWork.Bets.Update(bet);
        await _unitOfWork.SaveChangesAsync();

        return new BetResult
        {
            IsWin = isWin,
            WinAmount = winAmount,
            Multiplier = multiplier,
            ResultDetails = isWin ? $"Won {winAmount:C2}" : "Lost"
        };
    }

    public async Task<IEnumerable<Bet>> GetPlayerBetsAsync(Guid playerId, int limit = 50)
    {
        return await _unitOfWork.Bets.GetPlayerBetsAsync(playerId, limit);
    }

    public async Task<decimal> CalculatePotentialWinAsync(BetType betType, decimal betAmount, string? betNumbers)
    {
        if (betAmount <= 0)
            return 0;

        // For potential win calculation, assume the bet wins
        // Use default drawn numbers for calculation
        var defaultDrawnNumbers = new List<int> { 1, 2, 3 }; // Default numbers for calculation
        
        return _prizeCalculationService.CalculatePrize(betType, betNumbers, defaultDrawnNumbers, betAmount);
    }

    public async Task<bool> ValidateBetAsync(PlaceBetRequest request)
    {
        if (request.BetAmount <= 0)
            return false;

        if (string.IsNullOrEmpty(request.BetNumbers))
            return false;

        try
        {
            return ((Models.Entities.BetType)request.BetType) switch
            {
                Models.Entities.BetType.SingleNumber => ValidateSingleNumberBet(request.BetNumbers),
                Models.Entities.BetType.MatchingNumbers => ValidateMatchingNumbersBet(request.BetNumbers),
                Models.Entities.BetType.TotalSum => ValidateTotalSumBet(request.BetNumbers),
                Models.Entities.BetType.Size => ValidateSizeBet(request.BetNumbers),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateSingleNumberBet(string betNumbers)
    {
        var number = JsonSerializer.Deserialize<int>(betNumbers);
        return number >= 1 && number <= 6;
    }

    private bool ValidateMatchingNumbersBet(string betNumbers)
    {
        var numbers = JsonSerializer.Deserialize<int[]>(betNumbers);
        return numbers != null && numbers.Length == 3 && 
               numbers.All(n => n >= 1 && n <= 6);
    }

    private bool ValidateTotalSumBet(string betNumbers)
    {
        var sum = JsonSerializer.Deserialize<int>(betNumbers);
        return sum >= 3 && sum <= 18;
    }

    private bool ValidateSizeBet(string betNumbers)
    {
        var size = JsonSerializer.Deserialize<SizeResult>(betNumbers);
        return Enum.IsDefined(typeof(SizeResult), size);
    }
}