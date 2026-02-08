using Microsoft.Extensions.DependencyInjection;

namespace ErpSystem.BuildingBlocks.EventBus;

public static class EventBusExtensions
{
    public static IServiceCollection AddDaprEventBus(this IServiceCollection services)
    {
         // Dapr disabled for now
         // services.AddDaprClient(); 
         services.AddScoped<IEventBus, DummyEventBus>();
         return services;
    }
}
