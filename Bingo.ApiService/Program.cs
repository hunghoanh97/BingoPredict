using OfficeOpenXml;
using Bingo.ApiService.Services;
using Bingo.ApiService.Data;
using Bingo.ApiService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl;

namespace Bingo.ApiService;

public class Program
{
    public static void Main(string[] args)
    {
        #region config
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Add services to the container.
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpClient();
        
        // Configure Entity Framework
        builder.Services.AddDbContext<BingoDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("BingoDb");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("Bingo.ApiService");
                npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });

            var dbSettings = builder.Configuration.GetSection("DatabaseSettings");
            if (dbSettings.GetValue<bool>("EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
            
            if (dbSettings.GetValue<bool>("EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }
            
            var commandTimeout = dbSettings.GetValue<int>("CommandTimeout");
            if (commandTimeout > 0)
            {
                options.ConfigureWarnings(warnings => warnings.Default(WarningBehavior.Throw));
            }
        });

        // Add repository pattern
        builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
        builder.Services.AddScoped<IGameRepository, GameRepository>();
        builder.Services.AddScoped<IBetRepository, BetRepository>();
        builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
        builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
        
        // Add unit of work
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Add services
        builder.Services.AddScoped<IBingoService, BingoService>();
        builder.Services.AddScoped<IGameService, GameService>();
        builder.Services.AddScoped<IBettingService, BettingService>();
        builder.Services.AddScoped<IPrizeCalculationService, PrizeCalculationService>();
        builder.Services.AddScoped<IStatisticsService, StatisticsService>();

        // Add Quartz services
        builder.Services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

            // Configure logging
            q.UseSimpleTypeLoader();
            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = 10;
            });

            // Configure job store
            q.UseInMemoryStore();

            // Configure the job
            var jobKey = new JobKey("predictionSummaryJob", "group1");
            q.AddJob<PredictionSummaryJob>(opts => opts
                .WithIdentity(jobKey)
                .StoreDurably());

            // Add trigger to run immediately and then every 6 minutes
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("predictionSummaryTrigger", "group1")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(6)
                    .RepeatForever()));
        });

        builder.Services.AddQuartzHostedService(q =>
        {
            q.WaitForJobsToComplete = true;
            q.AwaitApplicationStarted = true;
        });

        // Add scheduler
        builder.Services.AddSingleton<IScheduler>(sp =>
        {
            var factory = new StdSchedulerFactory();
            var scheduler = factory.GetScheduler().Result;
            scheduler.Start();
            return scheduler;
        });

        // Add PredictionSummaryService and Job
        builder.Services.AddSingleton<PredictionSummaryJob>();
        builder.Services.AddSingleton<PredictionSummaryService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<PredictionSummaryService>());

        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();
        #endregion
        //API
        app.MapGet("/api/bingo/export", async (IBingoService bingoService) =>
        {
            try
            {
                var excelData = await bingoService.ExportBingoDataAsync();
                return Results.File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BingoData.xlsx");
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/bingo/exportCsv", async (IBingoService bingoService) =>
        {
            try
            {
                var csvData = await bingoService.ExportBingoDataAsCsvAsync();
                return Results.File(csvData, "text/csv", "BingoData.csv");
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/bingo/predict", async (IBingoService bingoService) =>
        {
            try
            {
                var prediction = await bingoService.PredictNextSumAsync();
                return Results.Ok(prediction);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/bingo/check-prediction", async (IBingoService bingoService) =>
        {
            try
            {
                var prediction = await bingoService.CheckPredictionAccuracyAsync();
                return Results.Ok(prediction);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/bingo/latest-summaries", async ([FromServices] PredictionSummaryService summaryService) =>
        {
            try
            {
                var summaries = await summaryService.GetLatestSummariesAsync();
                return Results.Ok(summaries);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/bingo/latest-results", async ([FromServices] IBingoService bingoService) =>
        {
            try
            {
                var results = await bingoService.GetLatestResultsAsync();
                return Results.Ok(results);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // Database migration endpoint
        app.MapPost("/api/database/migrate", async ([FromServices] BingoDbContext dbContext) =>
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                return Results.Ok(new { message = "Database migrated successfully" });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Migration failed: {ex.Message}");
            }
        });

        // Game management endpoints
        app.MapPost("/api/games", async ([FromServices] IGameService gameService, [FromBody] CreateGameRequest request) =>
        {
            try
            {
                var game = await gameService.CreateGameAsync(request);
                return Results.Ok(game);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/games/{gameId}", async ([FromServices] IGameService gameService, Guid gameId) =>
        {
            try
            {
                var game = await gameService.GetGameAsync(gameId);
                return game != null ? Results.Ok(game) : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapPost("/api/games/{gameId}/draw", async ([FromServices] IGameService gameService, Guid gameId) =>
        {
            try
            {
                var result = await gameService.DrawNumbersAsync(gameId);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // Betting endpoints
        app.MapPost("/api/bets", async ([FromServices] IBettingService bettingService, [FromBody] PlaceBetRequest request) =>
        {
            try
            {
                var bet = await bettingService.PlaceBetAsync(request);
                return Results.Ok(bet);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/players/{playerId}/bets", async ([FromServices] IBettingService bettingService, Guid playerId, [FromQuery] int? limit = 50) =>
        {
            try
            {
                var bets = await bettingService.GetPlayerBetsAsync(playerId, limit ?? 50);
                return Results.Ok(bets);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // Player management endpoints
        app.MapPost("/api/players", async ([FromServices] IPlayerRepository playerRepo, [FromBody] CreatePlayerRequest request) =>
        {
            try
            {
                var player = await playerRepo.CreateAsync(request);
                return Results.Ok(player);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/players/{playerId}", async ([FromServices] IPlayerRepository playerRepo, Guid playerId) =>
        {
            try
            {
                var player = await playerRepo.GetByIdAsync(playerId);
                return player != null ? Results.Ok(player) : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/players/{playerId}/balance", async ([FromServices] IPlayerRepository playerRepo, Guid playerId) =>
        {
            try
            {
                var balance = await playerRepo.GetBalanceAsync(playerId);
                return Results.Ok(new { playerId, balance });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // Statistics endpoints
        app.MapGet("/api/statistics/number-frequency", async ([FromServices] IStatisticsService statsService, [FromQuery] int? days = 30) =>
        {
            try
            {
                var stats = await statsService.GetNumberFrequencyAsync(days ?? 30);
                return Results.Ok(stats);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapGet("/api/statistics/player/{playerId}", async ([FromServices] IStatisticsService statsService, Guid playerId) =>
        {
            try
            {
                var stats = await statsService.GetPlayerStatisticsAsync(playerId);
                return Results.Ok(stats);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        app.MapDefaultEndpoints();

        app.Run();
    }
}

