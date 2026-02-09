using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.CRM.Infrastructure;
using MediatR;

namespace ErpSystem.CRM;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add service defaults

        // Persistence
        builder.Services.AddDbContext<CrmEventStoreDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("crmdb")));
        builder.Services.AddDbContext<CrmReadDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("crmdb")));

        // Dapr EventBus
        builder.Services.AddDaprEventBus();

        // MediatR - MUST be before IPublisher!
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly));

        // IPublisher (depends on MediatR)
        builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        // Register the main EventStore
        builder.Services.AddScoped<IEventStore>(sp =>
            new EventStore(
                sp.GetRequiredService<CrmEventStoreDbContext>(),
                sp.GetRequiredService<IPublisher>(),
                sp.GetRequiredService<IEventBus>(),
                name => Type.GetType($"ErpSystem.CRM.Domain.{name}, ErpSystem.CRM")!));

        // Register typed repositories
        builder.Services.AddScoped(typeof(EventStoreRepository<>));

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "ERP System - CRM Service", Version = "v1" });
        });

        WebApplication app = builder.Build();

        // Ensure databases created
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using IServiceScope scope = app.Services.CreateScope();
            CrmEventStoreDbContext es = scope.ServiceProvider.GetRequiredService<CrmEventStoreDbContext>();
            await es.Database.EnsureCreatedAsync();
            CrmReadDbContext rs = scope.ServiceProvider.GetRequiredService<CrmReadDbContext>();
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
