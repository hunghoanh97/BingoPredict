using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bingo.ApiService.Models.Entities;

[Table("game_results")]
public class GameResult
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("game_id")]
    [ForeignKey("Game")]
    public Guid GameId { get; set; }

    [Column("drawn_numbers")]
    public string DrawnNumbers { get; set; } = string.Empty; // JSON array of drawn numbers

    [Column("total_sum")]
    public int TotalSum { get; set; }

    [Column("size_result")]
    public SizeResult SizeResult { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Game Game { get; set; } = null!;
}