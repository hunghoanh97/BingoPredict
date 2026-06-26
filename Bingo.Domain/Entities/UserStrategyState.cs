namespace Bingo.Domain.Entities;

/// <summary>
/// Trạng thái nội bộ của chiến lược adaptive cho từng user
/// (ví dụ: bước martingale hiện tại, trọng số EWMA, ma trận Markov...).
/// </summary>
public class UserStrategyState
{
    /// <summary>PK đồng thời là FK 1-1 tới SimUser.</summary>
    public int SimUserId { get; set; }

    public string StateJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; }

    public SimUser? SimUser { get; set; }
}
