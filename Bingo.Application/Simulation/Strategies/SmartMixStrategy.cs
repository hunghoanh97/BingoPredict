using Bingo.Domain;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation.Strategies;

/// <summary>
/// "Mix" quản lý vốn (nghiên cứu từ dữ liệu): cược cửa EV tốt nhất + CHỌN LỌC + CHỐT LỜI/DỪNG LỖ theo ngày.
/// Vì game EV âm và không có bias khai thác được, mục tiêu là GIẢM LỖ tối đa, giữ nhiều ngày dương.
/// Stateless (tự duy trì qua số dư) — không cần martingale.
/// Config:
///  - "target": "Hoa" (mặc định, EV quan sát tốt nhất) | "mid" (cược tổng 10 &amp; 11) | "Lon" | "Nho"
///  - "everyN": chơi cách kỳ (mặc định 1)
///  - "takeProfit": lãi tới mức này thì NGHỈ trong ngày (mặc định 200.000)
///  - "stopLoss": lỗ tới mức này thì NGHỈ trong ngày (mặc định 300.000)
///  - "stakeUnits": số đơn vị 10.000 mỗi lần (mặc định 1)
/// </summary>
public sealed class SmartMixStrategy : StrategyBase
{
    public override string Key => "smart_mix";

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        var profit = ctx.Balance - GameConstants.DailyBudget;
        var tp = (decimal)ctx.Config.GetDouble("takeProfit", 200_000);
        var sl = (decimal)ctx.Config.GetDouble("stopLoss", 300_000);
        if (profit >= tp || profit <= -sl) return Array.Empty<BetDecision>(); // đã chốt lời / dừng lỗ hôm nay

        var everyN = Math.Max(1, ctx.Config.GetInt("everyN", 1));
        if (everyN > 1 && ctx.RecentDraws.Count > 0)
        {
            var slot = (int)(ctx.RecentDraws[0].DrawAt.TimeOfDay.TotalMinutes / GameConstants.DrawIntervalMinutes);
            if (slot % everyN != 0) return Array.Empty<BetDecision>();
        }

        var units = Math.Max(1, ctx.Config.GetInt("stakeUnits", 1));
        var stake = GameConstants.TicketPrice * units;
        var target = ctx.Config.GetString("target", "Hoa");

        if (string.Equals(target, "mid", StringComparison.OrdinalIgnoreCase))
            return new[] { new BetDecision(BetKind.Sum, "10", stake), new BetDecision(BetKind.Sum, "11", stake) };

        return new[] { new BetDecision(BetKind.Size, target, stake) };
    }
}
