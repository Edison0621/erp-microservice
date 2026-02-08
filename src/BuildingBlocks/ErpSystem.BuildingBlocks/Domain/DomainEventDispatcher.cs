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

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IPublisher publisher, ILogger<DomainEventDispatcher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task DispatchEventsAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // Clear events before publishing to prevent double dispatch
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            _logger.LogDebug("Dispatching domain event: {EventType}", domainEvent.GetType().Name);
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}

/// <summary>
/// SaveChanges interceptor that dispatches domain events
/// </summary>
public class DomainEventDispatcherInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher _dispatcher;

    public DomainEventDispatcherInterceptor(IDomainEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await _dispatcher.DispatchEventsAsync(eventData.Context, cancellationToken);
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
