namespace Bingo.ApiService.Models;

public class PredictionSummary
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalPredictions { get; set; }
    public int AccuratePredictions { get; set; }
    public double AccuracyRate { get; set; }
    public List<PredictionAccuracyResult> Details { get; set; } = new();
}