using Bingo.Application.Services;
using Bingo.Application.Simulation;
using Bingo.Application.Simulation.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Bingo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ---- 18 chiến lược (stateless -> singleton) ----
        services.AddSingleton<IBettingStrategy, AlwaysTaiStrategy>();
        services.AddSingleton<IBettingStrategy, AlwaysXiuStrategy>();
        services.AddSingleton<IBettingStrategy, AlwaysHoaStrategy>();
        services.AddSingleton<IBettingStrategy, StreakFollowStrategy>();
        services.AddSingleton<IBettingStrategy, StreakReverseStrategy>();
        services.AddSingleton<IBettingStrategy, HotSumStrategy>();
        services.AddSingleton<IBettingStrategy, ColdSumStrategy>();
        services.AddSingleton<IBettingStrategy, MostLikelySumStrategy>();
        services.AddSingleton<IBettingStrategy, FrequencyWeightedStrategy>();
        services.AddSingleton<IBettingStrategy, EvMaxStrategy>();
        services.AddSingleton<IBettingStrategy, MartingaleSizeStrategy>();
        services.AddSingleton<IBettingStrategy, ParoliSizeStrategy>();
        services.AddSingleton<IBettingStrategy, MarkovSumStrategy>();
        services.AddSingleton<IBettingStrategy, EwmaAdaptiveStrategy>();
        services.AddSingleton<IBettingStrategy, MlPredictStrategy>();
        services.AddSingleton<IBettingStrategy, HedgeJackpotStrategy>();
        services.AddSingleton<IBettingStrategy, RandomBaselineStrategy>();
        services.AddSingleton<IBettingStrategy, NumberHunterStrategy>();

        services.AddSingleton<IStrategyRegistry, StrategyRegistry>();

        // ---- use-case services (dùng UnitOfWork -> scoped) ----
        services.AddScoped<IDrawIngestionService, DrawIngestionService>();
        services.AddScoped<ISimulationService, SimulationService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<ITunerService, TunerService>();

        return services;
    }
}
