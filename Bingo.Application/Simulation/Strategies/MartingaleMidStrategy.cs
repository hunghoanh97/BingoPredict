using Bingo.Domain;
using Bingo.Domain.Entities;
using Bingo.Domain.Enums;

namespace Bingo.Application.Simulation.Strategies;

/// <summary>
/// Cược tổng 10 &amp; 11 (mỗi tổng trả ×4.4), GẤP ĐÔI tiền cược mỗi lần thua, reset khi thắng,
/// bắt đầu từ 10.000 và RESET MỖI NGÀY. Tùy chọn:
///  - "single": chỉ cược tổng 11 (bắt đầu đúng 10.000/kỳ) thay vì cả 10 &amp; 11.
///  - "everyN": chơi cách kỳ (chỉ đặt mỗi N kỳ).
///  - "stopOnWin": thắng xong là NGHỈ phần còn lại trong ngày (chốt lời "đến khi thắng").
///  - "maxStake": trần mức cược (mặc định = ngân sách ngày).
/// </summary>
public sealed class MartingaleMidStrategy : StrategyBase
{
    public override string Key => "martingale_mid";
    public override bool IsAdaptive => true;

    public override IReadOnlyList<BetDecision> DecideBets(StrategyContext ctx)
    {
        // Ngày mới (vốn đầy) → reset chuỗi gấp đôi & cờ "đã thắng hôm nay".
        if (ctx.Balance >= GameConstants.DailyBudget)
        {
            ctx.State.SetScalar("stake", (double)GameConstants.TicketPrice);
            ctx.State.SetScalar("won", 0);
        }

        if (ctx.Config.GetBool("stopOnWin", false) && ctx.State.GetScalar("won", 0) >= 1)
            return Array.Empty<BetDecision>(); // đã thắng hôm nay → nghỉ

        var everyN = Math.Max(1, ctx.Config.GetInt("everyN", 1));
        if (everyN > 1 && ctx.RecentDraws.Count > 0)
        {
            var slot = (int)(ctx.RecentDraws[0].DrawAt.TimeOfDay.TotalMinutes / GameConstants.DrawIntervalMinutes);
            if (slot % everyN != 0) return Array.Empty<BetDecision>(); // cách kỳ: bỏ qua
        }

        var stake = (decimal)ctx.State.GetScalar("stake", (double)GameConstants.TicketPrice);
        if (stake < GameConstants.TicketPrice) stake = GameConstants.TicketPrice;

        if (ctx.Config.GetBool("single", false))
            return new[] { new BetDecision(BetKind.Sum, "11", stake) };

        return new[]
        {
            new BetDecision(BetKind.Sum, "10", stake),
            new BetDecision(BetKind.Sum, "11", stake)
        };
    }

    public override void OnSettled(StrategyState state, StrategyConfig config, Draw resultDraw, IReadOnlyList<Ticket> settledTickets, Random rng)
    {
        if (settledTickets.Count == 0) return; // kỳ bị bỏ qua → giữ nguyên
        var anyWin = settledTickets.Any(t => t.IsWin);
        var cur = (decimal)state.GetScalar("stake", (double)GameConstants.TicketPrice);
        var cap = (decimal)config.GetDouble("maxStake", (double)GameConstants.DailyBudget);
        state.SetScalar("stake", (double)(anyWin ? GameConstants.TicketPrice : Math.Min(cur * 2m, cap)));
        if (anyWin) state.SetScalar("won", 1);
    }
}
