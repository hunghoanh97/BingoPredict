using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bingo.ApiService.Models.Entities;

[Table("players")]
public class Player
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    [Column("phone")]
    public string? Phone { get; set; }

    [Column("balance")]
    [Precision(18, 2)]
    public decimal Balance { get; set; } = 0;

    [Column("vip_level")]
    public int VipLevel { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Bet> Bets { get; set; } = new List<Bet>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<PlayerStatistics> Statistics { get; set; } = new List<PlayerStatistics>();
}