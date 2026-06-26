using Bingo.Application.Abstractions;
using Bingo.Application.Dtos;
using Bingo.Application.Persistence;
using Bingo.Application.Simulation;
using Bingo.Domain.Entities;
using Bingo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Bingo.Application.Services;

public sealed class TunerService : ITunerService
{
    private readonly IUnitOfWork _uow;
    private readonly IStrategyRegistry _registry;
    private readonly IPredictionModel _model;
    private readonly ILogger<TunerService> _logger;

    public TunerService(IUnitOfWork uow, IStrategyRegistry registry, IPredictionModel model, ILogger<TunerService> logger)
    {
        _uow = uow;
        _registry = registry;
        _model = model;
        _logger = logger;
    }

    /// <summary>Sinh các bộ tham số ứng viên (JSON override) theo từng chiến lược.</summary>
    private static IReadOnlyList<string> Candidates(string key) => key switch
    {
        "hot_sum" => new[] { "{\"window\":50}", "{\"window\":100}", "{\"window\":200}", "{\"window\":400}" },
        "cold_sum" => new[] { "{\"window\":100}", "{\"window\":300}", "{\"window\":500}" },
        "number_hunter" => new[] { "{\"window\":50}", "{\"window\":100}", "{\"window\":200}", "{\"window\":400}" },
        "ewma_adaptive" => new[] { "{\"alpha\":0.01}", "{\"alpha\":0.05}", "{\"alpha\":0.1}", "{\"alpha\":0.2}" },
        "frequency_weighted" => new[]
        {
            "{\"topK\":1,\"window\":200}", "{\"topK\":2,\"window\":200}",
            "{\"topK\":3,\"window\":200}", "{\"topK\":5,\"window\":200}"
        },
        "martingale_size" => new[] { "{\"side\":\"Lon\"}", "{\"side\":\"Nho\"}" },
        "paroli_size" => new[] { "{\"side\":\"Lon\"}", "{\"side\":\"Nho\"}" },
        _ => Array.Empty<string>()
    };

    public async Task<TuneResult> OptimizeAsync(string metric = "roi", int maxDraws = 2000, CancellationToken ct = default)
    {
        var drawsAsc = await _uow.Draws.GetAllAscAsync();
        if (drawsAsc.Count == 0) return new TuneResult(0, "Chưa có dữ liệu draws để tinh chỉnh.");
        if (maxDraws > 0 && drawsAsc.Count > maxDraws)
            drawsAsc = drawsAsc.Skip(drawsAsc.Count - maxDraws).ToList();

        var multipliers = (await _uow.PrizeRules.GetAllAsync())
            .ToDictionary(r => (r.BetKind, r.BetValue), r => r.Multiplier);
        var strategies = (await _uow.Strategies.GetAllAsync()).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        var users = await _uow.SimUsers.GetAllAsync();
        var byRoi = !string.Equals(metric, "winrate", StringComparison.OrdinalIgnoreCase);

        int tuned = 0;
        var detail = new System.Text.StringBuilder();

        foreach (var user in users)
        {
            var candidates = Candidates(user.StrategyKey);
            if (candidates.Count == 0) continue;
            var strat = _registry.Get(user.StrategyKey);
            if (strat is null) continue;
            strategies.TryGetValue(user.StrategyKey, out var stratEntity);

            string? bestJson = null;
            decimal bestScore = decimal.MinValue;
            foreach (var candJson in candidates)
            {
                var config = StrategyConfig.Parse(stratEntity?.DefaultParamsJson, candJson);
                var m = BacktestRunner.Run(strat, config, drawsAsc, multipliers, _model, user.Id == 0 ? 1 : user.Id);
                var score = byRoi ? m.Roi : m.WinRate;
                if (score > bestScore) { bestScore = score; bestJson = candJson; }
            }

            if (bestJson is not null && bestJson != user.ConfigJson)
            {
                user.ConfigJson = bestJson;
                tuned++;
                detail.Append($"{user.Name}->{bestJson}({bestScore}); ");
            }
        }

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Tuner cập nhật {Count} user theo metric {Metric}", tuned, metric);
        return new TuneResult(tuned, detail.ToString());
    }

    public async Task<IReadOnlyList<StrategyDiscoveryDto>> DiscoverAsync(string metric = "roi", int maxDraws = 2000, CancellationToken ct = default)
    {
        var drawsAsc = await _uow.Draws.GetAllAscAsync();
        if (drawsAsc.Count == 0) return Array.Empty<StrategyDiscoveryDto>();
        if (maxDraws > 0 && drawsAsc.Count > maxDraws)
            drawsAsc = drawsAsc.Skip(drawsAsc.Count - maxDraws).ToList();

        var multipliers = (await _uow.PrizeRules.GetAllAsync())
            .ToDictionary(r => (r.BetKind, r.BetValue), r => r.Multiplier);
        var strategies = (await _uow.Strategies.GetAllAsync()).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        var byRoi = !string.Equals(metric, "winrate", StringComparison.OrdinalIgnoreCase);

        var results = new List<StrategyDiscoveryDto>();
        foreach (var strat in _registry.All)
        {
            strategies.TryGetValue(strat.Key, out var stratEntity);
            // Thử cấu hình mặc định + các ứng viên (nếu có) để chọn bộ tốt nhất.
            var configs = new List<string?> { stratEntity?.DefaultParamsJson };
            configs.AddRange(Candidates(strat.Key));

            BacktestMetrics best = default;
            string bestConfig = stratEntity?.DefaultParamsJson ?? "{}";
            decimal bestScore = decimal.MinValue;

            foreach (var cand in configs.Distinct())
            {
                var config = StrategyConfig.Parse(stratEntity?.DefaultParamsJson, cand);
                var m = BacktestRunner.Run(strat, config, drawsAsc, multipliers, _model, 12345);
                var score = byRoi ? m.Roi : m.WinRate;
                if (score > bestScore) { bestScore = score; best = m; bestConfig = cand ?? (stratEntity?.DefaultParamsJson ?? "{}"); }
            }

            results.Add(new StrategyDiscoveryDto(
                strat.Key, stratEntity?.Name ?? strat.Key, bestConfig ?? "{}",
                best.Tickets, best.WinRate, best.Staked, best.Payout, best.NetProfit, best.Roi));
        }

        return byRoi
            ? results.OrderByDescending(r => r.Roi).ToList()
            : results.OrderByDescending(r => r.WinRate).ToList();
    }
}
