using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bingo.ApiService.Models.Entities;

[Table("transactions")]
public class Transaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("player_id")]
    [ForeignKey("Player")]
    public Guid PlayerId { get; set; }

    [Required]
    [Column("type")]
    public TransactionType Type { get; set; }

    [Required]
    [Column("amount")]
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Column("balance_before")]
    [Precision(18, 2)]
    public decimal BalanceBefore { get; set; }

    [Column("balance_after")]
    [Precision(18, 2)]
    public decimal BalanceAfter { get; set; }

    [StringLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("reference_id")]
    public Guid? ReferenceId { get; set; } // Reference to Bet or other entities

    [Column("status")]
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Player Player { get; set; } = null!;
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    BetPlaced,
    BetWon,
    Refund,
    Bonus
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}