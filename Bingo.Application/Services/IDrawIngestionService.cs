namespace Bingo.Application.Services;

public interface IDrawIngestionService
{
    /// <summary>Lấy dữ liệu từ nguồn ngoài và nạp các kỳ quay MỚI vào DB. Trả về số bản ghi đã thêm.</summary>
    Task<int> IngestAsync(int max = 100_000, CancellationToken ct = default);
}
