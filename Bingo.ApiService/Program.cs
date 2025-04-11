using OfficeOpenXml;
using Bingo.ApiService.Services;

namespace Bingo.ApiService;

public class Program
{
    public static void Main(string[] args)
    {
        #region config
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<IBingoService, BingoService>();
        builder.Services.AddHostedService<PredictionSummaryService>();

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




        app.MapDefaultEndpoints();

        app.Run();
    }
}

