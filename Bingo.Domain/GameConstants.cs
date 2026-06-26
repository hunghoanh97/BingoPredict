namespace Bingo.Domain;

/// <summary>
/// Hằng số luật chơi Bingo18 (theo Vietlott).
/// </summary>
public static class GameConstants
{
    /// <summary>Mệnh giá tối thiểu / đơn vị cược = 10.000đ. Mức cược là bội số của giá trị này.</summary>
    public const decimal TicketPrice = 10_000m;

    /// <summary>Ngân sách mỗi user mỗi ngày = 1.000.000đ.</summary>
    public const decimal DailyBudget = 1_000_000m;

    /// <summary>Giới hạn AN TOÀN số dòng cược (bet line) trong 1 kỳ — không giới hạn cứng 5 vé;
    /// mức cược mỗi dòng có thể tăng (20.000, 40.000... / gấp đôi). Chỉ chặn vòng lặp chạy quá đà.</summary>
    public const int MaxBetsPerDraw = 20;

    /// <summary>Tần suất quay: 6 phút/kỳ.</summary>
    public const int DrawIntervalMinutes = 6;

    /// <summary>Múi giờ lịch quay (Việt Nam, +07:00) — dùng để xác định "ngày chơi".</summary>
    public static readonly TimeSpan GameTimeZoneOffset = TimeSpan.FromHours(7);
}
