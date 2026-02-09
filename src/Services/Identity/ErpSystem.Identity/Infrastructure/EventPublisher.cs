using MediatR;

namespace ErpSystem.Identity.Infrastructure;

public class EventPublisher(IPublisher publisher)
{
    public async Task PublishAsync(IEnumerable<object> events)
    {
        foreach (object @event in events)
        {
            if (@event is INotification notification)
            {
                await publisher.Publish(notification);
            }
        }
    }
}
