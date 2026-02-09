using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Finance.Infrastructure;
using MediatR;

namespace ErpSystem.Finance;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Databases
        builder.Services.AddDbContext<FinanceEventStoreDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("financedb")));
        builder.Services.AddDbContext<FinanceReadDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("financedb")));

        // EventBus (needed by EventStore)
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
