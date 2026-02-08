using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MediatR;
using ErpSystem.BuildingBlocks.EventBus;

namespace ErpSystem.BuildingBlocks.Domain;

public interface IDomainEvent : MediatR.INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

public abstract class AggregateRoot<TId>
{
    public TId Id { get; protected set; } = default!;
    public long Version { get; private set; } = -1;

    private readonly List<IDomainEvent> _changes = new();

    public IReadOnlyCollection<IDomainEvent> GetChanges() => _changes.AsReadOnly();

    public void ClearChanges() => _changes.Clear();

    protected void ApplyChange(IDomainEvent @event)
    {
        Apply(@event);
        _changes.Add(@event);
    }

    protected abstract void Apply(IDomainEvent @event);

    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var e in history)
        {
            Apply(e);
            Version++;
        }
    }
}

public class EventStream
{
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public long Version { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
}

public interface IEventStore
{
    Task SaveAggregateAsync<TAggregate>(TAggregate aggregate) where TAggregate : AggregateRoot<Guid>, new();
    Task<TAggregate?> LoadAggregateAsync<TAggregate>(Guid id) where TAggregate : AggregateRoot<Guid>, new();
}

public class EventStore : IEventStore
{
    private readonly DbContext _context;
    private readonly IPublisher _publisher;
    private readonly IEventBus _eventBus;
    private readonly Func<string, Type> _eventTypeResolver;

    public EventStore(DbContext context, IPublisher publisher, IEventBus eventBus, Func<string, Type> eventTypeResolver)
    {
        _context = context;
        _publisher = publisher;
        _eventBus = eventBus;
        _eventTypeResolver = eventTypeResolver;
    }

    public async Task SaveAggregateAsync<TAggregate>(TAggregate aggregate) where TAggregate : AggregateRoot<Guid>, new()
    {
        var changes = aggregate.GetChanges();
        if (!changes.Any()) return;

        var version = aggregate.Version;
        var eventsToPublish = new List<IDomainEvent>();

        foreach (var @event in changes)
        {
            version++;
            var stream = new EventStream
            {
                AggregateId = aggregate.Id,
                AggregateType = typeof(TAggregate).Name,
                Version = version,
                EventType = @event.GetType().Name,
                Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                OccurredOn = @event.OccurredOn
            };
            _context.Set<EventStream>().Add(stream);
            eventsToPublish.Add(@event);
        }

        await _context.SaveChangesAsync();

        foreach (var evt in eventsToPublish)
        {
            if (evt is INotification notification)
            {
                await _publisher.Publish(notification);
            }
            await _eventBus.PublishAsync(evt);
        }

        aggregate.ClearChanges();
    }

    public async Task<TAggregate?> LoadAggregateAsync<TAggregate>(Guid id) where TAggregate : AggregateRoot<Guid>, new()
    {
        var streams = await _context.Set<EventStream>()
            .Where(e => e.AggregateId == id)
            .OrderBy(e => e.Version)
            .ToListAsync();

        if (!streams.Any()) return null;

        var aggregate = new TAggregate();
        var history = streams.Select(s =>
        {
            var type = _eventTypeResolver(s.EventType);
            return (IDomainEvent)JsonSerializer.Deserialize(s.Payload, type!)!;
        });

        aggregate.LoadFromHistory(history);
        return aggregate;
    }
}

public class EventStoreRepository<TAggregate> : IEventStore where TAggregate : AggregateRoot<Guid>, new()
{
    private readonly IEventStore _eventStore;

    public EventStoreRepository(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public Task SaveAsync(TAggregate aggregate) => _eventStore.SaveAggregateAsync(aggregate);
    public Task<TAggregate?> LoadAsync(Guid id) => _eventStore.LoadAggregateAsync<TAggregate>(id);

    // Implementation of IEventStore
    public Task SaveAggregateAsync<T>(T aggregate) where T : AggregateRoot<Guid>, new()
        => _eventStore.SaveAggregateAsync(aggregate);

    public Task<T?> LoadAggregateAsync<T>(Guid id) where T : AggregateRoot<Guid>, new()
        => _eventStore.LoadAggregateAsync<T>(id);
}
