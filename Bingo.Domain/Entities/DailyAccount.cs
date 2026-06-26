namespace Bingo.Domain.Entities;

/// <summary>
/// Ví của một user trong một ngày. Mỗi ngày reset về <see cref="GameConstants.DailyBudget"/>.
/// Hết tiền (không đủ mua 1 vé) thì <see cref="IsBusted"/> = true và dừng ngày đó.
/// </summary>
public class DailyAccount
{
    public long Id { get; set; }

    public int SimUserId { get; set; }

    /// <summary>Ngày chơi (theo giờ địa phương của lịch quay).</summary>
    public DateOnly GameDate { get; set; }

    public decimal StartingBalance { get; set; }

    public decimal CurrentBalance { get; set; }

    public int TicketsBought { get; set; }

    public decimal TotalStaked { get; set; }

    public decimal TotalPayout { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public bool IsBusted { get; set; }

    public SimUser? SimUser { get; set; }

    /// <summary>Lời/lỗ ròng trong ngày.</summary>
    public decimal NetProfit => CurrentBalance - StartingBalance;
}
