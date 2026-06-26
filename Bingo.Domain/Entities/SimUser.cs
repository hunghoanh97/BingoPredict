namespace Bingo.Domain.Entities;

/// <summary>
/// Người chơi giả lập (bot). Mỗi user dùng đúng một chiến lược.
/// </summary>
public class SimUser
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Khóa chiến lược (FK -> Strategy.Key).</summary>
    public string StrategyKey { get; set; } = string.Empty;

    /// <summary>Tham số riêng của user (JSON) override DefaultParamsJson của chiến lược.</summary>
    public string? ConfigJson { get; set; }

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public Strategy? Strategy { get; set; }
}
