using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Procurement.Infrastructure;
using MediatR;
using Dapr.Client;
using ErpSystem.BuildingBlocks;

namespace ErpSystem.Procurement;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Dapr Client
        var daprClient = new DaprClientBuilder().Build();

        // Fetch connection string from Dapr Secrets with retry
        string? connectionString = null;
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var secrets = await daprClient.GetSecretAsync("localsecretstore", "connectionstrings:procurementdb");
                connectionString = secrets.Values.FirstOrDefault();
                if (!string.IsNullOrEmpty(connectionString)) break;
            }
            catch { await Task.Delay(1000); }
        }

        if (string.IsNullOrEmpty(connectionString))
            connectionString = builder.Configuration.GetConnectionString("procurementdb");

        // Persistence
        builder.Services.AddDbContext<ProcurementEventStoreDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddDbContext<ProcurementReadDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Dapr
        builder.Services.AddDaprClient();

        // BuildingBlocks
        builder.Services.AddBuildingBlocks(new[] { typeof(Program).Assembly });
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

        WebApplication app = builder.Build();

        // Ensure databases created (for dev/demo)
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using IServiceScope scope = app.Services.CreateScope();
            ProcurementEventStoreDbContext es = scope.ServiceProvider.GetRequiredService<ProcurementEventStoreDbContext>();
            await es.Database.EnsureCreatedAsync();
            ProcurementReadDbContext rs = scope.ServiceProvider.GetRequiredService<ProcurementReadDbContext>();
            await rs.Database.EnsureCreatedAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapSubscribeHandler(); // Dapr subscription
        app.MapControllers();

        app.Run();
    }
}
