using Bingo.ApiService.Models;
using System.Text.Json;
using Quartz;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Bingo.ApiService.Services;

public class PredictionSummaryJob : IJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PredictionSummaryJob> _logger;
    private readonly string _summaryFolder;
    private static decimal _remainingBalance = 1000000m; // $1 million starting balance
    private static int _totalPredictions = 0;
    private static int _accuratePredictions = 0;
    private static decimal _totalWinnings = 0m;
    private static readonly string _balanceFile = "balance.json";

    // Prize money for each sum
    private static readonly Dictionary<int, decimal> PrizeMoney = new Dictionary<int, decimal>
    {
        { 3, 1200000m },
        { 4, 400000m },
        { 5, 200000m },
        { 6, 120000m },
        { 7, 80000m },
        { 8, 55000m },
        { 9, 47000m },
        { 10, 44000m },
        { 11, 44000m },
        { 12, 47000m },
        { 13, 55000m },
        { 14, 80000m },
        { 15, 120000m },
        { 16, 200000m },
        { 17, 400000m },
        { 18, 1200000m }
    };

    public PredictionSummaryJob(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PredictionSummaryJob> logger,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        // Get the current directory (where the application is running)
        var currentDirectory = Directory.GetCurrentDirectory();
        _summaryFolder = Path.Combine(currentDirectory, "PredictionResults");
        _logger.LogInformation("PredictionSummaryJob constructor called. Current directory: {CurrentDir}, Summary folder: {SummaryFolder}",
            currentDirectory, _summaryFolder);

        // Ensure summary folder exists
        if (!Directory.Exists(_summaryFolder))
        {
            try
            {
                Directory.CreateDirectory(_summaryFolder);
                _logger.LogInformation("Created summary folder: {Folder}", _summaryFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create summary folder: {Folder}", _summaryFolder);
            }
        }
        else
        {
            _logger.LogInformation("Summary folder already exists: {Folder}", _summaryFolder);
        }

        // Load balance from file if it exists
        LoadBalance();
    }

    private void LoadBalance()
    {
        var balanceFilePath = Path.Combine(_summaryFolder, _balanceFile);
        if (File.Exists(balanceFilePath))
        {
            try
            {
                var json = File.ReadAllText(balanceFilePath);
                var balanceData = JsonSerializer.Deserialize<BalanceData>(json);
                if (balanceData != null)
                {
                    _remainingBalance = balanceData.RemainingBalance;
                    _totalPredictions = balanceData.TotalPredictions;
                    _accuratePredictions = balanceData.AccuratePredictions;
                    _totalWinnings = balanceData.TotalWinnings;
                    _logger.LogInformation("Loaded balance from file: ${Balance}, Total predictions: {Total}, Accurate: {Accurate}, Winnings: ${Winnings}",
                        _remainingBalance, _totalPredictions, _accuratePredictions, _totalWinnings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading balance from file");
            }
        }
        else
        {
            _logger.LogInformation("No balance file found, using default values");
        }
    }

    private void SaveBalance()
    {
        var balanceFilePath = Path.Combine(_summaryFolder, _balanceFile);
        try
        {
            var balanceData = new BalanceData
            {
                RemainingBalance = _remainingBalance,
                TotalPredictions = _totalPredictions,
                AccuratePredictions = _accuratePredictions,
                TotalWinnings = _totalWinnings
            };
            var json = JsonSerializer.Serialize(balanceData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(balanceFilePath, json);
            _logger.LogInformation("Saved balance to file: ${Balance}, Total predictions: {Total}, Accurate: {Accurate}, Winnings: ${Winnings}",
                _remainingBalance, _totalPredictions, _accuratePredictions, _totalWinnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving balance to file");
        }
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting prediction summary job execution at {Time}", DateTime.Now);
        try
        {
            // Check if we have enough balance to make a prediction
            if (_remainingBalance < 10m)
            {
                _logger.LogWarning("Insufficient balance (${Balance}) to make a prediction. Game over!", _remainingBalance);
                return;
            }

            // Deduct $10 for the prediction
            _remainingBalance -= 10m;
            _totalPredictions++;

            var summary = new PredictionSummary
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                RemainingBalance = _remainingBalance,
                TotalPredictions = _totalPredictions,
                AccuratePredictions = _accuratePredictions,
                TotalWinnings = _totalWinnings
            };

            // Collect single prediction
            try
            {
                _logger.LogInformation("Checking prediction accuracy. Remaining balance: ${Balance}", _remainingBalance);
                using var scope = _serviceScopeFactory.CreateScope();
                var bingoService = scope.ServiceProvider.GetRequiredService<IBingoService>();
                var prediction = await bingoService.CheckPredictionAccuracyAsync();
                summary.Details = new List<PredictionAccuracyResult> { prediction };

                // Update accurate predictions count and balance
                if (prediction.IsAccurate)
                {
                    _accuratePredictions++;

                    // Get the prize money for the correct sum
                    if (prediction.ActualSum.HasValue && PrizeMoney.TryGetValue(prediction.ActualSum.Value, out decimal prize))
                    {
                        _remainingBalance += prize;
                        _totalWinnings += prize;
                        summary.PrizeWon = prize;

                        _logger.LogInformation("Prediction was accurate! Won ${Prize}. Current balance: ${Balance}, Accurate predictions: {Accurate}/{Total}, Total winnings: ${TotalWinnings}",
                            prize, _remainingBalance, _accuratePredictions, _totalPredictions, _totalWinnings);
                    }
                    else
                    {
                        _logger.LogWarning("Prediction was accurate but no prize money found for sum {Sum}", prediction.ActualSum);
                    }
                }
                else
                {
                    _logger.LogInformation("Prediction was not accurate. Current balance: ${Balance}, Accurate predictions: {Accurate}/{Total}, Total winnings: ${TotalWinnings}",
                        _remainingBalance, _accuratePredictions, _totalPredictions, _totalWinnings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking prediction accuracy");
            }

            // Calculate summary statistics
            summary.TotalPredictions = _totalPredictions;
            summary.AccuratePredictions = _accuratePredictions;
            summary.AccuracyRate = summary.TotalPredictions > 0
                ? (double)summary.AccuratePredictions / summary.TotalPredictions * 100
                : 0;
            summary.TotalWinnings = _totalWinnings;

            // Save summary to consolidated file
            var consolidatedFilePath = Path.Combine(_summaryFolder, "prediction_summaries.json");

            // Read existing summaries
            List<PredictionSummary> existingSummaries = new();
            if (File.Exists(consolidatedFilePath))
            {
                var existingJson = await File.ReadAllTextAsync(consolidatedFilePath);
                existingSummaries = JsonSerializer.Deserialize<List<PredictionSummary>>(existingJson) ?? new();
            }

            // Add new summary
            existingSummaries.Add(summary);

            // Save updated summaries
            var json = JsonSerializer.Serialize(existingSummaries, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(consolidatedFilePath, json);
            // Save balance after each prediction
            SaveBalance();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prediction summary");
        }
    }
}



public class PredictionSummaryService : IHostedService
{
    private readonly ILogger<PredictionSummaryService> _logger;
    private readonly string _summaryFolder;
    private readonly IScheduler _scheduler;

    public PredictionSummaryService(
        ILogger<PredictionSummaryService> logger,
        IScheduler scheduler,
        IConfiguration configuration)
    {
        _logger = logger;
        _scheduler = scheduler;

        // Get the current directory (where the application is running)
        var currentDirectory = Directory.GetCurrentDirectory();
        _summaryFolder = Path.Combine(currentDirectory, "PredictionResults");
        _logger.LogInformation("PredictionSummaryService constructor called. Current directory: {CurrentDir}, Summary folder: {SummaryFolder}",
            currentDirectory, _summaryFolder);

        // Ensure summary folder exists
        if (!Directory.Exists(_summaryFolder))
        {
            try
            {
                Directory.CreateDirectory(_summaryFolder);
                _logger.LogInformation("Created summary folder: {Folder}", _summaryFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create summary folder: {Folder}", _summaryFolder);
            }
        }
        else
        {
            _logger.LogInformation("Summary folder already exists: {Folder}", _summaryFolder);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PredictionSummaryService...");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping PredictionSummaryService...");
        await _scheduler.Shutdown(cancellationToken);
    }

    public async Task<List<PredictionSummary>> GetLatestSummariesAsync(int count = 5)
    {
        if (!Directory.Exists(_summaryFolder))
        {
            return new List<PredictionSummary>();
        }

        var files = Directory.GetFiles(_summaryFolder, "prediction_summary_*.json")
            .OrderByDescending(f => f)
            .Take(count);

        var summaries = new List<PredictionSummary>();
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var summary = JsonSerializer.Deserialize<PredictionSummary>(json);
                if (summary != null)
                {
                    summaries.Add(summary);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading summary file {File}", file);
            }
        }

        return summaries;
    }
}