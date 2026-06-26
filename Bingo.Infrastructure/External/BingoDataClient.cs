using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Bingo.Application.Abstractions;

namespace Bingo.Infrastructure.External;

/// <summary>Lấy kết quả quay Bingo18 từ API công khai.</summary>
public sealed class BingoDataClient : IBingoDataClient
{
    public const string DataUrl = "https://bingo18.top/data/data.json";

    private readonly HttpClient _http;
    public BingoDataClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<RawDraw>> GetDrawsAsync(int max = 100_000, CancellationToken ct = default)
    {
        var data = await _http.GetFromJsonAsync<BingoDataDto>(DataUrl, ct);
        if (data?.GbingoDraws is null || data.GbingoDraws.Count == 0)
            return Array.Empty<RawDraw>();

        return data.GbingoDraws
            .Where(d => !string.IsNullOrWhiteSpace(d.WinningResult))
            .Select(d => new RawDraw(d.DrawAt.UtcDateTime, d.WinningResult))
            .OrderByDescending(r => r.DrawAtUtc)
            .Take(max)
            .ToList();
    }

    private sealed class BingoDataDto
    {
        [JsonPropertyName("gbingoDraws")]
        public List<DrawDto>? GbingoDraws { get; set; }
    }

    private sealed class DrawDto
    {
        [JsonPropertyName("drawAt")]
        public DateTimeOffset DrawAt { get; set; }

        [JsonPropertyName("winningResult")]
        public string WinningResult { get; set; } = string.Empty;
    }
}
