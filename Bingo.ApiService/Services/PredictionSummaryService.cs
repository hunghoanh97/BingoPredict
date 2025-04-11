using Bingo.ApiService.Models;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Bingo.ApiService.Services;

public class PredictionSummaryService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PredictionSummaryService> _logger;
    private readonly string _summaryFolder = "PredictionSummaries";

    public PredictionSummaryService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PredictionSummaryService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
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
                for (int i = 0; i < 60; i++) // Check every minute
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var bingoService = scope.ServiceProvider.GetRequiredService<IBingoService>();
                        var prediction = await bingoService.CheckPredictionAccuracyAsync();
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