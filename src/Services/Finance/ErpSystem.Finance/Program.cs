using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Finance.Infrastructure;
using MediatR;
using Dapr.Client;
using ErpSystem.BuildingBlocks;

namespace ErpSystem.Finance;

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
                var secrets = await daprClient.GetSecretAsync("localsecretstore", "connectionstrings:financedb");
                connectionString = secrets.Values.FirstOrDefault();
                if (!string.IsNullOrEmpty(connectionString)) break;
            }
            catch { await Task.Delay(1000); }
        }

        if (string.IsNullOrEmpty(connectionString))
            connectionString = builder.Configuration.GetConnectionString("financedb");

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Databases
        builder.Services.AddDbContext<FinanceEventStoreDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddDbContext<FinanceReadDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Dapr
        builder.Services.AddDaprClient();

        // BuildingBlocks
        builder.Services.AddBuildingBlocks(new[] { typeof(Program).Assembly });
        builder.Services.AddDaprEventBus();

        // MediatR - Exclude Infrastructure handlers that require MongoDB
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Application.GlCommandHandler).Assembly);
            // Exclude MaterialCostProjectionHandler which requires MongoDB
            cfg.AddOpenBehavior(typeof(ExcludeInfrastructureBehavior<,>));
        });

        // IPublisher (needed by EventStore)
        builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        // EventStore (needed by GLCommandHandler)
        builder.Services.AddScoped<IEventStore>(sp =>
            new EventStore(
                sp.GetRequiredService<FinanceEventStoreDbContext>(),
                sp.GetRequiredService<IPublisher>(),
                sp.GetRequiredService<IEventBus>(),
                name => Type.GetType($"ErpSystem.Finance.Domain.{name}, ErpSystem.Finance")!));

        // EventStoreRepository (needed by FinanceCommandHandler)
        builder.Services.AddScoped(typeof(EventStoreRepository<>));

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapSubscribeHandler(); // Dapr subscription
        app.MapControllers();

        // Ensure databases created
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using IServiceScope scope = app.Services.CreateScope();
            FinanceEventStoreDbContext es = scope.ServiceProvider.GetRequiredService<FinanceEventStoreDbContext>();
            await es.Database.EnsureCreatedAsync();
            FinanceReadDbContext rs = scope.ServiceProvider.GetRequiredService<FinanceReadDbContext>();
            await rs.Database.EnsureCreatedAsync();
        }

        app.Run();
    }
}

// Dummy behavior to satisfy MediatR configuration
public class ExcludeInfrastructureBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await next(cancellationToken);
    }
}
