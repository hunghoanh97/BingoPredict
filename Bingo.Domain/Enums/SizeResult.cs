namespace Bingo.Domain.Enums;

/// <summary>
/// Khoảng tổng theo luật Tài/Xỉu/Hòa của Bingo18.
/// </summary>
public enum SizeResult
{
    /// <summary>Nhỏ (Xỉu): tổng 3-9.</summary>
    Nho,

    /// <summary>Hòa: tổng 10-11.</summary>
    Hoa,

    /// <summary>Lớn (Tài): tổng 12-18.</summary>
    Lon
}
