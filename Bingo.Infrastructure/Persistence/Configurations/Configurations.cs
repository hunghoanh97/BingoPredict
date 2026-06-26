using Bingo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bingo.Infrastructure.Persistence.Configurations;

public sealed class DrawConfig : IEntityTypeConfiguration<Draw>
{
    public void Configure(EntityTypeBuilder<Draw> e)
    {
        e.ToTable("draws");
        e.HasKey(x => x.Id);
        e.HasIndex(x => x.DrawAt).IsUnique();
        e.Property(x => x.WinningResult).HasMaxLength(3).IsRequired();
        e.Property(x => x.DrawAt).HasColumnType("timestamp with time zone");
        e.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
    }
}

public sealed class StrategyConfig : IEntityTypeConfiguration<Strategy>
{
    public void Configure(EntityTypeBuilder<Strategy> e)
    {
        e.ToTable("strategies");
        e.HasKey(x => x.Id);
        e.HasIndex(x => x.Key).IsUnique();
        e.Property(x => x.Key).HasMaxLength(64).IsRequired();
        e.Property(x => x.Name).HasMaxLength(128).IsRequired();
        e.Property(x => x.Description).HasMaxLength(1024).IsRequired();
    }
}

public sealed class SimUserConfig : IEntityTypeConfiguration<SimUser>
{
    public void Configure(EntityTypeBuilder<SimUser> e)
    {
        e.ToTable("sim_users");
        e.HasKey(x => x.Id);
        e.Property(x => x.Name).HasMaxLength(128).IsRequired();
        e.Property(x => x.StrategyKey).HasMaxLength(64).IsRequired();
        e.HasIndex(x => x.StrategyKey);
        e.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        e.HasOne(x => x.Strategy)
            .WithMany()
            .HasForeignKey(x => x.StrategyKey)
            .HasPrincipalKey(s => s.Key)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DailyAccountConfig : IEntityTypeConfiguration<DailyAccount>
{
    public void Configure(EntityTypeBuilder<DailyAccount> e)
    {
        e.ToTable("daily_accounts");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.SimUserId, x.GameDate }).IsUnique();
        e.Property(x => x.StartingBalance).HasPrecision(18, 2);
        e.Property(x => x.CurrentBalance).HasPrecision(18, 2);
        e.Property(x => x.TotalStaked).HasPrecision(18, 2);
        e.Property(x => x.TotalPayout).HasPrecision(18, 2);
        e.Ignore(x => x.NetProfit);
        e.HasOne(x => x.SimUser)
            .WithMany()
            .HasForeignKey(x => x.SimUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TicketConfig : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> e)
    {
        e.ToTable("tickets");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.SimUserId, x.TargetDrawAt });
        e.HasIndex(x => x.DailyAccountId);
        e.HasIndex(x => x.IsSettled);
        e.Property(x => x.BetKind).HasConversion<string>().HasMaxLength(16);
        e.Property(x => x.BetValue).HasMaxLength(32).IsRequired();
        e.Property(x => x.Stake).HasPrecision(18, 2);
        e.Property(x => x.Multiplier).HasPrecision(9, 2);
        e.Property(x => x.Payout).HasPrecision(18, 2);
        e.Property(x => x.Profit).HasPrecision(18, 2);
        e.Property(x => x.TargetDrawAt).HasColumnType("timestamp with time zone");
        e.Property(x => x.PlacedAt).HasColumnType("timestamp with time zone");

        e.HasOne(x => x.DailyAccount)
            .WithMany()
            .HasForeignKey(x => x.DailyAccountId)
            .OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Draw)
            .WithMany()
            .HasForeignKey(x => x.DrawId)
            .OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.SimUser)
            .WithMany()
            .HasForeignKey(x => x.SimUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PrizeRuleConfig : IEntityTypeConfiguration<PrizeRule>
{
    public void Configure(EntityTypeBuilder<PrizeRule> e)
    {
        e.ToTable("prize_rules");
        e.HasKey(x => x.Id);
        e.Property(x => x.BetKind).HasConversion<string>().HasMaxLength(16);
        e.Property(x => x.BetValue).HasMaxLength(32).IsRequired();
        e.Property(x => x.Multiplier).HasPrecision(9, 2);
        e.Property(x => x.Description).HasMaxLength(256).IsRequired();
        e.HasIndex(x => new { x.BetKind, x.BetValue }).IsUnique();
    }
}

public sealed class UserStatConfig : IEntityTypeConfiguration<UserStat>
{
    public void Configure(EntityTypeBuilder<UserStat> e)
    {
        e.ToTable("user_stats");
        e.HasKey(x => x.SimUserId);
        e.Property(x => x.SimUserId).ValueGeneratedNever();
        e.Property(x => x.WinRate).HasPrecision(9, 2);
        e.Property(x => x.Roi).HasPrecision(12, 2);
        e.Property(x => x.TotalStaked).HasPrecision(18, 2);
        e.Property(x => x.TotalPayout).HasPrecision(18, 2);
        e.Property(x => x.NetProfit).HasPrecision(18, 2);
        e.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        e.HasOne(x => x.SimUser)
            .WithMany()
            .HasForeignKey(x => x.SimUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserStrategyStateConfig : IEntityTypeConfiguration<UserStrategyState>
{
    public void Configure(EntityTypeBuilder<UserStrategyState> e)
    {
        e.ToTable("user_strategy_states");
        e.HasKey(x => x.SimUserId);
        e.Property(x => x.SimUserId).ValueGeneratedNever();
        e.Property(x => x.StateJson).HasColumnType("jsonb").IsRequired();
        e.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        e.HasOne(x => x.SimUser)
            .WithMany()
            .HasForeignKey(x => x.SimUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
