using Microsoft.AspNetCore.SignalR;
using ErpSystem.Analytics.API.Hubs;

namespace ErpSystem.Analytics.Infrastructure.BackgroundJobs;

public class AnalyticsNotifier(
    IHubContext<AnalyticsHub> hubContext,
    IServiceScopeFactory scopeFactory,
    ILogger<AnalyticsNotifier> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                TimescaleDataExtractor extractor = scope.ServiceProvider.GetRequiredService<TimescaleDataExtractor>();

                List<MaterialStatsDto> stats = await extractor.GetRealTimeStats();

                if (stats.Any())
                {
                    await hubContext.Clients.All.SendAsync("ReceiveStats", stats, stoppingToken);
                    logger.LogInformation("Broadcasted {Count} stats updates", stats.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking for analytics updates");
            }
        }
    }
}
