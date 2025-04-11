using OfficeOpenXml;
using Bingo.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
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
        builder.Services.AddScoped<IBingoService, BingoService>();

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

        app.MapDefaultEndpoints();

        app.Run();
    }
}

