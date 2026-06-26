using Bingo.Application.Abstractions;
using Bingo.Domain;
using Bingo.Domain.Entities;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation;

/// <summary>Một quyết định cược: kiểu cược, giá trị, và MỨC CƯỢC (bội số của 10.000).</summary>
public sealed record BetDecision(BetKind BetKind, string BetValue, decimal Stake = GameConstants.TicketPrice);

/// <summary>Bối cảnh truyền cho chiến lược khi quyết định đặt cược cho kỳ kế tiếp.</summary>
public sealed class StrategyContext
{
    /// <summary>Các kỳ quay gần đây, MỚI NHẤT Ở ĐẦU (index 0 = gần nhất).</summary>
    public required IReadOnlyList<Draw> RecentDraws { get; init; }

    /// <summary>Giới hạn an toàn số dòng cược trong kỳ.</summary>
    public required int MaxBets { get; init; }

    public required decimal Balance { get; init; }
    public required decimal TicketPrice { get; init; }
    public required StrategyConfig Config { get; init; }
    public required StrategyState State { get; init; }
    public required Random Rng { get; init; }
    public IPredictionModel? PredictionModel { get; init; }

    /// <summary>Bảng hệ số trả thưởng (cho các chiến lược dựa trên EV).</summary>
    public required IReadOnlyDictionary<(BetKind, string), decimal> Multipliers { get; init; }
}

/// <summary>Chiến lược cá cược. Engine lo ràng buộc vốn & số vé; chiến lược chỉ đề xuất.</summary>
public interface IBettingStrategy
{
    string Key { get; }
    bool IsAdaptive { get; }

    /// <summary>Đề xuất các vé cho kỳ kế tiếp (engine sẽ cắt theo vốn & giới hạn 5 vé).</summary>
    IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx);

    /// <summary>Cập nhật trạng thái sau khi biết kết quả kỳ (chỉ chiến lược adaptive cần).</summary>
    void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng);
}

/// <summary>Lớp cơ sở: mặc định không adaptive, OnSettled rỗng.</summary>
public abstract class StrategyBase : IBettingStrategy
{
    public abstract string Key { get; }
    public virtual bool IsAdaptive => false;
    public abstract IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx);
    public virtual void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng) { }

    /// <summary>Tổng xuất hiện nhiều nhất trong cửa sổ N kỳ gần đây.</summary>
    protected static int HottestSum(IReadOnlyList<Draw> draws, int window)
    {
        var counts = new int[19];
        foreach (var d in draws.Take(window)) counts[d.Sum]++;
        int best = 10, bestC = -1;
        for (int s = 3; s <= 18; s++) if (counts[s] > bestC) { bestC = counts[s]; best = s; }
        return best;
    }
}
