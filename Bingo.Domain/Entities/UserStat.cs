namespace Bingo.Domain.Entities;

/// <summary>
/// Thống kê tổng hợp toàn cục của một user (tính dồn qua các ngày).
/// </summary>
public class UserStat
{
    /// <summary>PK đồng thời là FK 1-1 tới SimUser.</summary>
    public int SimUserId { get; set; }

    public int TotalTickets { get; set; }

    public int TotalWins { get; set; }

    /// <summary>Tỉ lệ thắng vé, 0-100.</summary>
    public decimal WinRate { get; set; }

    public decimal TotalStaked { get; set; }

    public decimal TotalPayout { get; set; }

    public decimal NetProfit { get; set; }

    /// <summary>ROI (%) = NetProfit / TotalStaked * 100.</summary>
    public decimal Roi { get; set; }

    public int DaysPlayed { get; set; }

    public int DaysBusted { get; set; }

    public DateTime UpdatedAt { get; set; }

    public SimUser? SimUser { get; set; }
}
