using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ErpSystem.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ErpSystem.BuildingBlocks.Outbox;

public class OutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        DbContext? context = eventData.Context;

        if (context == null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        List<EntityEntry<AggregateRoot<Guid>>> entries = context.ChangeTracker
            .Entries<AggregateRoot<Guid>>() // Assuming Guid is the common key, or use object and reflection
            .Where(e => e.Entity.GetChanges().Any())
            .ToList();

        // If no aggregates with Guid, maybe try checking for a common "IAggregate" if we had one.
        // But AggregateRoot<TId> is the base. 
        // We can just iterate all entries and check via reflection if needed, but generic <AggregateRoot<Guid>> covers most.
        // Let's assume Guid for now as per DDDBase. 
        // Actually DDDBase defines AggregateRoot<TId>. 
        // Most services likely use Guid.

        foreach (EntityEntry<AggregateRoot<Guid>> entry in entries)
        {
            AggregateRoot<Guid> aggregate = entry.Entity;
            IReadOnlyCollection<IDomainEvent> events = aggregate.GetChanges();

            foreach (IDomainEvent @event in events)
            {
                OutboxMessage message = OutboxMessage.Create(@event); 
                context.Set<OutboxMessage>().Add(message);
            }

            aggregate.ClearChanges();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
