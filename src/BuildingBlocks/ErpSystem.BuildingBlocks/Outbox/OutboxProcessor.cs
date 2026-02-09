using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.BuildingBlocks.EventBus;

namespace ErpSystem.BuildingBlocks.Outbox;

public class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger) : BackgroundService
{
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        
        // Services/Infrastructure MUST register IOutboxRepository
        IOutboxRepository? repository = scope.ServiceProvider.GetService<IOutboxRepository>();
        IEventBus eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        if (repository == null)
        {
            // If the service hasn't registered a repository, we can't do anything.
            // This allows the processor to be registered but idle if strict outbox isn't used.
            // Or log warning logic.
            return;
        }

        IReadOnlyList<OutboxMessage> messages = await repository.GetUnprocessedAsync(BatchSize, stoppingToken);

        foreach (OutboxMessage message in messages)
        {
            try
            {
                object? payload = message.DeserializePayload();

                if (payload != null)
                {
                    await eventBus.PublishAsync(payload, stoppingToken);
                }

                message.MarkAsProcessed();
                await repository.UpdateAsync(message, stoppingToken);

                logger.LogInformation("Processed outbox message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                message.MarkAsFailed(ex.Message);
                await repository.UpdateAsync(message, stoppingToken);
            }
        }
    }
}
