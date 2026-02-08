using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace ErpSystem.BuildingBlocks.EventBus;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}

public class DaprEventBus : IEventBus
{
    private readonly DaprClient _dapr;
    private const string PUBSUB_NAME = "pubsub"; 

    public DaprEventBus(DaprClient dapr)
    {
        _dapr = dapr;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        // Topic convention: Type Name
        var topicName = typeof(T).Name;
        await _dapr.PublishEventAsync(PUBSUB_NAME, topicName, @event, cancellationToken);
    }
}

public class DummyEventBus : IEventBus
{
    private readonly ILogger<DummyEventBus> _logger;

    public DummyEventBus(ILogger<DummyEventBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogInformation("DummyEventBus: Publishing event {EventName}", typeof(T).Name);
        return Task.CompletedTask;
    }
}
