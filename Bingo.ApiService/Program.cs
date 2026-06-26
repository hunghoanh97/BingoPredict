using Bingo.Application;
using Bingo.Application.Dtos;
using Bingo.Application.Persistence;
using Bingo.Application.Services;
using Bingo.Domain;
using Bingo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();

// Áp dụng migration + nạp dữ liệu khởi tạo (chiến lược, bảng thưởng, users).
await app.Services.MigrateAndSeedAsync();

var sim = app.MapGroup("/api/sim");

// ---- Đọc dữ liệu (cho dashboard) ----
sim.MapGet("/leaderboard", async (ILeaderboardService s, string? metric) =>
    Results.Ok(await s.GetLeaderboardAsync(metric ?? "roi")));

sim.MapGet("/users", async (ILeaderboardService s) => Results.Ok(await s.GetUsersAsync()));

sim.MapGet("/users/{id:int}", async (ILeaderboardService s, int id) =>
    await s.GetUserDetailAsync(id) is { } u ? Results.Ok(u) : Results.NotFound());

sim.MapGet("/daily", async (ILeaderboardService s, string? date) =>
    Results.Ok(await s.GetDailyAsync(ParseDate(date))));

sim.MapGet("/draws/latest", async (ILeaderboardService s, int? count) =>
    Results.Ok(await s.GetLatestDrawsAsync(count ?? 20)));

sim.MapGet("/tickets", async (ILeaderboardService s, int? userId, string? date) =>
    Results.Ok(await s.GetTicketsAsync(userId, string.IsNullOrWhiteSpace(date) ? null : DateOnly.Parse(date))));

sim.MapGet("/strategies", async (ILeaderboardService s) => Results.Ok(await s.GetStrategiesAsync()));

// Săn lợi nhuận: xếp hạng mọi chiến lược theo lợi nhuận trên dữ liệu thật.
sim.MapGet("/discover", async (ITunerService s, string? metric, int? max) =>
    Results.Ok(await s.DiscoverAsync(metric ?? "roi", max ?? 2000)));

// ---- Hành động (admin) ----
sim.MapPost("/ingest", async (IDrawIngestionService s) =>
    Results.Ok(new OperationResultDto($"Đã nạp {await s.IngestAsync()} kỳ mới.")));

sim.MapPost("/run-tick", async (ISimulationService s) =>
    Results.Ok(new OperationResultDto($"Đã đặt {await s.RunTickAsync()} vé cho kỳ kế tiếp.")));

sim.MapPost("/replay", async (ISimulationService s, int? max) =>
{
    var r = await s.ReplayAsync(max ?? 2000);
    return Results.Ok(new OperationResultDto($"Replay {r.DrawsProcessed} kỳ, tạo {r.TicketsCreated} vé."));
});

sim.MapPost("/optimize", async (ITunerService s, string? metric, int? max) =>
{
    var r = await s.OptimizeAsync(metric ?? "roi", max ?? 2000);
    return Results.Ok(new OperationResultDto($"Đã tinh chỉnh {r.UsersTuned} user theo {metric ?? "roi"}. {r.Detail}"));
});

sim.MapPost("/reset", async (IUnitOfWork u) =>
{
    await u.ResetBettingDataAsync();
    return Results.Ok(new OperationResultDto("Đã reset dữ liệu cá cược (giữ draws/users/strategies)."));
});

app.MapDefaultEndpoints();

app.Run();

// Ngày chơi mặc định = hôm nay theo múi giờ lịch quay (+07:00).
static DateOnly ParseDate(string? s) =>
    string.IsNullOrWhiteSpace(s) ? BingoRules.GameDateOf(DateTime.UtcNow) : DateOnly.Parse(s);
