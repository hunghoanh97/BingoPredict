using Bingo.Application.Abstractions;
using Bingo.Application.Persistence;
using Bingo.Domain.Entities;

namespace Bingo.Application.Services;

public sealed class DrawIngestionService : IDrawIngestionService
{
    private readonly IBingoDataClient _client;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public DrawIngestionService(IBingoDataClient client, IUnitOfWork uow, IClock clock)
    {
        _client = client;
        _uow = uow;
        _clock = clock;
    }

    public async Task<int> IngestAsync(int max = 100_000, CancellationToken ct = default)
    {
        var raw = await _client.GetDrawsAsync(max, ct);
        if (raw.Count == 0) return 0;

        // Chỉ nạp các kỳ MỚI HƠN kỳ mới nhất đã có (draws có thứ tự thời gian, không trùng).
        var maxExisting = await _uow.Draws.GetMaxDrawAtAsync();
        var now = _clock.UtcNow;
        var seen = new HashSet<DateTime>();
        var toAdd = new List<Draw>();

        foreach (var r in raw.OrderBy(r => r.DrawAtUtc))
        {
            if (maxExisting is not null && r.DrawAtUtc <= maxExisting.Value) continue;
            if (!seen.Add(r.DrawAtUtc)) continue;
            try { toAdd.Add(Draw.FromRaw(r.DrawAtUtc, r.WinningResult, now)); }
            catch (ArgumentException) { /* bỏ qua bản ghi không hợp lệ */ }
        }

        if (toAdd.Count == 0) return 0;
        await _uow.Draws.AddRangeAsync(toAdd);
        await _uow.SaveChangesAsync(ct);
        return toAdd.Count;
    }
}
