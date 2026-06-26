using Bingo.Application.Dtos;

namespace Bingo.Application.Services;

public readonly record struct TuneResult(int UsersTuned, string Detail);

public interface ITunerService
{
    /// <summary>
    /// Backtest lưới tham số trên draws đã lưu cho các user dùng chiến lược có thể tinh chỉnh,
    /// ghi cấu hình tốt nhất (theo metric "winrate" hoặc "roi") vào SimUser.ConfigJson.
    /// </summary>
    Task<TuneResult> OptimizeAsync(string metric = "roi", int maxDraws = 2000, CancellationToken ct = default);

    /// <summary>
    /// Săn lợi nhuận: backtest MỌI chiến lược (với cấu hình tốt nhất) trên dữ liệu thật,
    /// xếp hạng theo metric (mặc định ROI/lợi nhuận) để tìm cách chơi sinh lời nhất.
    /// </summary>
    Task<IReadOnlyList<StrategyDiscoveryDto>> DiscoverAsync(string metric = "roi", int maxDraws = 2000, CancellationToken ct = default);
}
