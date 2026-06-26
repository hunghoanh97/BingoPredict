using Bingo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Bingo.Infrastructure.Jobs;

/// <summary>Job live chạy mỗi 6 phút: nạp kỳ mới + chạy một nhịp mô phỏng.</summary>
[DisallowConcurrentExecution]
public sealed class SimulationJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulationJob> _logger;

    public SimulationJob(IServiceScopeFactory scopeFactory, ILogger<SimulationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var ingestion = scope.ServiceProvider.GetRequiredService<IDrawIngestionService>();
        var sim = scope.ServiceProvider.GetRequiredService<ISimulationService>();
        try
        {
            var ingested = await ingestion.IngestAsync(ct: context.CancellationToken);
            var placed = await sim.RunTickAsync(context.CancellationToken);
            _logger.LogInformation("SimulationJob: nạp {Ingested} kỳ mới, đặt {Placed} vé", ingested, placed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SimulationJob lỗi");
        }
    }
}
