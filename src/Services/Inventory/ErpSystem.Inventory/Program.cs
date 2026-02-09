using Microsoft.EntityFrameworkCore;
using ErpSystem.Inventory.Infrastructure;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Inventory.Domain.Services;
using MediatR;

namespace ErpSystem.Inventory;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Persistence
        builder.Services.AddDbContext<InventoryEventStoreDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("inventorydb")));
        builder.Services.AddDbContext<InventoryReadDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("inventorydb")));

        // Dapr
        // builder.Services.AddDaprClient();

        // BuildingBlocks
        builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());
        builder.Services.AddDaprEventBus();

        // Register the main EventStore
        builder.Services.AddScoped<IEventStore>(sp => 
            new EventStore(
                sp.GetRequiredService<InventoryEventStoreDbContext>(),
                sp.GetRequiredService<IPublisher>(),
                sp.GetRequiredService<IEventBus>(),
                name => Type.GetType($"ErpSystem.Inventory.Domain.{name}, ErpSystem.Inventory")!));

        // Register typed repositories
        builder.Services.AddScoped(typeof(EventStoreRepository<>));

        // MediatR
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Application.ProcurementIntegrationEventHandler).Assembly));

        // Services
        builder.Services.AddScoped<IInventoryForecastService, InventoryForecastService>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        WebApplication app = builder.Build();

        // Ensure databases created (for dev/demo)
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using IServiceScope scope = app.Services.CreateScope();
            InventoryEventStoreDbContext es = scope.ServiceProvider.GetRequiredService<InventoryEventStoreDbContext>();
            await es.Database.EnsureCreatedAsync();
            InventoryReadDbContext rs = scope.ServiceProvider.GetRequiredService<InventoryReadDbContext>();
            await rs.Database.EnsureCreatedAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        // app.MapSubscribeHandler(); // Dapr subscription

        app.Run();
    }
}
