namespace Bingo.ApiService.Models;


public class PredictionSummary
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalPredictions { get; set; }
    public int AccuratePredictions { get; set; }
    public double AccuracyRate { get; set; }
    public List<PredictionAccuracyResult> Details { get; set; } = new List<PredictionAccuracyResult>();
    public decimal RemainingBalance { get; set; }
    public decimal TotalWinnings { get; set; }
    public decimal PrizeWon { get; set; }
}

public class BalanceData
{
    public decimal RemainingBalance { get; set; }
    public int TotalPredictions { get; set; }
    public int AccuratePredictions { get; set; }
    public decimal TotalWinnings { get; set; }
}