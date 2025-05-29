using Bingo.ApiService.Models;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using Bingo_ApiService; // For PredictNumberModel
using System.Globalization; // For CultureInfo

namespace Bingo.ApiService.Services;

public interface IBingoService
{
    Task<byte[]> ExportBingoDataAsync();
    Task<PredictionResult> PredictNextSumAsync();
    Task<PredictionAccuracyResult> CheckPredictionAccuracyAsync();
    Task<List<BingoDraw>> GetLatestResultsAsync(int count = 5);
    Task<byte[]> ExportBingoDataAsCsvAsync();
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

        // Get the latest 50,000 draws
        var latestDraws = response.GbingoDraws
            .OrderByDescending(d => d.DrawAt)
            .Take(50000)
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

    public async Task<byte[]> ExportBingoDataAsCsvAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetFromJsonAsync<BingoData>(API_URL);

        if (response == null)
            throw new Exception("No data available");

        // Get the latest 100,000 draws
        var latestDraws = response.GbingoDraws
            .OrderByDescending(d => d.DrawAt)
            .Take(100000)
            .ToList();

        // Calculate statistics from draws
        var calculatedStats = CalculateStatistics(latestDraws);

        var sb = new System.Text.StringBuilder();

        // Draws section
        sb.AppendLine("Draw Time,Winning Result,Sum");
        foreach (var draw in latestDraws)
        {
            var numbers = draw.WinningResult.Select(c => int.Parse(c.ToString())).ToList();
            var sum = numbers.Sum();
            // Escape commas in WinningResult if needed
            sb.AppendLine($"\"{draw.DrawAt:O}\",\"{draw.WinningResult}\",{sum}");
        }

        // Add a blank line to separate sections
        sb.AppendLine();

        // Statistics section
        sb.AppendLine("Type Play,Count,Percentage,Average Interval");
        foreach (var stat in calculatedStats.OrderBy(s => s.TypePlay))
        {
            sb.AppendLine($"\"{stat.TypePlay}\",{stat.Count},{stat.Percentage:F2}%,{stat.AverageInterval}");
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
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
        var bingoDataResponse = await client.GetFromJsonAsync<BingoData>(API_URL);

        if (bingoDataResponse == null || !bingoDataResponse.GbingoDraws.Any())
            throw new Exception("No data available or no draws found to make a prediction.");

        // Get the latest 50,000 draws for historical stats, ensure order is most recent first
        var latestDraws = bingoDataResponse.GbingoDraws
            .OrderByDescending(d => d.DrawAt)
            .Take(50000)
            .ToList();

        if (!latestDraws.Any())
            throw new Exception("No draws available after filtering to make a prediction.");

        var lastActualDraw = latestDraws.First(); // The most recent draw to base prediction on

        // Prepare input for the ML model
        // The model expects Draw_Time as string, Winning_Result as float, and Sum as float.
        // We use the last known draw's data. The model will predict the 'Sum' for a hypothetical next draw based on this input pattern.
        var modelInput = new PredictNumberModel.ModelInput
        {
            Draw_Time = lastActualDraw.DrawAt.ToString("o", CultureInfo.InvariantCulture), // ISO 8601 format
            Winning_Result = float.Parse(lastActualDraw.WinningResult, CultureInfo.InvariantCulture), // Assuming WinningResult is a numeric string
            // The 'Sum' in ModelInput is likely a feature, not the target for this specific prediction call if the model predicts 'Score' as sum.
            // If 'Sum' is indeed a feature, we provide the sum of the last draw.
            // If the model was trained to predict the *next* sum based on previous sum, this is correct.
            // If the model was trained to predict the sum of the *current* input, then 'Score' is that prediction.
            Sum = (float)lastActualDraw.WinningResult.Select(c => int.Parse(c.ToString())).Sum()
        };

        // Get prediction from ML model
        var predictionOutput = PredictNumberModel.Predict(modelInput);
        // 'Score' is the predicted sum by the ML model.
        int mlPredictedSum = (int)Math.Round(predictionOutput.Score);

        // Calculate historical statistics for the mlPredictedSum from latestDraws
        double statFrequency = 0;
        double statAverageInterval = 0;
        int statTimeSinceLastAppearance = latestDraws.Count; // Default if not found, means it never appeared or appeared >50k draws ago

        var sumOccurrencesIndices = new List<int>(); // Stores 0-based indices from latestDraws (0 is most recent)
        for (int i = 0; i < latestDraws.Count; i++)
        {
            if (latestDraws[i].WinningResult.Select(c => int.Parse(c.ToString())).Sum() == mlPredictedSum)
            {
                sumOccurrencesIndices.Add(i);
            }
        }

        if (sumOccurrencesIndices.Any())
        {
            statFrequency = (sumOccurrencesIndices.Count * 100.0) / latestDraws.Count;
            statTimeSinceLastAppearance = sumOccurrencesIndices.Min(); // Min index is the most recent appearance (0 = last draw)

            if (sumOccurrencesIndices.Count > 1)
            {
                var intervals = new List<int>();
                var sortedIndices = sumOccurrencesIndices.OrderBy(x => x).ToList(); // Sort by how long ago they occurred
                for (int i = 0; i < sortedIndices.Count - 1; i++)
                {
                    intervals.Add(sortedIndices[i + 1] - sortedIndices[i]);
                }
                statAverageInterval = intervals.Average();
            }
            // If only one occurrence, avgInterval could be 0, or latestDraws.Count, or specific logic
            // For now, if only one occurrence, average interval remains 0 (or could be set to latestDraws.Count)
        }

        var topPredictions = new List<SumPrediction>();
        topPredictions.Add(new SumPrediction
        {
            Sum = mlPredictedSum,
            Frequency = Math.Round(statFrequency, 2),
            AverageInterval = Math.Round(statAverageInterval, 2),
            TimeSinceLastAppearance = statTimeSinceLastAppearance, // This is 'draws ago'
            // Probability from the old method is not directly applicable. 
            // We can set a high confidence for the ML model's direct prediction or use its raw score if available and meaningful.
            // For simplicity, setting to 1.0 to indicate it's the primary prediction.
            Probability = 1.0 
        });

        // If you want to add other heuristic-based predictions as fallback or supplementary:
        // You could re-implement parts of the old logic here to generate other SumPrediction objects
        // and add them to topPredictions, then sort and take top 5.
        // For now, we only return the ML model's prediction.

        return new PredictionResult
        {
            Predictions = topPredictions, // Returns the single ML prediction with its historical stats
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
        // Get the latest 50,000 draws
        var latestDraws = response.GbingoDraws
            .OrderByDescending(d => d.DrawAt)
            .Take(50000)
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