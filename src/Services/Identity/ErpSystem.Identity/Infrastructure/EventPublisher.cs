using MediatR;

namespace ErpSystem.Identity.Infrastructure;

public class EventPublisher
{
    private readonly IPublisher _publisher;

    public EventPublisher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishAsync(IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            if (@event is INotification notification)
            {
                await _publisher.Publish(notification);
            }
        }
    }
}
