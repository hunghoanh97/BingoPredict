using Bingo.Domain.Enums;

namespace Bingo.Domain.Entities;

/// <summary>
/// Một kỳ quay Bingo18 nạp từ API thật. Đã chuẩn hóa sẵn các giá trị dẫn xuất.
/// </summary>
public class Draw
{
    public long Id { get; set; }

    /// <summary>Thời điểm quay (UTC), duy nhất.</summary>
    public DateTime DrawAt { get; set; }

    /// <summary>Kết quả 3 chữ số, ví dụ "236".</summary>
    public string WinningResult { get; set; } = string.Empty;

    public int D1 { get; set; }
    public int D2 { get; set; }
    public int D3 { get; set; }

    /// <summary>Tổng 3 số (3-18).</summary>
    public int Sum { get; set; }

    public SizeResult Size { get; set; }

    public bool IsTriple { get; set; }

    /// <summary>Chữ số của bộ ba trùng (nếu IsTriple), ngược lại null.</summary>
    public int? TripleDigit { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Tạo Draw từ dữ liệu thô của API.</summary>
    public static Draw FromRaw(DateTime drawAtUtc, string winningResult, DateTime nowUtc)
    {
        var (d1, d2, d3) = BingoRules.ParseDigits(winningResult);
        var sum = BingoRules.Sum(d1, d2, d3);
        var triple = BingoRules.IsTriple(d1, d2, d3);
        return new Draw
        {
            DrawAt = drawAtUtc,
            WinningResult = winningResult,
            D1 = d1,
            D2 = d2,
            D3 = d3,
            Sum = sum,
            Size = BingoRules.GetSize(sum),
            IsTriple = triple,
            TripleDigit = triple ? d1 : null,
            CreatedAt = nowUtc
        };
    }
}
