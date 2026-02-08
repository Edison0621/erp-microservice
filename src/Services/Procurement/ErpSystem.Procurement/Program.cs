using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Procurement.Domain;
using ErpSystem.Procurement.Infrastructure;
using MediatR;

namespace ErpSystem.Procurement;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults
        

        // Persistence
        builder.Services.AddDbContext<ProcurementEventStoreDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("procurementdb")));
        builder.Services.AddDbContext<ProcurementReadDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("procurementdb")));

        // Dapr
        // builder.Services.AddDaprClient();

        // BuildingBlocks - EventBus first
        builder.Services.AddDaprEventBus();

        // MediatR - MUST be before IPublisher!
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly));

        // IPublisher (depends on MediatR)
        builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        // Register the main EventStore
        builder.Services.AddScoped<IEventStore>(sp => 
            new EventStore(
                sp.GetRequiredService<ProcurementEventStoreDbContext>(),
                sp.GetRequiredService<IPublisher>(),
                sp.GetRequiredService<IEventBus>(),
                name => Type.GetType($"ErpSystem.Procurement.Domain.{name}, ErpSystem.Procurement")!));

        // Register typed repositories
        builder.Services.AddScoped(typeof(EventStoreRepository<>));

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        

        // Ensure databases created (for dev/demo)
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using (var scope = app.Services.CreateScope())
            {
                var es = scope.ServiceProvider.GetRequiredService<ProcurementEventStoreDbContext>();
                await es.Database.EnsureCreatedAsync();
                var rs = scope.ServiceProvider.GetRequiredService<ProcurementReadDbContext>();
                await rs.Database.EnsureCreatedAsync();
            }
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
