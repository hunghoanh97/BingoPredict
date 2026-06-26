namespace Bingo.Application.Services;

public readonly record struct ReplayResult(int DrawsProcessed, int TicketsCreated);

public interface ISimulationService
{
    /// <summary>
    /// Một "nhịp" live: nạp kỳ mới, settle các vé đã đặt cho kỳ vừa quay, rồi đặt cược cho kỳ kế tiếp.
    /// Trả về số vé vừa đặt.
    /// </summary>
    Task<int> RunTickAsync(CancellationToken ct = default);

    /// <summary>
    /// Backtest: chạy lại toàn bộ mô phỏng trên dữ liệu draws đã lưu (đặt + settle ngay theo từng kỳ),
    /// điền tickets/daily accounts/stats. Reset dữ liệu cá cược trước khi chạy.
    /// </summary>
    Task<ReplayResult> ReplayAsync(int maxDraws = 2000, CancellationToken ct = default);

    /// <summary>Tính lại bảng UserStat từ DailyAccounts.</summary>
    Task RecomputeStatsAsync(CancellationToken ct = default);
}
