using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bingo.Infrastructure.Persistence;

/// <summary>
/// Factory dùng lúc design-time (dotnet ef migrations) — không cần chạy ứng dụng hay kết nối DB thật.
/// </summary>
public sealed class BingoDbContextFactory : IDesignTimeDbContextFactory<BingoDbContext>
{
    public BingoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BingoDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=BingoDb;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly(typeof(BingoDbContext).Assembly.FullName))
            .Options;
        return new BingoDbContext(options);
    }
}
