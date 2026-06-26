using Bingo.Domain.Enums;

namespace Bingo.Domain.Entities;

/// <summary>
/// Một vé cược đặt cho một kỳ quay sắp tới (theo <see cref="TargetDrawAt"/>),
/// được settle khi kỳ đó có kết quả.
/// </summary>
public class Ticket
{
    public long Id { get; set; }

    public int SimUserId { get; set; }

    public long DailyAccountId { get; set; }

    /// <summary>Thời điểm kỳ quay được đặt cược (UTC) — để khớp với Draw khi settle.</summary>
    public DateTime TargetDrawAt { get; set; }

    /// <summary>FK tới Draw, gán khi đã settle.</summary>
    public long? DrawId { get; set; }

    public BetKind BetKind { get; set; }

    /// <summary>Giá trị cược: tổng "3".."18", size "Nho/Hoa/Lon", digit "1".."6", hoặc triple "specific:5"/"any".</summary>
    public string BetValue { get; set; } = string.Empty;

    public decimal Stake { get; set; }

    /// <summary>Hệ số trả thưởng áp dụng khi thắng (0 nếu thua).</summary>
    public decimal Multiplier { get; set; }

    public bool IsSettled { get; set; }

    public bool IsWin { get; set; }

    public decimal Payout { get; set; }

    /// <summary>Lời/lỗ của vé = Payout - Stake.</summary>
    public decimal Profit { get; set; }

    public DateTime PlacedAt { get; set; }

    public SimUser? SimUser { get; set; }
    public Draw? Draw { get; set; }
    public DailyAccount? DailyAccount { get; set; }
}
