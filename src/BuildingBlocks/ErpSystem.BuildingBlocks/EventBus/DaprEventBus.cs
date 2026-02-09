using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace ErpSystem.BuildingBlocks.EventBus;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}

public class DaprEventBus(DaprClient dapr, ILogger<DaprEventBus> logger) : IEventBus
{
    private const string PubSubName = "pubsub";

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            string topicName = typeof(T).Name;
            logger.LogInformation("Publishing event {EventName} to topic {TopicName}", typeof(T).Name, topicName);
            await dapr.PublishEventAsync(PubSubName, topicName, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish event {EventName}", typeof(T).Name);
            throw;
        }
    }
}

public class DummyEventBus(ILogger<DummyEventBus> logger) : IEventBus
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        logger.LogInformation("DummyEventBus: Publishing event {EventName}", typeof(T).Name);
        return Task.CompletedTask;
    }
}
