using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bingo.ApiService.Models.Entities;

[Table("player_statistics")]
public class PlayerStatistics
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("player_id")]
    [ForeignKey("Player")]
    public Guid PlayerId { get; set; }

    [Required]
    [Column("statistic_type")]
    public StatisticType StatisticType { get; set; }

    [Column("value")]
    public string Value { get; set; } = string.Empty; // JSON data based on type

    [Column("count")]
    public int Count { get; set; } = 0;

    [Column("win_rate")]
    [Precision(5, 2)]
    public decimal? WinRate { get; set; }

    [Column("total_bet")]
    [Precision(18, 2)]
    public decimal TotalBet { get; set; } = 0;

    [Column("total_win")]
    [Precision(18, 2)]
    public decimal TotalWin { get; set; } = 0;

    [Column("profit_loss")]
    [Precision(18, 2)]
    public decimal ProfitLoss { get; set; } = 0;

    [Column("total_bets")]
    public int TotalBets { get; set; } = 0;

    [Column("total_wins")]
    public int TotalWins { get; set; } = 0;

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Player Player { get; set; } = null!;
}

public enum StatisticType
{
    NumberFrequency,     // Tần suất số
    BetTypePerformance,  // Hiệu quả theo loại cược
    TimePattern,         // Mẫu thời gian
    SizePattern,         // Mẫu Lớn/Hòa/Nhỏ
    SumPattern           // Mẫu tổng
}