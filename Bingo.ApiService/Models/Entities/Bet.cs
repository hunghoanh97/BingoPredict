using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bingo.ApiService.Models.Entities;

[Table("bets")]
public class Bet
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("player_id")]
    [ForeignKey("Player")]
    public Guid PlayerId { get; set; }

    [Required]
    [Column("game_id")]
    [ForeignKey("Game")]
    public Guid GameId { get; set; }

    [Required]
    [Column("bet_type")]
    public BetType BetType { get; set; }

    [Column("bet_numbers")]
    public string? BetNumbers { get; set; } // JSON array based on bet type

    [Column("bet_amount")]
    [Precision(18, 2)]
    public decimal BetAmount { get; set; }

    [Column("potential_win")]
    [Precision(18, 2)]
    public decimal PotentialWin { get; set; }

    [Column("actual_win")]
    [Precision(18, 2)]
    public decimal? ActualWin { get; set; }

    [Column("status")]
    public BetStatus Status { get; set; } = BetStatus.Pending;

    [Column("placed_at")]
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    // Navigation properties
    public virtual Player Player { get; set; } = null!;
    public virtual Game Game { get; set; } = null!;
}

public enum BetType
{
    SingleNumber,    // Một số: 1-6
    MatchingNumbers, // Trùng nhau: 2-3 số giống nhau
    TotalSum,        // Cộng tổng: 3-18
    Size             // Lớn/Hòa/Nhỏ
}

public enum BetStatus
{
    Pending,
    Won,
    Lost,
    Cancelled,
    Refunded
}