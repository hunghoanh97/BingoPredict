using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bingo.ApiService.Models.Entities;

[Table("games")]
public class Game
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(20)]
    [Column("game_number")]
    public string GameNumber { get; set; } = string.Empty;

    [Column("draw_time")]
    public DateTime DrawTime { get; set; }

    [Column("status")]
    public GameStatus Status { get; set; } = GameStatus.Scheduled;

    [Column("drawn_numbers")]
    public string? DrawnNumbers { get; set; } // JSON array of 3 numbers [1-6]

    [Column("total_sum")]
    public int? TotalSum { get; set; }

    [Column("size_result")]
    public SizeResult? SizeResult { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();
    public virtual ICollection<GameResult> Results { get; set; } = new List<GameResult>();
}

public enum GameStatus
{
    Scheduled,
    Drawing,
    Completed,
    Cancelled
}

public enum SizeResult
{
    Small, // 3-10
    Tie,   // 11
    Large  // 12-18
}