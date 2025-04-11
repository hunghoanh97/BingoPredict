using Bingo.ApiService.Models;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;

namespace Bingo.ApiService.Services;

public interface IBingoService
{
    Task<byte[]> ExportBingoDataAsync();
    Task<PredictionResult> PredictNextSumAsync();
    Task<PredictionAccuracyResult> CheckPredictionAccuracyAsync();
    Task<List<BingoDraw>> GetLatestResultsAsync(int count = 5);
}

public class BingoService : IBingoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string API_URL = "https://bingo18.top/data/data.json";

    public BingoService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]> ExportBingoDataAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetFromJsonAsync<BingoData>(API_URL);

        if (response == null)
            throw new Exception("No data available");

        // Get the latest 10,000 draws
        var latestDraws = response.GbingoDraws
            .OrderByDescending(d => d.DrawAt)
            .Take(10000)
            .ToList();

        // Calculate statistics from draws
        var calculatedStats = CalculateStatistics(latestDraws);

        using var stream = new MemoryStream();
        using var package = new ExcelPackage(stream);

        // Create Draws worksheet
        var drawsWorksheet = package.Workbook.Worksheets.Add("Draws");
        drawsWorksheet.Cells[1, 1].Value = "Draw Time";
        drawsWorksheet.Cells[1, 2].Value = "Winning Result";
        drawsWorksheet.Cells[1, 3].Value = "Sum";

        for (int i = 0; i < latestDraws.Count; i++)
        {
            drawsWorksheet.Cells[i + 2, 1].Value = latestDraws[i].DrawAt.ToString();
            drawsWorksheet.Cells[i + 2, 2].Value = latestDraws[i].WinningResult;
            // Calculate sum of 3 digits in WinningResult
            var numbers = latestDraws[i].WinningResult.Select(c => int.Parse(c.ToString())).ToList();
            drawsWorksheet.Cells[i + 2, 3].Value = numbers.Sum();
        }
        drawsWorksheet.Cells.AutoFitColumns();

        // Create Statistics worksheet
        var statsWorksheet = package.Workbook.Worksheets.Add("Statistics");
        statsWorksheet.Cells[1, 1].Value = "Type Play";
        statsWorksheet.Cells[1, 2].Value = "Count";
        statsWorksheet.Cells[1, 3].Value = "Percentage";
        statsWorksheet.Cells[1, 4].Value = "Average Interval";

        var currentRow = 2;
        foreach (var stat in calculatedStats.OrderBy(s => s.TypePlay))
        {
            statsWorksheet.Cells[currentRow, 1].Value = stat.TypePlay;
            statsWorksheet.Cells[currentRow, 2].Value = stat.Count;
            statsWorksheet.Cells[currentRow, 3].Value = $"{stat.Percentage:F2}%";
            statsWorksheet.Cells[currentRow, 4].Value = stat.AverageInterval;
            currentRow++;
        }
        statsWorksheet.Cells.AutoFitColumns();

        await package.SaveAsync();
        return stream.ToArray();
    }

    private static List<CalculatedStatistic> CalculateStatistics(List<BingoDraw> draws)
    {
        var stats = new List<CalculatedStatistic>();
        var totalDraws = draws.Count;

        // Calculate statistics for each digit position (1-6)
        for (int position = 1; position <= 6; position++)
        {
            var digitStats = new Dictionary<int, int>();
            var lastAppearance = new Dictionary<int, int>();
            var intervals = new Dictionary<int, List<int>>();

            for (int i = 0; i < draws.Count; i++)
            {
                var winningResult = draws[i].WinningResult;
                if (string.IsNullOrEmpty(winningResult) || winningResult.Length < position)
                    continue;

                var digit = int.Parse(winningResult[position - 1].ToString());

                // Count occurrences
                if (!digitStats.ContainsKey(digit))
                    digitStats[digit] = 0;
                digitStats[digit]++;

                // Calculate intervals
                if (!lastAppearance.ContainsKey(digit))
                {
                    lastAppearance[digit] = i;
                    intervals[digit] = new List<int>();
                }
                else
                {
                    intervals[digit].Add(i - lastAppearance[digit]);
                    lastAppearance[digit] = i;
                }
            }

            // Add statistics for each digit
            foreach (var kvp in digitStats)
            {
                var digit = kvp.Key;
                var count = kvp.Value;
                var percentage = (count * 100.0) / totalDraws;
                var avgInterval = intervals[digit].Any() ? intervals[digit].Average() : 0;

                stats.Add(new CalculatedStatistic
                {
                    TypePlay = $"Position_{position}_{digit}",
                    Count = count,
                    Percentage = percentage,
                    AverageInterval = Math.Round(avgInterval, 2)
                });
            }
        }

        // Calculate sum statistics
        var sumStats = new Dictionary<int, int>();
        var sumIntervals = new Dictionary<int, List<int>>();
        var lastSumAppearance = new Dictionary<int, int>();

        for (int i = 0; i < draws.Count; i++)
        {
            var winningResult = draws[i].WinningResult;
            if (string.IsNullOrEmpty(winningResult))
                continue;

            var sum = winningResult.Select(c => int.Parse(c.ToString())).Sum();

            if (!sumStats.ContainsKey(sum))
                sumStats[sum] = 0;
            sumStats[sum]++;

            if (!lastSumAppearance.ContainsKey(sum))
            {
                lastSumAppearance[sum] = i;
                sumIntervals[sum] = new List<int>();
            }
            else
            {
                sumIntervals[sum].Add(i - lastSumAppearance[sum]);
                lastSumAppearance[sum] = i;
            }
        }

        foreach (var kvp in sumStats)
        {
            var sum = kvp.Key;
            var count = kvp.Value;
            var percentage = (count * 100.0) / totalDraws;
            var avgInterval = sumIntervals[sum].Any() ? sumIntervals[sum].Average() : 0;

            stats.Add(new CalculatedStatistic
            {
                TypePlay = $"Sum_{sum}",
                Count = count,
                Percentage = percentage,
                AverageInterval = Math.Round(avgInterval, 2)
            });
        }

        return stats;
    }

    public async Task<PredictionResult> PredictNextSumAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetFromJsonAsync<BingoData>(API_URL);

        if (response == null)
            throw new Exception("No data available");

        // Get the latest 10,000 draws
        var latestDraws = response.GbingoDraws
            .OrderByDescending(d => d.DrawAt)
            .Take(10000)
            .ToList();

        // Calculate sum statistics
        var sumStats = new Dictionary<int, int>();
        var sumIntervals = new Dictionary<int, List<int>>();
        var lastSumAppearance = new Dictionary<int, int>();

        for (int i = 0; i < latestDraws.Count; i++)
        {
            var winningResult = latestDraws[i].WinningResult;
            if (string.IsNullOrEmpty(winningResult))
                continue;

            var sum = winningResult.Select(c => int.Parse(c.ToString())).Sum();

            if (!sumStats.ContainsKey(sum))
                sumStats[sum] = 0;
            sumStats[sum]++;

            if (!lastSumAppearance.ContainsKey(sum))
            {
                lastSumAppearance[sum] = i;
                sumIntervals[sum] = new List<int>();
            }
            else
            {
                sumIntervals[sum].Add(i - lastSumAppearance[sum]);
                lastSumAppearance[sum] = i;
            }
        }

        // Calculate predictions
        var sumPredictions = new List<SumPrediction>();
        foreach (var kvp in sumStats)
        {
            var sum = kvp.Key;
            var count = kvp.Value;
            var percentage = (count * 100.0) / latestDraws.Count;
            var avgInterval = sumIntervals[sum].Any() ? sumIntervals[sum].Average() : 0;
            var lastAppearance = lastSumAppearance[sum];
            var timeSinceLastAppearance = latestDraws.Count - lastAppearance;

            sumPredictions.Add(new SumPrediction
            {
                Sum = sum,
                Frequency = percentage,
                AverageInterval = Math.Round(avgInterval, 2),
                TimeSinceLastAppearance = timeSinceLastAppearance,
                Probability = CalculateProbability(percentage, avgInterval, timeSinceLastAppearance)
            });
        }

        // Sort by probability and get top 5 predictions
        var topPredictions = sumPredictions
            .OrderByDescending(p => p.Probability)
            .Take(5)
            .ToList();

        return new PredictionResult
        {
            Predictions = topPredictions,
            AnalysisDate = DateTime.Now,
            DataPoints = latestDraws.Count
        };
    }

    private double CalculateProbability(double frequency, double avgInterval, int timeSinceLastAppearance)
    {
        // Base probability on frequency
        var baseProbability = frequency / 100.0;

        // Adjust based on time since last appearance
        var timeFactor = timeSinceLastAppearance / avgInterval;
        var timeAdjustment = Math.Min(timeFactor, 1.0);

        // Adjust based on average interval
        var intervalFactor = avgInterval > 0 ? 1.0 / avgInterval : 0;
        var intervalAdjustment = Math.Min(intervalFactor, 1.0);

        // Combine factors
        return baseProbability * (1 + timeAdjustment) * (1 + intervalAdjustment);
    }

    public async Task<PredictionAccuracyResult> CheckPredictionAccuracyAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetFromJsonAsync<BingoData>(API_URL);
        if (response == null)
            throw new Exception("No data available");
        // Get the latest 10,000 draws
        var latestDraws = response.GbingoDraws
            .OrderByDescending(d => d.DrawAt)
            .Take(10000)
            .ToList();
        // Calculate sum statistics
        var sumStats = new Dictionary<int, int>();
        var lastSumAppearance = new Dictionary<int, int>();
        for (int i = 0; i < latestDraws.Count; i++)
        {
            var winningResult = latestDraws[i].WinningResult;
            if (string.IsNullOrEmpty(winningResult))
                continue;
            var sum = winningResult.Select(c => int.Parse(c.ToString())).Sum();
            if (!sumStats.ContainsKey(sum))
                sumStats[sum] = 0;
            sumStats[sum]++;
            lastSumAppearance[sum] = i;
        }
        // Get the last prediction
        var lastPrediction = await PredictNextSumAsync();
        var predictedSum = lastPrediction.Predictions.First().Sum;
        // Check accuracy
        var actualSum = latestDraws.First().WinningResult.Select(c => int.Parse(c.ToString())).Sum();
        var isAccurate = actualSum == predictedSum;
        return new PredictionAccuracyResult
        {
            Message = isAccurate ? "Prediction was accurate" : "Prediction was not accurate",
            LastPredictionTime = DateTime.Now,
            ActualSum = actualSum,
            PredictedRank = predictedSum,
            IsAccurate = isAccurate,
            Predictions = lastPrediction.Predictions
        };
    }

    public async Task<List<BingoDraw>> GetLatestResultsAsync(int count = 5)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetFromJsonAsync<BingoData>(API_URL);
        if (response == null)
            throw new Exception("No data available");

        return response.GbingoDraws
            .OrderByDescending(d => d.DrawAt)
            .Take(count)
            .ToList();
    }
}