namespace Bingo.Domain.Entities;

/// <summary>
/// Danh mục một cách chơi (chiến lược). Mỗi <see cref="SimUser"/> gắn với một Strategy qua Key.
/// </summary>
public class Strategy
{
    public int Id { get; set; }

    /// <summary>Khóa định danh duy nhất, ví dụ "always_tai".</summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Chiến lược có tự cập nhật trạng thái theo kết quả hay không.</summary>
    public bool IsAdaptive { get; set; }

    /// <summary>Tham số mặc định (JSON), có thể bị override bởi SimUser.ConfigJson.</summary>
    public string? DefaultParamsJson { get; set; }

    public bool Enabled { get; set; } = true;
}
