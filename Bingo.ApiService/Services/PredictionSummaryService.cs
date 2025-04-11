using Bingo.ApiService.Models;
using System.Text.Json;

namespace Bingo.ApiService.Services;

public class PredictionSummaryService : BackgroundService
{
    private readonly IBingoService _bingoService;
    private readonly ILogger<PredictionSummaryService> _logger;
    private readonly string _summaryFolder = "PredictionSummaries";

    public PredictionSummaryService(
        IBingoService bingoService,
        ILogger<PredictionSummaryService> logger)
    {
        _bingoService = bingoService;
        _logger = logger;

        // Ensure summary folder exists
        if (!Directory.Exists(_summaryFolder))
        {
            Directory.CreateDirectory(_summaryFolder);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var summary = new PredictionSummary
                {
                    StartTime = DateTime.Now.AddHours(-1),
                    EndTime = DateTime.Now
                };

                // Collect predictions for the last hour
                var predictions = new List<PredictionAccuracyResult>();
                for (int i = 0; i < 60; i = i + 5) // Check every minute
                {
                    try
                    {
                        var prediction = await _bingoService.CheckPredictionAccuracyAsync();
                        predictions.Add(prediction);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking prediction accuracy");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }

                // Calculate summary statistics
                summary.Details = predictions;
                summary.TotalPredictions = predictions.Count;
                summary.AccuratePredictions = predictions.Count(p => p.IsAccurate);
                summary.AccuracyRate = summary.TotalPredictions > 0
                    ? (double)summary.AccuratePredictions / summary.TotalPredictions * 100
                    : 0;

                // Save summary to file
                var fileName = $"prediction_summary_{summary.StartTime:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(_summaryFolder, fileName);
                var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(filePath, json, stoppingToken);

                _logger.LogInformation("Prediction summary saved to {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prediction summary");
            }
        }
    }
}