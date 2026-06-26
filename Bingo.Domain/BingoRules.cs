using Bingo.Domain.Enums;

namespace Bingo.Domain;

/// <summary>
/// Logic luật chơi thuần (không phụ thuộc hạ tầng): phân tích kết quả quay,
/// tính tổng, xác định khoảng Tài/Xỉu/Hòa, kiểm tra ba số trùng.
/// </summary>
public static class BingoRules
{
    /// <summary>Tách chuỗi kết quả "236" thành 3 chữ số (mỗi số 1-6).</summary>
    public static (int d1, int d2, int d3) ParseDigits(string winningResult)
    {
        if (string.IsNullOrWhiteSpace(winningResult) || winningResult.Length != 3)
            throw new ArgumentException($"Kết quả không hợp lệ: '{winningResult}'", nameof(winningResult));

        int d1 = winningResult[0] - '0';
        int d2 = winningResult[1] - '0';
        int d3 = winningResult[2] - '0';

        if (d1 is < 1 or > 6 || d2 is < 1 or > 6 || d3 is < 1 or > 6)
            throw new ArgumentException($"Chữ số phải trong khoảng 1-6: '{winningResult}'", nameof(winningResult));

        return (d1, d2, d3);
    }

    public static int Sum(int d1, int d2, int d3) => d1 + d2 + d3;

    /// <summary>Xác định khoảng: Nhỏ 3-9, Hòa 10-11, Lớn 12-18.</summary>
    public static SizeResult GetSize(int sum) => sum switch
    {
        >= 3 and <= 9 => SizeResult.Nho,
        10 or 11 => SizeResult.Hoa,
        >= 12 and <= 18 => SizeResult.Lon,
        _ => throw new ArgumentOutOfRangeException(nameof(sum), sum, "Tổng phải trong khoảng 3-18")
    };

    public static bool IsTriple(int d1, int d2, int d3) => d1 == d2 && d2 == d3;

    /// <summary>Đếm số lần một digit xuất hiện trong kết quả.</summary>
    public static int CountDigit(int digit, int d1, int d2, int d3)
    {
        int c = 0;
        if (d1 == digit) c++;
        if (d2 == digit) c++;
        if (d3 == digit) c++;
        return c;
    }

    /// <summary>Phân phối số cách ra mỗi tổng (3-18) trên 216 kết quả của 3 xúc xắc 1-6.</summary>
    public static readonly IReadOnlyDictionary<int, int> SumCombinations = new Dictionary<int, int>
    {
        [3] = 1, [4] = 3, [5] = 6, [6] = 10, [7] = 15, [8] = 21, [9] = 25, [10] = 27,
        [11] = 27, [12] = 25, [13] = 21, [14] = 15, [15] = 10, [16] = 6, [17] = 3, [18] = 1
    };

    public const int TotalCombinations = 216;

    /// <summary>Xác suất lý thuyết một tổng xuất hiện.</summary>
    public static double SumProbability(int sum) =>
        SumCombinations.TryGetValue(sum, out var c) ? (double)c / TotalCombinations : 0d;

    /// <summary>Xác suất lý thuyết của một khoảng Tài/Xỉu/Hòa.</summary>
    public static double SizeProbability(SizeResult size) => size switch
    {
        SizeResult.Nho => 81d / TotalCombinations, // tổng 3-9
        SizeResult.Hoa => 54d / TotalCombinations, // tổng 10-11
        SizeResult.Lon => 81d / TotalCombinations, // tổng 12-18
        _ => 0d
    };

    /// <summary>"Ngày chơi" của một thời điểm UTC, quy theo múi giờ lịch quay (+07:00).</summary>
    public static DateOnly GameDateOf(DateTime utc) =>
        DateOnly.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified) + GameConstants.GameTimeZoneOffset);
}
