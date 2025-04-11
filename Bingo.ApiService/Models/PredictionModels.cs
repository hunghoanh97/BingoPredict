namespace Bingo.ApiService.Models;

public class SumPrediction
{
    public int Sum { get; set; }
    public double Frequency { get; set; }
    public double AverageInterval { get; set; }
    public int TimeSinceLastAppearance { get; set; }
    public double Probability { get; set; }
}

public class PredictionResult
{
    public List<SumPrediction> Predictions { get; set; } = new();
    public DateTime AnalysisDate { get; set; }
    public int DataPoints { get; set; }
}

public class PredictionAccuracyResult
{
    public string Message { get; set; } = string.Empty;
    public DateTime LastPredictionTime { get; set; }
    public int? ActualSum { get; set; }
    public int? PredictedRank { get; set; }
    public bool IsAccurate { get; set; }
    public List<SumPrediction> Predictions { get; set; } = new();
}