using Bingo.Domain.Enums;

namespace Bingo.Domain.Entities;

/// <summary>
/// Hệ số trả thưởng theo cách chơi. Cấu hình trong DB để dễ điều chỉnh.
/// Quy ước <see cref="BetValue"/>:
///  - Sum: "3".."18"
///  - Size: "Nho" | "Hoa" | "Lon"
///  - NumberCount: "1" | "2" | "3" (số lần digit xuất hiện)
///  - Triple: "specific" | "any"
/// </summary>
public class PrizeRule
{
    public int Id { get; set; }

    public BetKind BetKind { get; set; }

    public string BetValue { get; set; } = string.Empty;

    public decimal Multiplier { get; set; }

    public string Description { get; set; } = string.Empty;
}
