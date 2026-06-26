using Bingo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bingo.Infrastructure.Persistence;

public class BingoDbContext : DbContext
{
    public BingoDbContext(DbContextOptions<BingoDbContext> options) : base(options) { }

    public DbSet<Draw> Draws => Set<Draw>();
    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<SimUser> SimUsers => Set<SimUser>();
    public DbSet<DailyAccount> DailyAccounts => Set<DailyAccount>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<PrizeRule> PrizeRules => Set<PrizeRule>();
    public DbSet<UserStat> UserStats => Set<UserStat>();
    public DbSet<UserStrategyState> StrategyStates => Set<UserStrategyState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BingoDbContext).Assembly);
    }
}
