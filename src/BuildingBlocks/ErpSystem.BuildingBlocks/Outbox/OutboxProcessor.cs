using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.BuildingBlocks.EventBus;

namespace ErpSystem.BuildingBlocks.Outbox;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private const int BatchSize = 20;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        
        // Services/Infrastructure MUST register IOutboxRepository
        var repository = scope.ServiceProvider.GetService<IOutboxRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        if (repository == null)
        {
            // If the service hasn't registered a repository, we can't do anything.
            // This allows the processor to be registered but idle if strict outbox isn't used.
            // Or log warning logic.
            return;
        }

        var messages = await repository.GetUnprocessedAsync(BatchSize, stoppingToken);

        foreach (var message in messages)
        {
            try
            {
                var payload = message.DeserializePayload();

                if (payload != null)
                {
                    await eventBus.PublishAsync(payload);
                }

                message.MarkAsProcessed();
                await repository.UpdateAsync(message, stoppingToken);
                
                _logger.LogInformation("Processed outbox message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                message.MarkAsFailed(ex.Message);
                await repository.UpdateAsync(message, stoppingToken);
            }
        }
    }
}
