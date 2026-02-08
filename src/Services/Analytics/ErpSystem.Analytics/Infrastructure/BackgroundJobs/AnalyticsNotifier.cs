using Microsoft.AspNetCore.SignalR;
using ErpSystem.Analytics.API.Hubs;

namespace ErpSystem.Analytics.Infrastructure.BackgroundJobs;

public class AnalyticsNotifier : BackgroundService
{
    private readonly IHubContext<AnalyticsHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalyticsNotifier> _logger;

    public AnalyticsNotifier(
        IHubContext<AnalyticsHub> hubContext,
        IServiceScopeFactory scopeFactory,
        ILogger<AnalyticsNotifier> logger)
    {
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var extractor = scope.ServiceProvider.GetRequiredService<TimescaleDataExtractor>();

                var stats = await extractor.GetRealTimeStats();

                if (stats.Any())
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveStats", stats, stoppingToken);
                    _logger.LogInformation("Broadcasted {Count} stats updates", stats.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for analytics updates");
            }
        }
    }
}
