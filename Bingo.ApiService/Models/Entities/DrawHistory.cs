using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bingo.ApiService.Models.Entities;

[Table("draw_history")]
public class DrawHistory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(20)]
    [Column("draw_number")]
    public string DrawNumber { get; set; } = string.Empty;

    [Column("draw_date")]
    public DateTime DrawDate { get; set; }

    [Required]
    [Column("drawn_numbers")]
    public string DrawnNumbers { get; set; } = string.Empty; // JSON array of 3 numbers

    [Column("total_sum")]
    public int TotalSum { get; set; }

    [Column("size_result")]
    public SizeResult SizeResult { get; set; }

    [Column("frequency_analysis")]
    public string? FrequencyAnalysis { get; set; } // JSON with number frequencies

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}