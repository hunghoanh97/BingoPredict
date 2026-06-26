namespace Bingo.Application.Abstractions;

/// <summary>Dữ liệu kỳ quay thô lấy từ nguồn ngoài (API bingo18).</summary>
public readonly record struct RawDraw(DateTime DrawAtUtc, string WinningResult);

/// <summary>Cổng truy cập nguồn dữ liệu kết quả quay Bingo18.</summary>
public interface IBingoDataClient
{
    /// <summary>Lấy danh sách kỳ quay (mới nhất trước), tối đa <paramref name="max"/> bản ghi.</summary>
    Task<IReadOnlyList<RawDraw>> GetDrawsAsync(int max = 100_000, CancellationToken ct = default);
}
