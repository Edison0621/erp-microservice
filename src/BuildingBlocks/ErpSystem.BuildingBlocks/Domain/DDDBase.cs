using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MediatR;
using ErpSystem.BuildingBlocks.EventBus;

namespace ErpSystem.BuildingBlocks.Domain;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

public abstract class AggregateRoot<TId>
{
    public TId Id { get; protected set; } = default!;
    public long Version { get; private set; } = -1;

    private readonly List<IDomainEvent> _changes = [];

    public IReadOnlyCollection<IDomainEvent> GetChanges() => this._changes.AsReadOnly();

    public void ClearChanges() => this._changes.Clear();

    protected void ApplyChange(IDomainEvent @event)
    {
        this.Apply(@event);
        this._changes.Add(@event);
    }

    protected abstract void Apply(IDomainEvent @event);

    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (IDomainEvent e in history)
        {
            this.Apply(e);
            this.Version++;
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

public class EventStore(DbContext context, IPublisher publisher, IEventBus eventBus, Func<string, Type> eventTypeResolver)
    : IEventStore
{
    public async Task SaveAggregateAsync<TAggregate>(TAggregate aggregate) where TAggregate : AggregateRoot<Guid>, new()
    {
        IReadOnlyCollection<IDomainEvent> changes = aggregate.GetChanges();
        if (!changes.Any()) return;

        long version = aggregate.Version;
        List<IDomainEvent> eventsToPublish = [];

        foreach (IDomainEvent @event in changes)
        {
            version++;
            EventStream stream = new()
            {
                AggregateId = aggregate.Id,
                AggregateType = typeof(TAggregate).Name,
                Version = version,
                EventType = @event.GetType().Name,
                Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                OccurredOn = @event.OccurredOn
            };
            context.Set<EventStream>().Add(stream);
            eventsToPublish.Add(@event);
        }

        await context.SaveChangesAsync();

        foreach (IDomainEvent evt in eventsToPublish)
        {
            if (evt is INotification notification)
            {
                await publisher.Publish(notification);
            }

            await eventBus.PublishAsync(evt);
        }

        aggregate.ClearChanges();
    }

    public async Task<TAggregate?> LoadAggregateAsync<TAggregate>(Guid id) where TAggregate : AggregateRoot<Guid>, new()
    {
        List<EventStream> streams = await context.Set<EventStream>()
            .Where(e => e.AggregateId == id)
            .OrderBy(e => e.Version)
            .ToListAsync();

        if (!streams.Any()) return null;

        TAggregate aggregate = new();
        IEnumerable<IDomainEvent> history = streams.Select(s =>
        {
            Type type = eventTypeResolver(s.EventType);
            return (IDomainEvent)JsonSerializer.Deserialize(s.Payload, type!)!;
        });

        aggregate.LoadFromHistory(history);
        return aggregate;
    }
}

public class EventStoreRepository<TAggregate>(IEventStore eventStore) : IEventStore
    where TAggregate : AggregateRoot<Guid>, new()
{
    public Task SaveAsync(TAggregate aggregate) => eventStore.SaveAggregateAsync(aggregate);
    public Task<TAggregate?> LoadAsync(Guid id) => eventStore.LoadAggregateAsync<TAggregate>(id);

    // Implementation of IEventStore
    public Task SaveAggregateAsync<T>(T aggregate) where T : AggregateRoot<Guid>, new()
        =>
            eventStore.SaveAggregateAsync(aggregate);

    public Task<T?> LoadAggregateAsync<T>(Guid id) where T : AggregateRoot<Guid>, new()
        =>
            eventStore.LoadAggregateAsync<T>(id);
}
