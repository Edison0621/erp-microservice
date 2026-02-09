using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using MediatR;

namespace ErpSystem.Identity;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Dapr Client
        var daprClient = new Dapr.Client.DaprClientBuilder().Build();

        // Fetch connection string from Dapr Secrets with retry
        string? connectionString = null;
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var secrets = await daprClient.GetSecretAsync("localsecretstore", "connectionstrings:identitydb");
                connectionString = secrets.Values.FirstOrDefault();
                if (!string.IsNullOrEmpty(connectionString)) break;
            }
            catch { await Task.Delay(1000); }
        }

        if (string.IsNullOrEmpty(connectionString))
            connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        // DB
        builder.Services.AddDbContext<EventStoreDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddDbContext<IdentityReadDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Dapr
        builder.Services.AddDaprClient();

        // MediatR
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Application.HrIntegrationEventHandler).Assembly));
        builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());
        builder.Services.AddDaprEventBus();

        // Register the main EventStore
        builder.Services.AddScoped<IEventStore>(sp =>
            new EventStore(
                sp.GetRequiredService<EventStoreDbContext>(),
                sp.GetRequiredService<IPublisher>(),
                sp.GetRequiredService<IEventBus>(),
                name => Type.GetType($"ErpSystem.Identity.Domain.{name}, ErpSystem.Identity")!));

        // Register typed repositories
        builder.Services.AddScoped(typeof(EventStoreRepository<>));
        builder.Services.AddHealthChecks();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapSubscribeHandler(); // Dapr subscription
        app.MapControllers();
        app.MapHealthChecks("/health");

        // Auto-migrate (for demo)
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using IServiceScope scope = app.Services.CreateScope();
            EventStoreDbContext db = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
            db.Database.EnsureCreated();
            IdentityReadDbContext readDb = scope.ServiceProvider.GetRequiredService<IdentityReadDbContext>();
            readDb.Database.EnsureCreated();
        }

        app.Run();
    }
}
