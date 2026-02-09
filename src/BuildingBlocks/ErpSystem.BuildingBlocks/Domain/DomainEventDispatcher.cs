using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ErpSystem.BuildingBlocks.Domain;

/// <summary>
/// Domain Event Dispatcher - Dispatches domain events after SaveChanges.
/// Integrates with EF Core to collect and publish events from aggregates.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(DbContext context, CancellationToken cancellationToken = default);
}

public class DomainEventDispatcher(IPublisher publisher, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        List<IAggregateRoot> aggregatesWithEvents = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        List<IDomainEvent> domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // Clear events before publishing to prevent double dispatch
        foreach (IAggregateRoot aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            logger.LogDebug("Dispatching domain event: {EventType}", domainEvent.GetType().Name);
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}

/// <summary>
/// SaveChanges interceptor that dispatches domain events
/// </summary>
public class DomainEventDispatcherInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await dispatcher.DispatchEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}

/// <summary>
/// Interface for aggregate roots with domain events
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
