using Bingo.Application.Abstractions;
using Bingo.Application.Persistence;
using Bingo.Domain;
using Bingo.Infrastructure.External;
using Bingo.Infrastructure.Jobs;
using Bingo.Infrastructure.Ml;
using Bingo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Bingo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BingoDb");
        services.AddDbContext<BingoDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(BingoDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPredictionModel, MlnetPredictionModel>();
        services.AddHttpClient<IBingoDataClient, BingoDataClient>();

        // Live job 6 phút/kỳ (bật/tắt qua cấu hình Simulation:LiveEnabled, mặc định bật).
        var liveEnabled = configuration.GetValue("Simulation:LiveEnabled", true);
        if (liveEnabled)
        {
            services.AddQuartz(q =>
            {
                var jobKey = new JobKey(nameof(SimulationJob));
                q.AddJob<SimulationJob>(o => o.WithIdentity(jobKey));
                q.AddTrigger(o => o
                    .ForJob(jobKey)
                    .WithIdentity($"{nameof(SimulationJob)}-trigger")
                    .StartAt(DateBuilder.FutureDate(30, IntervalUnit.Second))
                    .WithSimpleSchedule(s => s
                        .WithIntervalInMinutes(GameConstants.DrawIntervalMinutes)
                        .RepeatForever()));
            });
            services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
        }

        return services;
    }

    /// <summary>Áp dụng migration và nạp dữ liệu khởi tạo. Gọi lúc khởi động.</summary>
    public static async Task MigrateAndSeedAsync(this IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BingoDbContext>();
        await ctx.Database.MigrateAsync(ct);
        await DataSeeder.SeedAsync(ctx, ct);
    }
}
