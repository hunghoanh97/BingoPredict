namespace Bingo.Domain.Enums;

/// <summary>
/// Các nhóm cách chơi của Bingo18.
/// </summary>
public enum BetKind
{
    /// <summary>Cộng tổng: đoán chính xác tổng 3 số (3-18).</summary>
    Sum,

    /// <summary>Tài/Xỉu/Hòa: đoán khoảng của tổng (Nhỏ/Hòa/Lớn).</summary>
    Size,

    /// <summary>Cơ bản: chọn 1 digit (1-6), ăn theo số lần xuất hiện.</summary>
    NumberCount,

    /// <summary>Ba số trùng nhau (cụ thể hoặc bất kỳ).</summary>
    Triple
}
