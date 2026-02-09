using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace ErpSystem.BuildingBlocks.EventBus;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}

public class DaprEventBus(DaprClient dapr) : IEventBus
{
    private const string PubSubName = "pubsub";

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        // Topic convention: Type Name
        string topicName = typeof(T).Name;
        await dapr.PublishEventAsync(PubSubName, topicName, @event, cancellationToken);
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
