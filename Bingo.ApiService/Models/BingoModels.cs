using System.Text.Json.Serialization;

namespace Bingo.ApiService.Models;

public class BingoData
{
    [JsonPropertyName("gbingoDraws")]
    public List<BingoDraw> GbingoDraws { get; set; } = new();

    [JsonPropertyName("gbingoStatistics")]
    public List<BingoStatistic> GbingoStatistics { get; set; } = new();
}

public class BingoDraw
{
    [JsonPropertyName("drawAt")]
    public DateTime DrawAt { get; set; }

    [JsonPropertyName("winningResult")]
    public string WinningResult { get; set; } = string.Empty;
}

public class BingoStatistic
{
    [JsonPropertyName("typePlay")]
    public string TypePlay { get; set; } = string.Empty;

    [JsonPropertyName("countNearest")]
    public int CountNearest { get; set; }

    [JsonPropertyName("countHit")]
    public int CountHit { get; set; }

    [JsonPropertyName("averageCount")]
    public int AverageCount { get; set; }
}

public class CalculatedStatistic
{
    public string TypePlay { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public double AverageInterval { get; set; }
}