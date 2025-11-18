using Microsoft.EntityFrameworkCore;
using Bingo.ApiService.Models.Entities;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Bingo.ApiService.Data;

public class BingoDbContext : DbContext
{
    public BingoDbContext(DbContextOptions<BingoDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Player> Players { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Bet> Bets { get; set; }
    public DbSet<GameResult> GameResults { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<PlayerStatistics> PlayerStatistics { get; set; }
    public DbSet<DrawHistory> DrawHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Player
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure Game
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasIndex(e => e.GameNumber).IsUnique();
            entity.HasIndex(e => e.DrawTime);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.SizeResult).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure Bet
        modelBuilder.Entity<Bet>(entity =>
        {
            entity.HasIndex(e => new { e.PlayerId, e.GameId });
            entity.HasIndex(e => e.PlacedAt);
            entity.Property(e => e.BetType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.BetAmount).HasPrecision(18, 2);
            entity.Property(e => e.PotentialWin).HasPrecision(18, 2);
            entity.Property(e => e.ActualWin).HasPrecision(18, 2);
            entity.Property(e => e.PlacedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Player)
                  .WithMany(e => e.Bets)
                  .HasForeignKey(e => e.PlayerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Game)
                  .WithMany(e => e.Bets)
                  .HasForeignKey(e => e.GameId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure GameResult
        modelBuilder.Entity<GameResult>(entity =>
        {
            entity.HasIndex(e => e.GameId);
            entity.Property(e => e.DrawnNumbers).HasMaxLength(50);
            entity.Property(e => e.SizeResult).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Game)
                  .WithMany(e => e.Results)
                  .HasForeignKey(e => e.GameId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.BalanceBefore).HasPrecision(18, 2);
            entity.Property(e => e.BalanceAfter).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Player)
                  .WithMany(e => e.Transactions)
                  .HasForeignKey(e => e.PlayerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure PlayerStatistics
        modelBuilder.Entity<PlayerStatistics>(entity =>
        {
            entity.HasIndex(e => new { e.PlayerId, e.StatisticType });
            entity.Property(e => e.StatisticType).HasConversion<string>();
            entity.Property(e => e.WinRate).HasPrecision(5, 2);
            entity.Property(e => e.TotalBet).HasPrecision(18, 2);
            entity.Property(e => e.TotalWin).HasPrecision(18, 2);
            entity.Property(e => e.ProfitLoss).HasPrecision(18, 2);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Player)
                  .WithMany(e => e.Statistics)
                  .HasForeignKey(e => e.PlayerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DrawHistory
        modelBuilder.Entity<DrawHistory>(entity =>
        {
            entity.HasIndex(e => e.DrawNumber).IsUnique();
            entity.HasIndex(e => e.DrawDate);
            entity.Property(e => e.SizeResult).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure global settings
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.UseSerialColumns();
    }
}